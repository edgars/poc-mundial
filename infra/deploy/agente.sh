#!/usr/bin/env bash
# Agente de deploy: compara o topo de origin/main com o que está implantado e
# chama o deploy.sh quando difere. Disparado por systemd timer a cada minuto.
#
# É um modelo *pull*: a máquina busca, o GitHub não empurra. Isso mantém a
# porta 22 fechada para o mundo — o security group libera SSH apenas para um
# IP — e dispensa guardar chave privada nos segredos do repositório.
set -euo pipefail

RAIZ=/opt/mundial
FONTE=$RAIZ/fonte

# O próprio deploy.sh vem do repositório: assim uma melhoria no processo de
# deploy chega pelo mesmo caminho que o código.
cd "$FONTE"
git fetch --depth 1 origin main --quiet
ALVO=$(git rev-parse origin/main)
ATUAL=$(git rev-parse HEAD)

if [ "$ALVO" = "$ATUAL" ]; then
  exit 0
fi

echo "novo commit em origin/main: ${ATUAL:0:7} → ${ALVO:0:7}"

# O agente também se atualiza pelo repositório, senão uma correção aqui só
# chegaria na máquina por scp — e o processo de deploy voltaria a ser manual.
# A variável de ambiente impede laço se a nova versão for igual à antiga.
if [ -z "${MUNDIAL_AGENTE_ATUALIZADO:-}" ]; then
  NOVO_AGENTE=$(mktemp)
  if git show "$ALVO:infra/deploy/agente.sh" > "$NOVO_AGENTE" 2>/dev/null &&
     [ -s "$NOVO_AGENTE" ] &&
     ! cmp -s "$NOVO_AGENTE" /usr/local/bin/mundial-agente.sh; then
    install -m 0755 "$NOVO_AGENTE" /usr/local/bin/mundial-agente.sh
    rm -f "$NOVO_AGENTE"
    echo "agente atualizado por ${ALVO:0:7}; reexecutando"
    MUNDIAL_AGENTE_ATUALIZADO=1 exec /usr/local/bin/mundial-agente.sh
  fi
  rm -f "$NOVO_AGENTE"
fi

# Commit que não toca no que roda não merece rebuild: trocar api e web tira a
# aplicação do ar por alguns segundos, e por um README isso é custo sem troco.
# Mesmos caminhos do paths-ignore do workflow, mais o que só afeta provisionamento.
IGNORAVEIS='(^docs/|^_bmad/|^_bmad-output/|^infra/terraform/|^\.github/|\.md$)'

if ! git diff --name-only "$ATUAL" "$ALVO" | grep -qvE "$IGNORAVEIS"; then
  echo "mudanças não afetam o que roda; apenas registrando ${ALVO:0:7}"
  git reset --hard "$ALVO" --quiet
  ESTADO=/opt/mundial/publico/deploy.json
  cat > "$ESTADO" <<JSON
{
  "situacao": "ok",
  "commit": "$ALVO",
  "anterior": "$ATUAL",
  "em": "$(date -Is)",
  "detalhe": "sem mudança no que roda; nenhum rebuild"
}
JSON
  chmod 644 "$ESTADO"
  exit 0
fi

# O deploy.sh do commit alvo é quem vai rodar — uma melhoria no processo de
# deploy chega pelo mesmo caminho que o código. Escreve num temporário e só
# substitui se vier conteúdo: redirecionar direto para o script vivo o trunca
# antes de saber se a origem existe, e aí não sobra nem o deploy anterior.
TEMP=$(mktemp)
trap 'rm -f "$TEMP"' EXIT

if git show "$ALVO:infra/deploy/deploy.sh" > "$TEMP" 2>/dev/null && [ -s "$TEMP" ]; then
  install -m 0755 "$TEMP" /usr/local/bin/mundial-deploy.sh
else
  echo "aviso: ${ALVO:0:7} não traz infra/deploy/deploy.sh; mantendo o script atual"
fi

[ -s /usr/local/bin/mundial-deploy.sh ] || { echo "erro: nenhum deploy.sh disponível"; exit 1; }

exec /usr/local/bin/mundial-deploy.sh "$ALVO"
