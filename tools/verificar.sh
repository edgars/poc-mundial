#!/bin/sh
# Verificação completa. Roda em container, sem exigir SDK na máquina.
# Qualquer falha aqui interrompe: é o portão antes de commitar.
set -e
cd "$(dirname "$0")/.."

echo "== invariantes de arquitetura =="
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/Mundial.Testes.Arquitetura --nologo -v q

echo
echo "== testes de regra =="
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/Mundial.Testes/Mundial.Testes.csproj --nologo -v q

echo
echo "== rastreabilidade ruleKey -> teste =="
python3 tools/rastreabilidade.py
