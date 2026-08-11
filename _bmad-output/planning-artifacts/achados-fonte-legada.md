# Achados na fonte legada — leitura direta via MCP do RNC

**Data:** 2026-08-10
**Workspace:** `b2e707a2-7df1-4156-b9ad-a35ed33e2e78` · retenção `RETAINED` · 11 arquivos
**Objetivo:** resolver Q-1 e Q-4 do PRD sem supor.

Resolveu as duas, e derrubou quatro coisas que estavam erradas nos documentos anteriores.

## Arquivos disponíveis

| Arquivo | O que é |
| --- | --- |
| `readme.txt` | descrição do sistema pelo cliente |
| `src/conferencia.PRG` | programa principal — menu, log, erro, impressora |
| `src/conferencia.BAK` | idêntico ao PRG menos um comentário |
| `src/conferencia.LST` | lista dos arquivos do projeto |
| `database/DBFs/estoq_structure.TXT` | estrutura completa do DBF de produtos |
| `database/SQL Scripts/Create Table - {Acesso,Conferencia,Forne,Usuario}.sql` | DDL real, SQL Server |

**Os `.SCX` não foram retidos.** `Rec_nf.scx`, `Dun14.scx`, `Log_conf.scx` e `Menu_conf.scx` são
binários do Visual FoxPro — a lógica dos formulários só existe através das 70 regras que o RNC
extraiu. Isso limita o que dá para confirmar na fonte.

---

## O que mudou de entendimento

### F-1 — O legado já é SQL Server, não só DBF

Os quatro DDLs começam com `USE [sgm]`, gerados em **21/10/2025**. `acesso`, `conferencia`,
`forne` e `usuario` já vivem em SQL Server. Só `estoq` continua em DBF, num share de rede
(`\\10.1.1.9\estoq200`, definido em `conferencia.PRG`).

**Consequência:** a migração não é DBF → SQL Server. É SQL Server → SQL Server para quatro tabelas,
com o DDL de origem em mãos, e DBF → SQL Server só para `estoq`. Muito menos risco do que o
planejamento assumia.

### F-2 — `estok_sgm` é o próprio banco, não um ERP externo

O banco se chama `sgm`. As duas "conexões ODBC para `estok_sgm`" que o UIR marcou como
`logicOpaque` são a conexão do FoxPro com este mesmo SQL Server.

**Consequência:** o AD-14 tratava `estok_sgm` como sistema externo desconhecido a isolar atrás de
anticorruption layer. Não é. A suposição A-5 (de que `entrada` e `esco_imp` pertenciam a um ERP
externo) está **errada** — ver F-3.

### F-3 — `esco_imp` é "escolhe impressora"

```foxpro
Proced esco_imp
  Parameter acao
  =Aprinters(gaprinters)
  ...
  nom_imp=Getprinter( )
```

Não é importação de estoque. É a rotina que seleciona a impressora Windows, com preferência por
nome contendo `(R`. Chamada no boot do sistema.

### F-4 — `acesso.arquivo` é nome de **tabela**, não de tela

Do `readme.txt`, escrito pelo cliente:

> `acesso`: … onde "arquivo" seria o **nome da tabela** que seria acessada para definir as
> permissões por usuário e "matric" seria a matrícula interna do usuário na empresa.

O DDL confirma: `[arquivo] [char](10) NOT NULL`. *(Dez caracteres acomodam `estoq`, `forne`,
`acesso`, `usuario` e `log_even` — mas **não** `conferencia`, que tem 11. Ver F-9.)*

**Consequência:** AD-8 e FR-5 diziam "`arquivo` identifica a tela". Errado — a permissão é **por
tabela**. Uma tela que lê duas tabelas exige as duas permissões.

### F-5 — `conferencia` é **item de nota**, não a nota

A PK composta inclui `codigo` — o código do produto:

```sql
PRIMARY KEY CLUSTERED (filial, orig_des, tipo_doc, SERIE, numero, codigo)
```

E `estoq.CODIGO` é `Character(5)`, mesmo tipo e tamanho de `conferencia.codigo` `char(5)`.

Ou seja: **documento** = `filial + orig_des + tipo_doc + SERIE + numero`; cada produto do documento é
uma linha. `itnf` é o número do item na nota. `qtd_nf` e `qtd_rec` são daquele item.

**Consequência:** "finalizar conferência" age sobre **todas as linhas do documento**, não sobre uma.
O agregado do AD-10 é o documento, não a linha.

### F-6 — As linhas de `conferencia` nascem antes da conferência

Do `readme.txt`, os passos que antecedem o uso:

> 1. Emissão de nota fiscal pelo fornecedor; 2. Recepção do veículo; 3. Validação da nota fiscal;
> 4. **Integração da nota fiscal (a partir deste processo nascem os registros na tabela Conferencia)**

**Consequência:** o sistema **nunca cria** conferência. Ela chega pronta com `qtd_nf` preenchida, e o
operador só preenche `qtd_rec`. Confirma FR-8 e FR-9, e explica por que não existe tela de "nova
conferência" no legado.

### F-7 — `timer1` não é lixo de formulário

```foxpro
Define Class ShutTimer As Timer
	Interval = 14400000
	Procedure Timer
	If ( Datetime()- m.horaUltUso)*1000 > This.Interval * 2
		Keyboard '{ALT+I}'
		Keyboard '{ALT+R}'
```

É **encerramento de sessão por inatividade**. O relatório de divergência classificou `timer1` como
controle de formulário sem valor — verdade quanto a não ser coluna, falso quanto a não ter função.

**Consequência:** requisito funcional que nenhum documento capturou.

### F-8 — O schema de `log_even` está no código

O UIR devolveu a tabela com zero colunas. A função `reg_log` do `conferencia.PRG` grava:

| Coluna | Conteúdo |
| --- | --- |
| `data_eve` | `Datetime()` |
| `chave` | valor da chave primária do registro alterado |
| `arquivo` | nome da tabela (`Alias()`) |
| `val_ant` | valor anterior — texto multilinha `campo = valor` |
| `val_atu` | valor atual, ou `'Registro Excluido'` |
| `usuario` | `m.usuario` |

Semântica: inclusão grava `val_ant = 'Registro Incluido'`; exclusão grava
`val_atu = 'Registro Excluido'`; alteração grava só os campos que mudaram.

**Consequência:** a suposição A-1 morre. O schema é recuperado, não inventado — e o FR-41 inventava
campos que não existem (`ação`, `origem`).

### F-9 — `acesso.arquivo` é curto demais para `conferencia`

Descoberto ao semear o banco: o `INSERT` falhou com *"String or binary data would be truncated in
table 'sgm.dbo.acesso', column 'arquivo'. Truncated value: 'conferenci'"*.

`acesso.arquivo` é `char(10)`, e **`conferencia` tem 11 caracteres**. O F-4 afirmava que a largura
acomodava os nomes das tabelas — não acomoda o mais importante deles.

Todas as outras cabem: `estoq` (5), `forne` (5), `acesso` (6), `usuario` (7), `log_even` (8).

**Consequência:** o legado nunca conseguiu guardar o nome inteiro. A chave de permissão é o nome
truncado em 10 caracteres, e isso virou `Tabelas.Chave()` no domínio. Registrado como **Q-10**:
confirmar com a Mundial se o legado grava `conferenci` ou outro identificador.

Este achado só apareceria em execução — nenhuma leitura de documento o pegaria.

---

## Q-4 — resolvida: `barr_emb3` existe

De `estoq_structure.TXT`:

| # | Campo | Tipo | Largura |
| --- | --- | --- | --- |
| 1 | `CODIGO` | Character | 5 |
| 2 | `DESCRI` | Character | 60 |
| 12 | `CODBARR` | Character | 13 |
| 13 | `CODBARR2` | Character | 13 |
| 14 | `CODBARR3` | Character | 13 |
| 15 | `BARR_EMB` | Character | 14 |
| 16 | `BARR_EMB2` | Character | 14 |
| 17 | `BARR_EMB3` | Character | 14 |
| 21 | `EMBALAG` | Character | 10 |
| 22 | `EMBALQT` | Numeric | 9,4 |

`BARR_EMB3` existe e é **Character(14)**, não `Decimal`. A suposição A-3 morre, e o
`getErModel` estava incompleto — devolvia 6 colunas com tipo `UNKNOWN`.

**Também resolve Q-3.** E revela a simetria que explica o domínio: **três slots de EAN-13**
(`CODBARR*`, 13 dígitos, unidade de venda) e **três slots de DUN-14** (`BARR_EMB*`, 14 dígitos,
embalagem). É exatamente o par que a regra `RK-3b8ef53b6cf2` compara.

**`estoq` tem 116 campos**, não 6. É a tabela mestre de produtos do supermercado; a tela `Dun14.scx`
edita seis deles.

---

## Q-1 — sem prova direta, mas com evidência forte: **substitui**

A lógica de `Rec_nf.scx` é binária e não foi retida. Não há prova textual. O que a estrutura diz:

1. **`qtd_rec` é campo escalar de um item de nota** (`decimal(10,3)`, default 0), ao lado de um
   `qtd_nf` já preenchido pela integração. É um valor conferido, não um acumulador de eventos.
2. **Não existe tabela de lançamentos.** Nada no schema registra bipagens individuais — só o total
   por item. Um modelo de acúmulo normalmente tem onde acumular.
3. **O aviso só faz sentido protegendo ação destrutiva.** `RK-8233e231d6fb` dispara em
   `qtd_rec > 0` e `RK-5960908935ee` pergunta "Deseja lança-lo assim mesmo?", abortando em "Não".
   Somar não é destrutivo — não precisaria de confirmação. Substituir apaga a contagem anterior, e
   aí a pergunta protege alguma coisa.
4. **`Set Exclusive On`** no `conferencia.PRG` — o legado abre as tabelas em modo exclusivo. Dois
   operadores no mesmo documento simultaneamente não era cenário possível, o que enfraquece a
   hipótese de acúmulo por vários operadores.

**Decisão:** inverter para **substitui**, com confirmação explícita. Reversível — se alguém da
Mundial confirmar acúmulo, muda `FR-18` e `AD-17` juntos.

A jornada UJ-1 do PRD, que narrava "confirma que quer somar porque o palete veio dividido", era
invenção minha e foi corrigida.

---

## Ainda em aberto

| # | Pergunta | Por que a fonte não resolveu |
| --- | --- | --- |
| Q-1 | soma ou substitui | lógica em `.SCX` binário |
| Q-2 | o que são os níveis de `usuario.niv_usu` | `nchar(1)`, valores não documentados |
| Q-6 | existe tolerância de divergência | não aparece no schema nem no PRG |
| Q-7 | destino do item pendente no fechamento | `pendencia bit` existe, semântica não |
| Q-9 | quantas docas | `doca int`, sem domínio declarado |
| Q-10 | o que o legado grava em `acesso.arquivo` para a tabela `conferencia`, que não cabe em `char(10)` | descoberto em execução; ver F-9 |
