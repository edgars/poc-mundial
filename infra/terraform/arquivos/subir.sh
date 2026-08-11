#!/usr/bin/env bash
# Sobe a aplicação inteira. Rode depois que as imagens existirem no registry
# e depois de `docker login <registry>` nesta máquina.
#
#   /opt/mundial/subir.sh            usa a TAG que está no .env
#   /opt/mundial/subir.sh 0.2.0      troca a tag e sobe essa versão
set -euo pipefail

cd /opt/mundial

if [ $# -ge 1 ]; then
  sed -i "s/^TAG=.*/TAG=$1/" .env
  # As três imagens carregam a tag no próprio valor; reescreve todas.
  sed -i -E "s#^(IMAGEM_(API|WEB|MIGRATIONS)=.*):[^:]*\$#\1:$1#" .env
  echo "TAG alterada para $1"
fi

docker compose pull
docker compose --profile app up -d --remove-orphans

echo
echo "Aguardando os serviços ficarem saudáveis..."
sleep 5
docker compose ps

echo
echo "Se 'migrations' não aparecer, confira que ele saiu com código 0:"
echo "  docker compose logs migrations"
