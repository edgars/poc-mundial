# Roteiro de teste — Mundial · Conferência

Guia para operar o POC à mão. Todos os dados abaixo são os que o seeder planta; conferi contra o
banco antes de escrever, então os códigos e quantidades são exatos.

## Subir

```bash
cp .env.example .env      # se ainda não existir
docker compose up --build
```

| Onde | Endereço |
| --- | --- |
| Aplicação | http://localhost:3000 |
| API | http://localhost:5001 |
| Banco | localhost:1433 · usuário `sa` · senha do `.env` |

O seed roda sozinho na subida quando o banco está vazio. Para semear de novo, apague os dados e
reinicie a API:

```bash
docker run --rm --network poc-mundial_default mcr.microsoft.com/mssql-tools \
  /opt/mssql-tools/bin/sqlcmd -S db -U sa -P 'Mundial#2026Dev' -d sgm \
  -Q "DELETE FROM conferencia; DELETE FROM acesso; DELETE FROM estoq;
      DELETE FROM forne; DELETE FROM usuario;"
docker compose restart api
```

> O container do banco não traz o `sqlcmd` — a imagem do Azure SQL Edge não o inclui. Por isso os
> comandos de banco usam um container efêmero com as ferramentas, na mesma rede do compose.

---

## Quem entra

Senha de todos: **`mundial`**

| Matrícula | Nome | Nível | O que consegue fazer |
| --- | --- | --- | --- |
| **04127** | Cleber Santos | 3 | Opera a conferência. **Não** pode incluir código novo. Não vê auditoria. |
| **04310** | Rosana Meireles | 3 | Supervisão — tudo, inclusive incluir código e ver a trilha de auditoria. |
| **04982** | Marcos Teixeira | 3 | Igual ao Cleber. Serve para testar dois operadores ao mesmo tempo. |
| **05001** | Paulo Andrade | 1 | **Não entra.** Nível insuficiente. |

A permissão é **por tabela**, não por tela (`acesso.arquivo`). Cleber tem `consultar` em
`conferencia`, `estoq` e `forne`, mas `incluir = 0` em todas — por isso a oferta de cadastrar um
código na hora não aparece para ele.

> Detalhe do legado: `acesso.arquivo` é `char(10)` e `conferencia` tem 11 caracteres, então a chave
> gravada é `conferenci`, truncada. Não é bug — é o que o legado sempre fez.

---

## O que está plantado

### Docas

| Doca | Documento | Fornecedor | Itens | Situação |
| --- | --- | --- | --- | --- |
| 1 | `000148372/1` | Bebidas Primavera | 3 de 4 lançados | em conferência · **tem divergência e item pendente** |
| 2 | `000147901/1` | Laticínios Serra Azul | 1 de 3 lançados | em conferência · **aberta há mais de 3h, aparece atrasada** |
| 3 | `000148415/2` | Higiene Total | 0 de 2 | aguardando |
| 4 | `000147744/1` | Higiene Total | 2 de 2 | **fechada** — somente leitura |

O painel ordena por **tempo de doca aberta**, não por número. A doca 2 aparece em primeiro, com
anel âmbar, porque passou do limite.

### Produtos e códigos de barras

| Código | Produto | Embalagem | EAN-13 (unidade) | DUN-14 (caixa) |
| --- | --- | --- | --- | --- |
| 04127 | Refrigerante Cola 2L | CX c/ 6 | `7891234567897` | `17891234567894` |
| 04982 | Cerveja Pilsen Lata 350ml | CX c/ 12 | `7891234500013` | `17891234500010` |
| 05310 | Água Mineral s/ Gás 500ml | FD c/ 12 | `7891234511019` | `17891234511016` |
| 05877 | Suco Uva Integral 1L | CX c/ 6 | `7891234522015` | `17891234522012` |
| 06120 | Leite Integral 1L | CX c/ 12 | `7899876500019` | `17899876500016` |
| 06430 | Sabão em Pó 1kg | CX c/ 10 | `7894455000012` | `17894455000019` |
| 07001 | Biscoito Recheado 140g | CX c/ 30 | `7890000111222` | `17890000111229` |
| 07002 | Biscoito Recheado 140g **Promo** | CX c/ 30 | `7890000111222` | `17890000111229` |

Os dois últimos compartilham o mesmo código **de propósito** — é o caso de leitura ambígua.

Tanto o EAN-13 quanto o DUN-14 resolvem: o operador pode bipar a unidade ou a caixa.

---

## Roteiro 1 · Cleber confere a carga da doca 1

Entre com **04127** / **mundial**.

O painel abre com as quatro docas. Clique na **doca 1** — `000148372/1`, Bebidas Primavera.

Ao abrir, aparece o aviso **"Este Documento já foi lançado! Confirma assim mesmo?"**, porque três
itens já têm quantidade. É aviso, não bloqueio.

O cursor já está no campo de leitura. **Não use o mouse** — a tela inteira funciona por teclado.

| # | Digite e tecle Enter | O que deve acontecer |
| --- | --- | --- |
| 1 | `7891234522015` | Aceita. Selo verde, "SUCO UVA INTEGRAL 1L", bipe curto. Este item está **pendente** e tem 0 lançado. Digite `24` e Enter — a linha vira `ok` e some a marca de pendência. |
| 2 | `7891234567897` | Aceita, mas avisa: **"Este Código já tem Qtde lançada (40)! Deseja lança-lo assim mesmo?"** Confirme com **Sim**, digite `38`, Enter. O valor **substitui** — vira 38, não 78. A linha passa a mostrar divergência `-2`. |
| 3 | `7899999000123` | **Recusa.** Som diferente, selo vermelho, "Código Não cadastrado!". Como o Cleber não tem permissão de inclusão, **não** aparece oferta de cadastrar. |
| 4 | `7894455000012` | **Recusa.** "Código Não cadastrado para BEBIDAS PRIMAVERA LTDA!" — o sabão existe, mas não está nesta nota. |
| 5 | `7890000111222` | **Recusa por ambiguidade.** O painel lista os dois candidatos: `07001` e `07002`. O sistema não escolhe por você. |
| 6 | Tecle `F2` | Pergunta **"Finalizar conferência?"**. Confirme. O documento fecha, a tela ganha faixa âmbar de somente leitura, e o rodapé registra quem fechou. |

**O que observar durante:**

- O resultado de cada leitura aparece **sempre no mesmo lugar** — coluna da direita, mesma altura.
  Nunca procure na tela.
- Acerto e recusa têm **sons diferentes**. Dá para silenciar no botão do topo.
- A linha do item lançado desliza para a posição; o **número da quantidade nunca anima**.
- Depois de cada lançamento o foco **volta sozinho** ao campo de leitura.

---

## Roteiro 2 · A carga atrasada da doca 2

Ainda como Cleber, volte ao painel (botão **Docas**) e clique na **doca 2** — `000147901/1`,
Laticínios Serra Azul.

Antes de entrar, repare no cartão: **anel âmbar**, pílula `atrasada`, e o tempo em vermelho. Ele
está aberto há mais de três horas.

| Digite | Resultado |
| --- | --- |
| `7899876500019` | Leite Integral. Já tem 60 de 200 — pede confirmação. Confirme e lance `200`. A divergência some. |
| `7891234511019` | Água Mineral, zerada. Aceita direto, sem confirmação. Lance `80`. |
| `7894455000012` | Sabão em Pó, zerado. Aceita direto. Lance `45` — repare que a nota pede 50, então a linha marca divergência `-5`. |

Não finalize. Volte ao painel e veja o cartão da doca 2 com a barra de progresso cheia.

---

## Roteiro 3 · O que a permissão bloqueia

Saia e entre com **04310** / **mundial** (Rosana).

Abra a **doca 3** — `000148415/2`, que está aguardando, com nada lançado.

| Digite | Diferença em relação ao Cleber |
| --- | --- |
| `7899999000123` | Recusa igual, **mas agora aparece "Código Não cadastrado! Deseja Cadastrar agora?"** — a Rosana tem permissão de inclusão. |

Essa é a única diferença visível entre os dois perfis nas telas de hoje. A oferta leva ao cadastro
de códigos, que ainda **não tem tela** — veja a seção seguinte.

Depois teste **05001** / **mundial**: o acesso é negado com **"Você não está autorizado a usar
este Sistema"**, antes de qualquer tela.

E teste as recusas de login:

| Matrícula | Senha | Mensagem esperada |
| --- | --- | --- |
| `99999` | qualquer | "Matrícula não cadastrada! Favor contactar supervisor" |
| `04127` | `errada` | "Senha inválida" |

---

## Roteiro 4 · Documento fechado é intocável

Volte ao painel e olhe a **doca 4** — `000147744/1`. O cartão está apagado e **não é clicável**.

Para ver a tela em modo leitura, abra direto:

```
http://localhost:3000/conferencia/000147744%2F1
```

A faixa no topo diz **"Este Documento já foi conferido! Fechado por 04127 — somente leitura."**, o
campo de leitura some, e não há botão de finalizar. Não é um botão cinza escondido no meio: a tela
inteira muda de modo.

---

## Roteiro 5 · Dois operadores no mesmo documento

Precisa de duas janelas — uma normal e uma anônima, para as sessões não se misturarem.

1. Janela A: entre como **04127**, abra a doca 1, bipe `7891234567897`. **Pare aí** — não lance ainda.
2. Janela B: entre como **04982**, abra a **mesma** doca 1, bipe o mesmo código e lance `50`.
3. Volte à janela A e lance `30`.

A janela A recebe uma faixa vermelha no topo: **"Outro operador lançou este item enquanto você
trabalhava. Recarregue e confira o valor atual."** A tela recarrega sozinha e mostra `50` — o valor
que B gravou. Os `30` de A **não** entram, e não há botão de forçar.

Pela API o mesmo caso devolve **HTTP 409** com `problem+json`:

```bash
# leia o documento e guarde a "versao" do item que vai lançar
curl -s "http://localhost:5001/api/conferencia?documento=000148372%2F1" \
  | python3 -c "import sys,json;print([i for i in json.load(sys.stdin)['itens'] if i['codigo']=='04127'][0]['versao'])"

# lance com uma versão velha — devolve 409
curl -s -X POST "http://localhost:5001/api/conferencia/lancamentos?documento=000148372%2F1" \
  -H 'Content-Type: application/json' \
  -d '{"codigo":"04127","quantidade":30,"matricula":"04127","confirmado":true,"versao":"AAAAAAAAAAA="}'
```

---

## O que só existe pela API

Estas partes têm regra implementada e testada, mas **ainda não têm tela**. Use `curl` ou o
navegador.

### Cadastro de códigos DUN-14

```bash
# ver o produto
curl -s "http://localhost:5001/api/produtos/04127" | python3 -m json.tool

# código que já pertence a outro produto — recusa nomeando o dono
curl -s -X PUT "http://localhost:5001/api/produtos/04127/codigos" \
  -H 'Content-Type: application/json' \
  -d '{"dun":["17891234500010","",""],"matricula":"04310","confirmado":false}'
# → "Código já cadastrado para o Produto 04982 — CERVEJA PILSEN LATA 350ML"

# apagar um código pede confirmação
curl -s -X PUT "http://localhost:5001/api/produtos/04127/codigos" \
  -H 'Content-Type: application/json' \
  -d '{"dun":["","",""],"matricula":"04310","confirmado":false}'
# → "Tem certeza que deseja excluir este código?"

# repetir o mesmo código em dois slots do mesmo produto — recusa
curl -s -X PUT "http://localhost:5001/api/produtos/04127/codigos" \
  -H 'Content-Type: application/json' \
  -d '{"dun":["17891234567894","17891234567894",""],"matricula":"04310","confirmado":false}'
# → "Este Código já esta cadastrado"
```

### Etiqueta ZPL

```bash
curl -s "http://localhost:5001/api/produtos/04127/etiqueta" \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['zpl'])"
```

Saída, byte a byte como o legado monta:

```
^XA
^FO510,40^A0R,150,36^FDREFRIGERANTE COLA 2L^FS
^FO420,360^A0R,100,50^FDCX c/ 6^FS
^FO220,270^BCR,200,Y,N,N^FD17891234567894^FS
^FO110,335^A0R,55,35^FD17891234567894^FS
^XZ
```

Passe `?codigoBarras=7891234567897` para ver a diferença: com 13 dígitos o código de barras vai
para `^FO210`, com 14 vai para `^FO220`. É o que o legado faz.

### Lista de conferências

```bash
curl -s "http://localhost:5001/api/conferencias?pagina=0&tamanho=10" | python3 -m json.tool
curl -s "http://localhost:5001/api/conferencias?busca=PRIMAVERA" | python3 -m json.tool
```

### Códigos à mão

```bash
curl -s "http://localhost:5001/api/demo/codigos" | python3 -m json.tool
```

Também aparece como painel na parte de baixo da tela de conferência: clique num código e ele é
enviado ao campo de leitura como se tivesse sido bipado.

### Trilha de auditoria

```bash
docker run --rm --network poc-mundial_default mcr.microsoft.com/mssql-tools \
  /opt/mssql-tools/bin/sqlcmd -S db -U sa -P 'Mundial#2026Dev' -d sgm -W -s '|' \
  -Q "SELECT TOP 10 data_eve, usuario, RTRIM(arquivo), chave, val_ant, val_atu
      FROM log_even ORDER BY id DESC"
```

O formato é o do legado, recuperado da função `reg_log` do FoxPro: valor anterior e valor atual,
campo a campo.

---

## Verificar as regras

```bash
./tools/verificar.sh
```

Roda os 90 testes e imprime o relatório de rastreabilidade `ruleKey → teste`. Cada regra recuperada
do legado carrega sua chave `RK-…`, e o relatório falha se alguma regra implementada ficar sem
teste.

Para conferir uma regra específica contra a fonte original, use o MCP do RNC:
`getRule(workspaceId, "RK-8233e231d6fb")`.

---

## O que ainda não existe

Para não gastar seu tempo procurando:

| Não existe | Onde está a regra |
| --- | --- |
| Tela de cadastro de códigos DUN-14 | API e testes; mock em `mockups/superficies-restantes.html` |
| Tela de prévia da etiqueta | API e testes; mesmo mock |
| Tela de consultas do supervisor | API `/api/conferencias`; mesmo mock |
| Tela de auditoria | grava certo; só se lê pelo banco |
| Botão de reset da demonstração | apague as tabelas e reinicie a API |
| Expiração de sessão por inatividade | especificada, não implementada |
| Cadastro de fornecedor | fora do escopo do POC por decisão; as 13 regras estão testadas |
| Envio para impressora física | fora do escopo; o ZPL é gerado e conferível |

---

## Se algo não funcionar

| Sintoma | Causa provável |
| --- | --- |
| Painel de docas vazio | O seed não rodou. `docker compose logs api \| grep -i semea` |
| "Address already in use" na porta 5001 | Outra coisa usando a porta. Mude `PORTA_API` no `.env`. |
| Banco não sobe | Em Apple Silicon o SQL Server 2022 não roda. O `.env` já vem com `DB_IMAGE` apontando para o Azure SQL Edge. |
| Erro de CORS no navegador | `ORIGEM_WEB` no `.env` precisa bater com a URL que você abriu. |
| Login não responde | A API pode estar esperando o banco. `docker compose logs api` |

---

## Capturar as telas de novo

Os slides com as 18 telas estão em `_bmad-output/implementation-artifacts/slides-testes.html`.
Para regerá-los depois de mudar a interface:

```bash
# 1. estado limpo
docker run --rm --network poc-mundial_default mcr.microsoft.com/mssql-tools \
  /opt/mssql-tools/bin/sqlcmd -S db -U sa -P 'Mundial#2026Dev' -d sgm \
  -Q "DELETE FROM conferencia; DELETE FROM acesso; DELETE FROM estoq;
      DELETE FROM forne; DELETE FROM usuario;"
docker compose restart api && sleep 15

# 2. capturar (o navegador roda dentro da rede do compose)
docker run --rm --network poc-mundial_default \
  -v "$PWD/tools/capturar.mjs:/app/capturar.mjs" \
  -v "$PWD/tools/capturas:/saida" \
  -w /app mcr.microsoft.com/playwright:latest \
  sh -c "npm i playwright@1.46.1 >/dev/null 2>&1 && node capturar.mjs"

# 3. montar os slides
python3 tools/montar-slides.py
```

O script `tools/capturar.mjs` percorre os mesmos passos deste roteiro. Se um passo mudar aqui,
mude lá também — as legendas dos slides saem de dentro dele.
