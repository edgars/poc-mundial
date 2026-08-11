#!/usr/bin/env bash
# Atualiza a POC para um commit. Roda NA máquina, chamado pelo agente.
#
#   deploy.sh <sha>     implanta esse commit
#   deploy.sh           implanta o topo de origin/main
#
# Garantias:
#   - imagem marcada pelo commit; a anterior continua na máquina para rollback
#   - o banco NUNCA é recriado; só migrações, api e web
#   - o compose passa a apontar para a imagem deste commit; confere depois
#     que os containers no ar são mesmo ela
#   - falhou qualquer das duas, volta sozinho para a versão anterior
#   - o estado do deploy fica em /opt/mundial/publico/deploy.json, servido
#     pelo proxy em /deploy.json — é assim que o pipeline confirma o resultado
set -euo pipefail

RAIZ=/opt/mundial
FONTE=$RAIZ/fonte
PUBLICO=$RAIZ/publico
ESTADO=$PUBLICO/deploy.json
URL_LOCAL=http://127.0.0.1/api/saude
TENTATIVAS_SAUDE=30

mkdir -p "$PUBLICO"

registra() { # situacao, sha, detalhe
  local agora
  agora=$(date -Is)
  cat > "$ESTADO" <<JSON
{
  "situacao": "$1",
  "commit": "$2",
  "anterior": "${SHA_ANTERIOR:-}",
  "em": "$agora",
  "detalhe": "$3"
}
JSON
  chmod 644 "$ESTADO"
}

log() { echo "[$(date +%H:%M:%S)] $*"; }

# ---------------------------------------------------------------- commit alvo
cd "$FONTE"
git fetch --depth 1 origin main --quiet
ALVO=${1:-$(git rev-parse origin/main)}
ALVO=$(git rev-parse "$ALVO")
SHA_ANTERIOR=$(git rev-parse HEAD 2>/dev/null || echo "")

if [ "$ALVO" = "$SHA_ANTERIOR" ] && [ -f "$ESTADO" ] && grep -q '"situacao": "ok"' "$ESTADO"; then
  log "já está em ${ALVO:0:7}; nada a fazer"
  exit 0
fi

log "implantando ${ALVO:0:7} (atual: ${SHA_ANTERIOR:0:7})"
registra "implantando" "$ALVO" "build em andamento"

# ------------------------------------------------------------------- checkout
git reset --hard "$ALVO" --quiet
# artefatos de build de outra plataforma quebram o restore dentro do container
find . -type d \( -name bin -o -name obj -o -name node_modules \) -prune -exec rm -rf {} + 2>/dev/null || true

# ---------------------------------------------------------------------- build
# Cada imagem é marcada com o commit. A tag :anterior guarda a versão que está
# no ar agora, para o rollback não depender de rebuild.
for img in api web migracoes; do
  if docker image inspect "mundial-$img:atual" >/dev/null 2>&1; then
    docker tag "mundial-$img:atual" "mundial-$img:anterior"
  fi
done

docker build --target api       -t "mundial-api:$ALVO"       -t mundial-api:atual       ./src
docker build --target migracoes -t "mundial-migracoes:$ALVO" -t mundial-migracoes:atual ./src
docker build --target web       -t "mundial-web:$ALVO"       -t mundial-web:atual       ./web

# ---------------------------------------------------------------------- subida
cd "$RAIZ"

# O compose lê IMAGEM_* do .env. Sem reescrever aqui, ele recria os containers
# a partir da tag antiga e o deploy vira um nada silencioso: o health check
# passa porque a versão velha está saudável, e o commit novo nunca entra no ar.
aponta_imagens() { # tag
  sed -i -E "s|^IMAGEM_API=.*|IMAGEM_API=mundial-api:$1|; \
             s|^IMAGEM_WEB=.*|IMAGEM_WEB=mundial-web:$1|; \
             s|^IMAGEM_MIGRACOES=.*|IMAGEM_MIGRACOES=mundial-migracoes:$1|" "$RAIZ/.env"
}
aponta_imagens "$ALVO"

# O db fica de fora de propósito: não muda entre versões da aplicação, e
# recriá-lo custaria a indisponibilidade que a demonstração não pode ter.
#
# O `up` espera a api ficar saudável, porque o web depende dela por health.
# Se a api entra em laço de reinício, o compose sai != 0 — e sob `set -e` isso
# matava o script exatamente aqui, antes do health check e antes do rollback:
# a POC ficava fora do ar e o deploy.json congelado em "implantando" para
# sempre. Falhar a subida agora é um resultado a tratar, não um fim de script.
subiu=1
docker compose --profile app up -d --force-recreate migracoes api web || subiu=0

# --------------------------------------------------------------- health check
saudavel=0
if [ "$subiu" = "0" ]; then
  MOTIVO="o compose não subiu os containers deste commit"
  log "$MOTIVO"
else
  log "verificando saúde"
  for _ in $(seq 1 $TENTATIVAS_SAUDE); do
    if curl -fsS --max-time 5 "$URL_LOCAL" >/dev/null 2>&1; then saudavel=1; break; fi
    sleep 3
  done
fi

# Responder não basta: a versão anterior também responde. Isto confere que os
# containers no ar são exatamente as imagens recém-construídas — a verificação
# que faltava quando o compose seguia apontando para uma tag velha.
imagem_certa() {
  local servico esperado rodando
  for servico in api web; do
    esperado=$(docker image inspect -f '{{.Id}}' "mundial-$servico:$ALVO" 2>/dev/null || echo x)
    rodando=$(docker inspect -f '{{.Image}}' "$(docker compose ps -q "$servico")" 2>/dev/null || echo y)
    if [ "$esperado" != "$rodando" ]; then
      log "container $servico não está rodando a imagem de ${ALVO:0:7}"
      return 1
    fi
  done
  return 0
}

if [ "$saudavel" = "1" ] && ! imagem_certa; then
  saudavel=0
  MOTIVO="containers subiram com imagem que não é a deste commit"
fi

if [ "$saudavel" = "1" ]; then
  log "no ar em ${ALVO:0:7}"
  registra "ok" "$ALVO" "health check respondeu e os containers rodam a imagem deste commit"
  docker image prune -f >/dev/null 2>&1 || true
  exit 0
fi

# ------------------------------------------------------------------- rollback
log "DEPLOY FALHOU — voltando para ${SHA_ANTERIOR:0:7}"
registra "revertendo" "$ALVO" "${MOTIVO:-health check nao respondeu}"

# Sem a tag :anterior não há para onde voltar — é o caso do primeiro deploy da
# máquina. Apontar o compose para uma imagem inexistente só trocaria uma queda
# por outra, então o estado terminal é "falhou" e a máquina fica como está.
if docker image inspect mundial-api:anterior >/dev/null 2>&1; then
  aponta_imagens anterior
  # Mesmo motivo da subida: se a volta também falhar, ainda queremos registrar
  # o desfecho em vez de morrer no set -e e deixar o estado em "revertendo".
  docker compose --profile app up -d --force-recreate migracoes api web ||
    log "aviso: a volta para :anterior também falhou"
  cd "$FONTE" && git reset --hard "$SHA_ANTERIOR" --quiet || true
  registra "revertido" "$SHA_ANTERIOR" "deploy de ${ALVO:0:7} falhou: ${MOTIVO:-health check}"
else
  log "não há imagem :anterior para voltar"
  registra "falhou" "$ALVO" "deploy falhou sem versão anterior: ${MOTIVO:-health check}"
fi
exit 1
