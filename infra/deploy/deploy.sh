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
INICIO=$(date +%s)

mkdir -p "$PUBLICO"

# Marca o deploy no coletor. É o que permite responder "isso começou depois de qual versão?" —
# sem marcador, uma regressão de latência e um commit novo são dois fatos sem ligação na tela.
#
# Log OTLP e não span: deploy é evento, não operação com pai e filho. Vai como serviço próprio
# (mundial-deploy), então não polui o gráfico de disponibilidade da api.
#
# Nunca derruba o deploy: sem coletor configurado sai calado, e a rede tem cinco segundos no
# máximo. Telemetria que impede o rollback é pior que telemetria nenhuma.
marca_deploy() { # situacao, sha, detalhe
  local endpoint token ambiente detalhe agora duracao severidade texto
  endpoint=$(grep -m1 '^OTEL_EXPORTER_OTLP_ENDPOINT=' "$RAIZ/.env" 2>/dev/null | cut -d= -f2- | tr -d '\r' || true)
  [ -n "$endpoint" ] || return 0

  token=$(grep -m1 '^OTEL_EXPORTER_OTLP_HEADERS=' "$RAIZ/.env" 2>/dev/null | sed 's/.*Bearer //' | tr -d '\r' || true)
  ambiente=$(grep -m1 '^OTEL_RESOURCE_ATTRIBUTES=' "$RAIZ/.env" 2>/dev/null | sed -n 's/.*deployment\.environment=\([^,]*\).*/\1/p' | tr -d '\r' || true)
  # Aspas e barras quebrariam o JSON montado à mão; nenhuma mensagem daqui as usa.
  detalhe=$(printf '%s' "$3" | tr -d '"\\' || true)
  agora=$(date +%s%N)
  duracao=$(( $(date +%s) - INICIO ))

  # Números do OTLP: INFO 9, WARN 13, ERROR 17. Sem esta distinção, um deploy revertido não
  # apareceria num filtro por severidade — que é o primeiro lugar onde se procura.
  case "$1" in
    falhou|revertido) severidade=17; texto=ERROR ;;
    revertendo)       severidade=13; texto=WARN ;;
    *)                severidade=9;  texto=INFO ;;
  esac

  curl -fsS --max-time 5 -X POST "$endpoint/v1/logs" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $token" \
    -d "{\"resourceLogs\":[{\"resource\":{\"attributes\":[
      {\"key\":\"service.name\",\"value\":{\"stringValue\":\"mundial-deploy\"}},
      {\"key\":\"deployment.environment\",\"value\":{\"stringValue\":\"$ambiente\"}}]},
      \"scopeLogs\":[{\"logRecords\":[{
        \"timeUnixNano\":\"$agora\",
        \"severityNumber\":$severidade,
        \"severityText\":\"$texto\",
        \"body\":{\"stringValue\":\"deploy $1 ${2:0:7}\"},
        \"attributes\":[
          {\"key\":\"event.name\",\"value\":{\"stringValue\":\"deploy\"}},
          {\"key\":\"deploy.situacao\",\"value\":{\"stringValue\":\"$1\"}},
          {\"key\":\"deploy.commit\",\"value\":{\"stringValue\":\"$2\"}},
          {\"key\":\"deploy.anterior\",\"value\":{\"stringValue\":\"${SHA_ANTERIOR:-}\"}},
          {\"key\":\"deploy.duracao_s\",\"value\":{\"intValue\":\"$duracao\"}},
          {\"key\":\"deploy.detalhe\",\"value\":{\"stringValue\":\"$detalhe\"}}]}]}]}]}" \
    >/dev/null 2>&1 || true
}

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
  # Todo desfecho do deploy passa por registra(), então marcar aqui cobre o começo, o fim e a
  # volta atrás sem espalhar chamada pelo script.
  marca_deploy "$1" "$2" "$3"
}

log() { echo "[$(date +%H:%M:%S)] $*"; }

# bin/ e obj/ estão versionados neste repositório, então o checkout os traz de volta a cada
# deploy — construídos em outra plataforma, e é isso que quebra o restore dentro do container.
# Precisa rodar depois do checkout e de novo depois dos testes, que os recriam.
limpa_artefatos() {
  find . -type d \( -name bin -o -name obj -o -name node_modules \) -prune -exec rm -rf {} + 2>/dev/null || true
}

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
limpa_artefatos

# ------------------------------------------------------------------- validação
# A barreira que faltava. Até aqui a única defesa era o health check DEPOIS da troca, e ele só
# pergunta se a API responde: um commit que compila e quebra uma regra de negócio passava por
# ele e ia ao ar. Os testes do GitHub rodam em paralelo ao deploy, não antes — quando ficam
# vermelhos, o commit já está publicado há minutos.
#
# Roda na mesma imagem do SDK que o build já usa, então não há nada novo para baixar. O volume
# de pacotes evita repetir o restore inteiro a cada deploy.
IMAGEM_SDK=mcr.microsoft.com/dotnet/sdk:10.0
testes_ok=1
for projeto in tests/Mundial.Testes tests/Mundial.Testes.Arquitetura; do
  log "testes: $projeto"
  docker run --rm \
    -v "$FONTE:/repo" -w /repo \
    -v mundial-nuget:/root/.nuget/packages \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    "$IMAGEM_SDK" dotnet test "$projeto" --nologo -v minimal || { testes_ok=0; break; }
done

# O dotnet test acabou de escrever bin/ e obj/ no checkout; o build não pode vê-los.
limpa_artefatos

if [ "$testes_ok" = "0" ]; then
  log "TESTES REPROVARAM — nada é trocado"
  registra "falhou" "$ALVO" "os testes reprovaram; a versão anterior segue no ar"
  exit 1
fi

# ---------------------------------------------------------------------- build
# Cada imagem é marcada com o commit. A tag :anterior guarda a versão que está
# no ar agora, para o rollback não depender de rebuild.
for img in api web migracoes; do
  if docker image inspect "mundial-$img:atual" >/dev/null 2>&1; then
    docker tag "mundial-$img:atual" "mundial-$img:anterior"
  fi
done

# VERSAO entra na imagem e vira service.version na telemetria. Pela imagem, e não pelo .env, de
# propósito: o .env da máquina só é escrito no primeiro boot, e variável nova nele nunca chega
# sozinha — foi assim que a telemetria inteira ficou desligada em produção.
#
# Build que falha é desfecho tratado, não morte do script. Sob `set -e` um erro de compilação
# saía daqui direto: os containers seguiam na versão antiga — o que está certo —, mas o
# deploy.json congelava em "implantando" e o agente, com o checkout já em $ALVO, dava o commit
# por implantado e nunca mais tentava. O pipeline esperava os vinte minutos inteiros e ficava
# vermelho sem dizer por quê. É o mesmo motivo do `subiu=0` mais abaixo.
for par in "api ./src" "migracoes ./src" "web ./web"; do
  set -- $par
  if ! docker build --target "$1" --build-arg "VERSAO=$ALVO" \
         -t "mundial-$1:$ALVO" -t "mundial-$1:atual" "$2"; then
    log "o build da imagem $1 falhou"
    registra "falhou" "$ALVO" "o build da imagem $1 falhou; a versão anterior segue no ar"
    exit 1
  fi
done

# ---------------------------------------------------------------------- subida
cd "$RAIZ"

# O compose da máquina foi escrito pelo cloud-init na primeira subida e, até aqui,
# nada o atualizava: mudanças em infra/terraform/ nem disparam deploy (o agente as
# ignora). Variável nova no repositório ficava só no repositório — foi o que
# aconteceria com as do OpenTelemetry. A cópia anterior fica ao lado, para conferir
# o que mudou se algo quebrar.
COMPOSE_REPO=$FONTE/infra/terraform/arquivos/docker-compose.yml
if [ -f "$COMPOSE_REPO" ] && ! cmp -s "$COMPOSE_REPO" "$RAIZ/docker-compose.yml"; then
  cp -f "$RAIZ/docker-compose.yml" "$RAIZ/docker-compose.yml.anterior" 2>/dev/null || true
  install -m 644 "$COMPOSE_REPO" "$RAIZ/docker-compose.yml"
  log "compose da máquina atualizado a partir do repositório"
fi

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
