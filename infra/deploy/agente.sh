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
