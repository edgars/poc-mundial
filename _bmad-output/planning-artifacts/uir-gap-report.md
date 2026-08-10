# Relatório de Divergência — UIR do RNC × Documentos BMAD

**Data:** 2026-08-10
**Workspace RNC:** `b2e707a2-7df1-4156-b9ad-a35ed33e2e78` (Mundial Conferencia)
**Módulo UIR:** `42977f88-ab4a-49f5-9887-427716780db6` — `dun14.SCX` (FOXPRO, 4 telas, 70 regras, 26 campos)
**Fonte:** MCP RNC — `listUirModules`, `getErModel`, `getUirModule`, `getModuleRules`

## Resumo

Os documentos em `docs/` divergem do UIR em pontos que quebram a diretiva do autor
*"O modelo de dados fidedigno ao RNC UIR"*. Implementar as stories como estão hoje produz
um app que **não é funcionalmente equivalente** ao legado.

Sete divergências (D-01 … D-07), quatro delas bloqueantes.

---

## D-01 — Chave primária composta descartada (BLOQUEANTE)

**UIR:** `conferencia` tem PK composta de 6 colunas:
`filial`, `orig_des`, `tipo_doc`, `SERIE`, `numero`, `codigo`.

**Docs:** `docs/architecture.md` declara `id Int (PK, auto)` e **omite as 6 colunas da PK**.

**Impacto:** a identidade de negócio do registro desaparece. Não há como localizar uma
conferência pelo documento fiscal (filial + série + número), que é exatamente como o
operador trabalha. Rotas `/conferencias/[id]/edit` não têm `id` no legado.

**Ação:** reintroduzir as 6 colunas. Ou PK composta, ou surrogate `id` + índice único
sobre as 6. A segunda opção é compatível com REST e preserva a regra.

---

## D-02 — Colunas fantasma: campos de tela virando colunas de tabela (BLOQUEANTE)

O gerador tratou **labels de controles do Visual FoxPro** como colunas.

Não existem em `conferencia` no ER do UIR:

| Coluna nos docs | Que é de verdade |
|---|---|
| `dataenvironment` | objeto `DataEnvironment` do form VFP |
| `timer1` | controle `Timer` do form |
| `listnf`, `listprod` | ListBox da tela de recebimento |
| `contem`, `qtde`, `ean13` | campos de tela sem binding no UIR |
| `descri`, `embalqt` | pertencem a `estoq`, não a `conferencia` |

Em `estoq`: `dataenvironment` e `codigo1` também são de tela (`codigo1` é o label do
campo na tela "Cadastro de Codigos Dun-14", que bindeia em `estoq.codigo`).

**Prova:** `getUirModule.ui.screens[].fields[]` lista exatamente esses nomes como campos
de tela; `getErModel` não os lista em nenhuma entidade.

**Ação:** remover do schema. `docs/ux/DESIGN.md` deve mapear label de tela → coluna real.

---

## D-03 — Labels de tela ≠ nomes de coluna (BLOQUEANTE)

`dataBindings` do UIR dá o mapeamento verdadeiro:

| Campo de tela | Label exibido | Coluna real |
|---|---|---|
| `rec_nf.documento` | Documento | `conferencia.acesso` (max 25) |
| `rec_nf.codigo` | Código Ean 13: | `conferencia.dun14` (max 14) |
| `rec_nf.doca` | Doca | `conferencia.doca` |
| `log_conf.senha3` | Senha3 | `usuario.senha` |

Os docs usam o label como se fosse a coluna. O campo rotulado "Código Ean 13:" grava em
`dun14` — e existe *outro* campo `ean13` na tela. Trocar os dois corrompe dado.

**Ação:** `docs/ux/DESIGN.md` e as stories devem citar coluna real + label separadamente.

---

## D-04 — Quatro entidades reais fora do escopo (BLOQUEANTE)

**UIR:** 6 entidades — `conferencia`, `estoq`, `forne`, `usuario`, `acesso`, `log_even`.
**Docs:** só `conferencia` e `estoq`. `bmad-context.md` diz "reference tables: 0".

| Entidade | Colunas | PK | Papel |
|---|---|---|---|
| `forne` | 46 | `codfor` | fornecedor — origem dos FR-51…FR-63 (`cgc`, `cod_com`, `categ`, `tiplog`, `lograd`, `bairro`, `cep`) |
| `usuario` | 6 | `matric` | login — origem dos FR-25…FR-28 (`senha`, `matric`, `nome`, `niv_usu`, `loja`) |
| `acesso` | 7 | `matric`+`arquivo` | permissões por tela: `alterar`, `incluir`, `excluir`, `consultar` — origem dos FR-53…FR-56 |
| `log_even` | 0 | — | auditoria (tabela sem colunas mapeadas) |

As FRs que eu havia chamado de órfãs **não são órfãs**: apontam para entidades que os
docs deixaram de fora. O PRD pede regras de tabelas que o schema não vai criar.

**FKs no UIR (docs dizem que não há nenhuma):** `acesso → usuario`, `conferencia → estoq`.

**Ação:** decidir escopo. Mínimo viável de equivalência: `usuario` + `acesso` (o legado
tem login e permissão por tela), `forne` como lookup read-only.

---

## D-05 — O app não é CRUD; é uma estação de conferência

Telas reais no UIR:

1. **Conferência de Recebimento de Mercadoria** (`rec_nf`) — 12 campos, 2 ações. Núcleo do sistema.
2. **Cadastro de Codigos Dun-14** (`dun14`) — 6 campos, 2 ações. Cadastro de código de barras de embalagem.
3. **Form1** (`log_conf`) — login: matrícula + senha + confirmação.
4. **Form1** — menu (`commandgroup1`).

O fluxo real é: operador loga → escolhe doca → bipa documento fiscal → bipa EAN-13 →
sistema resolve DUN-14 e embalagem via `estoq` → acumula quantidade → **finaliza
conferência** (FR-29, grava `fechado`, `matr_fec`, `dt_hora`) → imprime etiqueta ZPL.

Os docs BMAD reduziram isso a "list/create/edit/delete de Conferencias" — perdendo o
workflow inteiro. Nenhuma story cobre bipar, acumular, finalizar ou imprimir.

**Ação:** criar story do fluxo de conferência. CRUD genérico não substitui.

---

## D-06 — Classificação das 70 regras

Todas as 70 vêm com `completeness: COMPLETE`, `isUnambiguous: true`,
`requiresHumanReview: false`. **Nenhuma regra é NEEDS REVIEW** — a seção correspondente do
`bmad-context.md` não se aplica a este workspace.

Distribuição real (`candidateType`):

| Tipo | Qtd |
|---|---|
| APPLICATION_VALIDATION | 29 |
| VALIDATION | 22 |
| DOMAIN_INVARIANT | 19 |

Severidade: 48 WARN, 22 ERROR.

Mas por conteúdo, 16 das 70 não são regra de negócio:

- **7 regras são geração de etiqueta ZPL** (impressora Zebra): `^XA`, `^FO`, `^BCR`, `^XZ`.
  Chaves: `RK-b382d85d0edc`, `RK-0811a89bc8e6`, `RK-2b3c11b27fef`, `RK-25721748a2b1`,
  `RK-3ff169d79617`, `RK-1b386e3870da`, `RK-e8876989538a`.
  Isso é **um requisito de impressão de etiqueta**, não uma validação. Vira serviço próprio.
- **9 regras são confirmação de `MessageBox`** — UX de diálogo, não invariante.

**Ação:** o PRD deve separar validação de domínio, UX de confirmação e requisito de
impressão. Hoje os três estão na mesma lista de FRs.

---

## D-07 — PRD truncado e integrações ignoradas

- `docs/prd.md` **corta no meio do FR-63** (`cep is required`, sem linha de condição).
  Brief promete 70 regras; PRD entrega ~63 e incompletas.
- **23 integrações** no UIR, nenhuma citada nos docs:
  - 2 conexões **ODBC** para `estok_sgm` (sistema externo, `logicOpaque: true`)
  - 11 `SQL_PASSTHROUGH` (`usuario`, `acesso1`, `forne`, `conferencia`, `entrada`)
  - 5 `EMBEDDED_SQL` (`myquery`, `estoq`, `produto`, `vdireito`)
  - 4 `CALL` para módulos: `esco_imp`, `log_conf`, `menu_conf`, `dun14`
  - `dataStore: DBF+SQL_PASSTHROUGH` — o legado já é híbrido DBF + SQL

  A tabela `entrada` e o módulo `esco_imp` sugerem integração com o ERP de estoque.
  Migrar sem decidir o que acontece com `estok_sgm` deixa o app cego.
- **5 achados de segurança** ignorados: 3× `CLEARTEXT_PASSWORD` (HIGH) em `senha1/2/3`,
  2× `SENSITIVE_DATA_EXPOSURE` (MEDIUM) em `nome1` e `documento`.
  O legado guarda senha em texto puro (`usuario.senha VARCHAR`, comparação `This.Value#senha`
  no FR-26). O app novo **não pode** replicar isso — hash obrigatório.

---

## Conflito de stack (independente do UIR)

`docs/architecture.md`: `dotnet-core + prisma`. Prisma é ORM Node/TypeScript — não roda
em .NET. `bmad-context.md` já derivou `Backend language: typescript`. Precisa de decisão
do autor antes de qualquer código.

---

## Prioridade sugerida

1. Decidir stack (bloqueia tudo).
2. Corrigir schema: D-01, D-02, D-03 — diretiva "fidedigno ao UIR" exige.
3. Decidir escopo de entidades: D-04.
4. Reescrever PRD e stories: D-05, D-06, D-07.
5. Só então implementar.
