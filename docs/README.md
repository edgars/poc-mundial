# Documentação — Mundial · Conferência

## Onde está a verdade

Os documentos de planejamento válidos estão em **`_bmad-output/planning-artifacts/`**:

| Arquivo | O que é |
| --- | --- |
| `prds/prd-poc-mundial-2026-08-10/prd.md` | **PRD** — 54 requisitos funcionais, 12 não-funcionais, 3 jornadas |
| `architecture/architecture-poc-mundial-2026-08-10/ARCHITECTURE-SPINE.md` | **Arquitetura** — 21 decisões (`AD-1`…`AD-21`) |
| `epics.md` | **Épicos e stories** — 5 épicos, 30 stories com critérios de aceite |
| `achados-fonte-legada.md` | O que a leitura do código FoxPro original revelou |
| `uir-gap-report.md` | As 7 divergências entre o UIR do RNC e o pacote gerado |

## `historico-rnc/` — não use para construir

Esta pasta guarda o pacote que o RNC gerou automaticamente a partir do legado. Ele foi o ponto de
partida do trabalho e está preservado por rastreabilidade, mas **contém erros que produziriam o
aplicativo errado**:

- `prd.md` está truncado no meio do requisito 63, e cobre 2 das 6 entidades reais.
- `architecture.md` descarta a chave primária composta de 6 colunas de `conferencia` e declara uma
  combinação impossível de stack (`dotnet-core + prisma` — Prisma é ORM de Node, não roda em .NET).
- `ux/DESIGN.md` trata rótulo de tela como nome de coluna: o campo rotulado "Código Ean 13:" grava
  em `dun14`, não em `ean13`. Também lista como colunas oito controles de formulário do Visual
  FoxPro (`dataenvironment`, `timer1`, `listnf`, `listprod`…) que não são dado persistido.
- `stories/` descreve dois CRUDs genéricos no lugar do processo operacional de conferência, e não
  cobre login, permissão, fornecedor nem auditoria.

O relatório completo está em `_bmad-output/planning-artifacts/uir-gap-report.md`.
