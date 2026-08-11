#!/bin/sh
# Verificação completa: testes + rastreabilidade ruleKey -> teste (AD-20).
# Roda em container, sem exigir SDK na máquina.
set -e
cd "$(dirname "$0")/.."
echo "== testes =="
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/Mundial.Testes/Mundial.Testes.csproj --nologo -v q
echo
echo "== rastreabilidade =="
python3 tools/rastreabilidade.py
