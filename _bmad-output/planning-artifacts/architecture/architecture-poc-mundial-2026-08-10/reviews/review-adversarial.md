# Review — Adversarial: duas unidades obedientes que ainda divergem

**Lente:** construir pares de unidades um nível abaixo que obedecem **todo** AD à risca e mesmo
assim se tornam incompatíveis. Cada par é um buraco a fechar.
**Veredito:** REPROVA — 6 pares encontrados, 2 deles contradição interna entre ADs existentes.

## Contradições dentro do próprio spine

### A-1 — CRÍTICO — AD-3 e AD-7 se contradizem

- **AD-3:** "nome de tabela e de coluna vêm de `getErModel`, **sem renomear**".
- **AD-7:** "`usuario.senha` … é **renomeada** para `senha_hash`".

Um construtor que segue AD-3 cria `usuario.senha`. Outro que segue AD-7 cria `usuario.senha_hash`.
Ambos obedeceram o spine. O schema diverge e o login quebra.

Pior: a tabela de `dataBindings` do AD-4 ainda lista `log_conf.senha3 → usuario.senha`, mantendo o
nome antigo em um terceiro lugar.

**Fechado por:** AD-3 ganha cláusula de exceção explícita — segurança (AD-7) vence fidelidade — e a
tabela do AD-4 foi corrigida.

### A-2 — ALTO — AD-3 proíbe a coluna que 3 regras exigem

AD-3: "um campo só entra no schema se `getErModel` o lista". Mas `barr_emb3` **não** aparece nas 6
colunas que o ER devolve para `estoq`, e três regras dependem dela: `RK-9f4468b42859`,
`RK-75e2169fe930`, `RK-dfe2ca45ec1a`. Um construtor omite a coluna e não consegue implementar as
regras; outro cria a coluna e viola AD-3.

**Fechado por:** cláusula em AD-3 — coluna citada por regra do UIR mas ausente do ER entra marcada,
e o par (coluna, `ruleKey`) vai para as questões em aberto do memlog.

## Pares que divergem sem violar nada

### A-3 — ALTO — Nenhum AD define o contrato de listagem

`GET /api/conferencias` e `GET /api/estoqs` são construídos por unidades diferentes. Nada no spine
diz como se pagina. Uma usa `?page=0&size=50` com envelope `{items,total}`; a outra usa
`?pageNumber=1&pageSize=50` com `X-Total-Count` no header. Ambas obedecem AD-1, AD-9 e AD-11.
O frontend precisa de dois clientes HTTP.

**Fechado por:** AD-15.

### A-4 — ALTO — `estoq` tem dois donos

O fluxo de conferência **lê** `estoq` para resolver DUN-14 → descrição/embalagem. O cadastro DUN-14
**escreve** em `estoq`. AD-10 declara `Conferencia` como agregado, mas nada declara quem é dono de
`estoq`. Uma unidade cria `EstoqRepository` no slice de conferência; a outra cria outro no slice de
cadastro. Duas fontes de escrita, dois caches, regras de unicidade de código de barras aplicadas em
um lado só.

**Fechado por:** AD-16.

### A-5 — MÉDIO — Concorrência no lançamento de item não decidida

Dois operadores conferem o mesmo documento em docas diferentes. AD-10 garante atomicidade só do
**finalizar**. O lançamento de quantidade (`qtd_rec`) não tem regra: uma unidade faz
`UPDATE … SET qtd_rec = @valor` (last-write-wins, perde contagem); outra faz
`SET qtd_rec = qtd_rec + @valor`. Resultados diferentes com os mesmos cliques.

Agrava: `FR-47` (`qtd_rec > 0` → "Este Código já tem Qtde lançada") só faz sentido se a semântica de
acúmulo estiver fixada.

**Fechado por:** AD-17.

### A-6 — MÉDIO — Validação no cliente sem regra de autoridade

AD-6 manda as 51 regras de validação para `Aplicacao`. Não diz o que o Angular faz. Uma unidade não
valida nada no cliente (operador bipa 40 itens e só descobre o erro no fim); outra reimplementa a
regra em TypeScript e ela sai de sincronia com a versão C# na primeira mudança.

**Fechado por:** AD-18.

### A-7 — MÉDIO — Fuso horário: UTC no JSON vs hora local do armazém

Convenção diz "ISO-8601 UTC no JSON, exibição `dd/MM/yyyy HH:mm`". Mas `dt_hora`, `data_conf` e
`data_mov` do legado são hora local do armazém, sem fuso. Uma unidade grava `DateTime.UtcNow`; outra
grava hora local. Uma conferência fechada às 23h30 aparece no dia seguinte para metade do sistema.

**Fechado por:** AD-19.

## Dimensão inteira em silêncio

### A-8 — ALTO — Nenhuma palavra sobre teste

O módulo TEA está instalado, o sistema tem 70 regras rastreáveis por `ruleKey`, e o spine não diz
onde o teste vive nem o que é obrigatório testar. Uma unidade entrega regra sem teste; outra monta
suíte E2E com Playwright. Sem contrato, a cobertura das regras migradas é impossível de auditar —
que é justamente a promessa do `ruleKey` do AD-5.

**Fechado por:** AD-20.
