#!/usr/bin/env python3
"""
AD-20: relatório de rastreabilidade ruleKey -> teste, gerado a cada build.

Cruza três fontes:
  1. as regras que o RNC recuperou (o UIR, ou a lista congelada abaixo)
  2. os atributos [RegraNegocio("RK-...")] no código
  3. os nomes dos testes que citam a mesma chave

Uma regra só conta como migrada quando aparece nas três (AD-5).
Sai com código 1 se alguma regra implementada estiver sem teste.
"""
import json, re, sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parent.parent

def chaves_no_codigo():
    achadas = {}
    for arq in (RAIZ / "src").rglob("*.cs"):
        for m in re.finditer(r'RegraNegocio\("(RK-[0-9a-f]{12})"', arq.read_text(encoding="utf-8")):
            achadas.setdefault(m.group(1), set()).add(arq.relative_to(RAIZ).as_posix())
    return achadas

def chaves_nos_testes():
    """Uma chave conta como testada quando aparece no DisplayName do teste ou nos
    InlineData de um Theory — os dois são citação explícita da regra."""
    achadas = {}
    for arq in (RAIZ / "tests").rglob("*.cs"):
        texto = arq.read_text(encoding="utf-8")
        for m in re.finditer(r'DisplayName = "([^"]*?(RK-[0-9a-f]{12})[^"]*)"', texto):
            achadas.setdefault(m.group(2), []).append(m.group(1))
        # Theory: as chaves ficam nos dados, e o DisplayName descreve o conjunto
        for bloco in re.finditer(
                r'((?:\s*\[InlineData\([^\]]*\)\]\n)+)\s*public[^\n]*?(\w+)\(', texto):
            dados, metodo = bloco.group(1), bloco.group(2)
            rotulo = re.search(r'Theory\(DisplayName = "([^"]+)"', texto[:bloco.start()][::-1][:400][::-1])
            nome = rotulo.group(1) if rotulo else metodo
            for m in re.finditer(r'(RK-[0-9a-f]{12})', dados):
                achadas.setdefault(m.group(1), []).append(f"{nome} [{m.group(1)}]")
    return achadas

def chaves_do_uir():
    """Lista congelada das 70 regras. O UIR é a autoridade; isto é o retrato dela."""
    arq = RAIZ / "tools" / "regras-uir.json"
    return set(json.loads(arq.read_text(encoding="utf-8"))) if arq.exists() else set()

def main():
    codigo, testes, uir = chaves_no_codigo(), chaves_nos_testes(), chaves_do_uir()
    implementadas = set(codigo)
    testadas = set(testes)

    sem_teste = sorted(implementadas - testadas)
    orfas = sorted(testadas - implementadas)
    nao_implementadas = sorted(uir - implementadas) if uir else []

    print("RASTREABILIDADE ruleKey -> teste  (AD-5, AD-20)")
    print("=" * 62)
    if uir:
        print(f"regras no UIR .............. {len(uir)}")
    print(f"implementadas no código .... {len(implementadas)}")
    print(f"com teste citando a chave .. {len(implementadas & testadas)}")
    print()

    for chave in sorted(implementadas):
        marca = "ok " if chave in testadas else "SEM TESTE"
        arquivos = ", ".join(sorted(codigo[chave]))
        print(f"  [{marca:9}] {chave}  {arquivos}")
        for nome in testes.get(chave, []):
            print(f"                 └─ {nome}")

    if nao_implementadas:
        print(f"\n{len(nao_implementadas)} regra(s) do UIR ainda sem implementação:")
        for c in nao_implementadas:
            print(f"  - {c}")

    if orfas:
        print("\nTestes citando chave que não existe no código:")
        for c in orfas:
            print(f"  - {c}")

    if sem_teste:
        print(f"\nFALHA: {len(sem_teste)} regra(s) implementada(s) sem teste.")
        for c in sem_teste:
            print(f"  - {c}")
        return 1

    print("\nToda regra implementada tem teste citando a mesma chave.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
