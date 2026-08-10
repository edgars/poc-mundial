---
title: 'PRD — Mundial · Conferência de Recebimento'
status: final
created: '2026-08-10'
updated: '2026-08-10'
stakes: 'POC demonstrável com ambientação de armazém de entrada'
sources:
  - 'RNC UIR — workspace b2e707a2-7df1-4156-b9ad-a35ed33e2e78, módulo dun14.SCX (FOXPRO)'
  - 'ARCHITECTURE-SPINE.md — 20 ADs, status final'
  - 'uir-gap-report.md — 7 divergências'
---

# PRD: Mundial · Conferência de Recebimento

## 0. Propósito do documento

Este PRD substitui o `docs/prd.md` gerado automaticamente, que estava truncado, cobria 2 das 6
entidades reais e descrevia dois CRUDs no lugar do processo operacional.

Os requisitos vêm do UIR do RNC — telas, `dataBindings`, modelo ER e as 70 regras com suas chaves
`RK-…`. Nenhum requisito de negócio foi inventado. Onde o UIR é ambíguo e não há ninguém para
perguntar, existe uma suposição marcada `[ASSUMPTION]` e indexada na seção 10.

**Calibração: POC demonstrável.** Ninguém opera carga de verdade. O produto precisa **parecer** o
chão de armazém e rodar o fluxo inteiro de ponta a ponta numa apresentação, com dado semeado.

Isso não é a mesma coisa que "POC frouxo". Um POC que prova o caminho do RNC pode ser feio; este
precisa convencer quem assiste de que o operador trabalharia ali. Por isso os requisitos de
percepção (seção 8) continuam firmes enquanto os de operação real saíram — o registro completo da
troca está na seção 12.

**Invenção autorizada.** A seção 4.7 introduz uma tela que o legado não tem. Está declarada como
andaime de demonstração, não como equivalência funcional.

## 1. Visão

Um operador de doca hoje confere carga numa tela de Visual FoxPro escrita em 2011, remendada até
2021, que só roda em desktop, guarda senha em `char(6)` texto puro e lê os produtos de um DBF num
share de rede. O processo em si é
bom: bipa o documento, bipa o produto, conta, fecha, imprime etiqueta.

A visão é preservar esse processo inteiro — inclusive as decisões que o legado toma no meio dele —
sobre uma base que a Mundial consiga manter, auditar e evoluir. O sucesso não é "um sistema novo".
É **um operador que faz a mesma conferência do mesmo jeito, sem treinar de novo**, e um supervisor
que pela primeira vez consegue responder "o que foi conferido ontem, por quem".

## 2. Usuário-alvo

Dois papéis, ambos internos, ambos já existem no legado (`usuario.niv_usu` distingue).

**Operador de recebimento.** Fica na doca. Trabalha em pé, com coletor de código de barras na mão
e caminhão esperando. Não tem paciência para navegação — o legado inteiro é operado por leitura de
código e tecla Enter. Qualquer passo a mais custa tempo de doca.

**Supervisor.** Libera exceção, cadastra código novo, confere o que a equipe fez. É quem hoje
recebe a ligação de "o código não está cadastrado, e agora?".

Na demonstração existe um terceiro papel, fora do produto: **quem apresenta**. A seção 4.8 atende a
essa pessoa e some quando o POC virar produto.

### 2.1 Jobs To Be Done

| Quando… | eu quero… | para que… |
| --- | --- | --- |
| chega um caminhão na doca | registrar o que veio, item a item, sem sair do coletor | a carga entre no estoque com a contagem certa |
| o código bipado não é reconhecido | resolver na hora, sem parar o caminhão | a doca não trave esperando cadastro |
| a quantidade recebida difere da nota | registrar a divergência em vez de forçar o número | o financeiro trate a diferença com o fornecedor |
| termino a conferência | fechar o documento de forma irreversível | ninguém altere a contagem depois |
| preciso identificar uma embalagem | imprimir a etiqueta com o código de barras | o produto seja rastreável no armazém |

### 2.2 Não-usuários (v1)

Fornecedor, transportadora e financeiro **não** acessam o sistema. Recebem o resultado por outros
meios. O legado também não os atendia.

### 2.3 Jornadas

**UJ-1 · Cleber confere uma carga de bebida.**
Cleber assume a doca 3 às 6h. Loga com a matrícula e a senha. O painel mostra as quatro docas; a 3
tem um documento aguardando. Ele entra e bipa o documento — o sistema traz o fornecedor e os itens
esperados. Bipa o EAN-13 da primeira caixa: o sistema resolve o DUN-14, mostra "Refrigerante Cola
2L", embalagem "CX c/ 6", e dá um bipe curto. Ele digita 40 e confirma. Bipa o próximo. Na terceira
leitura o sistema avisa que aquele código já tem 12 lançadas e pergunta se ele quer substituir;
Cleber conferiu de novo e confirma 18. Ao final manda finalizar, o sistema pergunta uma vez, ele confirma, e
o documento fecha. Total: 22 minutos, sem tirar a mão do coletor.

**UJ-2 · Cleber bate num código que não existe.**
Quarta leitura: som de erro, diferente do bipe de acerto, e "Código não cadastrado". Hoje ele liga
para a Rosana. No sistema novo vê a mesma mensagem e um caminho: se tiver permissão de inclusão,
cadastra o DUN-14 ali mesmo; se não tiver, marca o item como pendente e segue com o resto da carga
em vez de travar o caminhão.

**UJ-3 · Rosana revisa o turno.**
7h. Rosana abre o painel. Vê as docas, o que fechou, o que está aberto há mais tempo e quais
conferências têm divergência entre a quantidade da nota e a recebida. Abre uma delas e vê item a
item. Não consegue alterar nada — está fechada. É a primeira vez que ela tem isso.

## 3. Glossário

| Termo | Significado |
| --- | --- |
| **DUN-14** | Código de barras de 14 dígitos da *embalagem* (caixa, fardo). Coluna `conferencia.dun14`. |
| **EAN-13** | Código de barras de 13 dígitos da *unidade de venda*. Um DUN-14 agrupa N EAN-13. |
| **Doca** | Baia de descarga. `conferencia.doca`. |
| **Documento** | Nota fiscal ou romaneio da carga. Identificado por `filial + orig_des + tipo_doc + SERIE + numero` — é o agregado. O número que o operador bipa fica em `conferencia.acesso` (nome herdado do legado; nada a ver com permissão). |
| **Matrícula** | Identificador do funcionário. `usuario.matric`. É o login. |
| **ZPL** | Zebra Programming Language — texto que comanda a impressora de etiquetas. |
| **Regra `RK-…`** | Chave estável de uma regra recuperada pelo RNC. Verificável por `getRule()`. |
| **Fechar / finalizar** | Tornar o documento imutável. `fechado = 1` em todas as suas linhas. |
| **Item de conferência** | Uma linha de `conferencia` — o documento mais `codigo` do produto. `qtd_nf` e `qtd_rec` são do item. |
| **Divergência** | Diferença entre quantidade da nota e quantidade recebida. Dado, não erro. |
| **Andaime de demo** | Funcionalidade que existe só para a apresentação e sai quando o POC virar produto. |

## 4. Features

### 4.1 Autenticação e permissão por tabela

O legado autentica por matrícula + senha e controla permissão em `acesso(matric, arquivo)` com
quatro flags, onde `arquivo` é o nome da tabela. Nenhum dos dois existia nos documentos gerados.

- **FR-1** — O sistema autentica por **matrícula e senha**.
  *Aceite:* matrícula inexistente devolve "Matrícula não cadastrada! Favor contactar supervisor" (`RK-046f5592ef5b`). Senha errada devolve "Senha inválida" (`RK-f8293cf9dbb3`). Nenhuma das duas revela qual dos dois campos falhou além do texto legado.
- **FR-2** — O sistema **recusa acesso** a usuário sem nível suficiente, com "Você não está autorizado a usar este Sistema" (`RK-8ffd715ce9ad`, condição legada `vsenha < 3`).
- **FR-3** — Na definição ou troca de senha, o sistema **exige confirmação** e recusa quando os dois campos divergem: "Você deve Confirmar a senha" (`RK-58fefec22db6`).
- **FR-4** — Toda senha é armazenada com **hash**, inclusive as dos usuários semeados. *(AD-7.)* Nenhum caminho da aplicação lê senha em claro.
- **FR-5** — O sistema aplica **permissão por tabela** com as quatro operações do legado: consultar, incluir, alterar, excluir (`RK-04c918661d8d`, `RK-6022cae899fa`, `RK-fa1ca141cf21`, `RK-be780ff12c0e`). `acesso.arquivo` é o **nome da tabela**, não da tela — uma tela que toca duas tabelas exige as duas permissões. Ausência de registro em `acesso` significa **negado**. *(AD-8.)*
- **FR-6** — A permissão é decidida **no servidor**. A interface esconde o que o usuário não pode fazer, mas isso é conveniência, não controle. *(AD-8)*
- **FR-7** — `usuario.nome` e `acesso.descri` são **obrigatórios** (`RK-d1a55f1103db`, `RK-ea5a22eaf219`).
- **FR-54** — A sessão **expira por inatividade** e devolve o usuário ao login. O legado faz isso com um `Timer` global (`ShutTimer` em `conferencia.PRG`) que encerra a aplicação após inatividade prolongada — é o que o campo `timer1` da tela representa. Requisito real que nenhum documento anterior capturou.
  *Aceite:* período configurável; o padrão reproduz a ordem de grandeza do legado (horas, não minutos). Conferência aberta não perde lançamento já gravado.

### 4.2 Conferência de recebimento — o núcleo

Esta é a feature que o produto existe para entregar. Tela legada "Conferência de Recebimento de
Mercadoria", 12 campos, 2 ações.

**Abertura do documento**

- **FR-8** — O operador **seleciona a doca** e **bipa ou digita o documento**. O sistema localiza o documento e seus itens.
  *Nota:* o sistema **nunca cria** linha de `conferencia`. Elas nascem da integração da nota fiscal, antes da conferência, com `qtd_nf` já preenchida — confirmado no `readme.txt` do cliente. No POC, quem cumpre esse papel é o seeder (FR-49).
- **FR-9** — Documento inexistente é recusado: "Documento não cadastrado!" (`RK-c0fce5362f62`).
- **FR-10** — Documento já fechado é recusado para edição: "Este Documento já foi conferido!" (`RK-ff51aa26bf33`, `RK-69b41cd017dd` — condição `fechado = .T.`).
- **FR-11** — Documento já lançado gera **aviso com confirmação**, não bloqueio: "Este Documento já foi lançado! Confirma assim mesmo?" (`RK-cc8cfa3658d1`, `RK-45e526801fea`). Se o operador recusa, a operação para.
- **FR-12** — Fornecedor diferente do esperado gera **aviso com confirmação**: "Fornecedor diferente! Confirma este fornecedor?" (`RK-a7f3c0eb65c1`).

**Lançamento de item**

- **FR-13** — O operador **bipa o EAN-13**; o sistema resolve o **DUN-14** correspondente e exibe descrição e embalagem a partir de `estoq`.
  *Aceite:* a resolução devolve **exatamente um** DUN-14. Como um produto aceita três códigos de embalagem (FR-30), o código bipado pode casar com mais de um registro — nesse caso o sistema recusa a leitura e exibe os candidatos, em vez de escolher um. Zero resultados cai em FR-14.
- **FR-14** — Código não reconhecido é recusado com "Código Não cadastrado!" e a oferta de cadastrar na hora (`RK-6fef4d31a290`, `RK-798f00f19690`, `RK-dab7d2033e2e`). A oferta só aparece para quem tem permissão de inclusão (FR-5).
- **FR-15** — Código não cadastrado **para aquele fornecedor** é recusado: "Código Não cadastrado para…" (`RK-732bb9300bad`).
- **FR-16** — EAN-13 que não pertence ao DUN-14 informado é recusado: "Código EAN não é desse DUN-14!" (`RK-3b8ef53b6cf2`).
- **FR-17** — Código que já tem quantidade lançada gera **aviso com confirmação** mostrando o valor atual: "Este Código já tem Qtde lançada (n)! Deseja lançá-lo assim mesmo?" (`RK-8233e231d6fb`, `RK-5960908935ee`).
- **FR-18** — Quantidade lançada **substitui** o valor de `qtd_rec` daquele item; quando já havia valor, só grava após a confirmação do FR-17, e a recusa aborta sem gravar. *(AD-17.)* **Decisão provisória** — a lógica está em `.SCX` binário, não retido. A evidência estrutural aponta para substituição: `qtd_rec` é campo escalar do item, não há tabela onde acumular, e o aviso só protege se a ação for destrutiva. Ver Q-1.
- **FR-19** — Exclusão de lançamento pede confirmação: "Confirma Exclusão?" (`RK-bdfbdff6c821`).
- **FR-20** — O sistema registra separadamente **quantidade da nota** e **quantidade recebida**, em embalagem e em unidade — `qtd_nf`, `qtd_unid_nf`, `qtd_rec`, `qtd_unid_rec`. A diferença entre elas é a **divergência** e é dado de primeira classe, não erro. *(Quatro colunas que os documentos gerados ignoravam por completo.)*
- **FR-21** — `peso_bruto_col` e `balanca` são **obrigatórios** na conferência (`RK-82c929f4e851`, `RK-c5a64175c9a1`), e `situacao` também (`RK-16bc1acd7b74`).

**Fechamento**

- **FR-22** — Finalizar pede **uma confirmação explícita**: "Finalizar conferência?" (`RK-fa93a48fbecc`).
- **FR-23** — Ao finalizar, o sistema grava em **transação única**: `fechado`, `matr_fec` (quem fechou), `dt_hora` e `situacao`. *(AD-10.)*
- **FR-24** — Depois de fechada, a conferência é **imutável**. Nenhuma operação de edição a atinge.
- **FR-25** — Dois operadores no mesmo documento: o segundo a gravar recebe **conflito** e revê o estado atual, em vez de sobrescrever a contagem do primeiro. *(AD-17.)*

**Consulta**

- **FR-26** — Lista de conferências com busca e paginação, mostrando situação, doca, documento, data e se há divergência. *(AD-15 fixa o contrato.)*
- **FR-27** — Detalhe da conferência mostra os itens lançados, quem conferiu (`matr_conf`), quem fechou (`matr_fec`) e quando.

### 4.3 Cadastro de códigos DUN-14

Tela legada "Cadastro de Codigos Dun-14". Vinte das setenta regras vivem aqui — é a área mais
regrada do sistema, porque código de barras duplicado corrompe toda conferência futura.

Cada produto aceita **até três códigos de embalagem**: `barr_emb`, `barr_emb2`, `barr_emb3`. As
regras se repetem para os três.

- **FR-28** — Cadastro de produto com código, descrição, embalagem e quantidade por embalagem.
- **FR-29** — Produto inexistente é recusado: "Código não cadastrado!" (`RK-5a7aaaa8862d`, `RK-e84d750f340a`).
- **FR-30** — Um código de barras **não pode repetir dentro do mesmo produto**: "Este Código já esta cadastrado!" — validado nos três campos (`RK-a0bb1eeee55d`, `RK-99e9bfdcea75`, `RK-f9e0b12a76af`, `RK-4ca8df36a760`, `RK-41493150036e`, `RK-ab62193a2b2d`).
- **FR-31** — Um código de barras **não pode pertencer a outro produto**: "Código já cadastrado para o Produto…" (`RK-2976e3756f6d`, `RK-ab467d52fa1f`, `RK-f3bda1fa3b77`). A mensagem nomeia o produto que já usa o código.
- **FR-32** — Apagar um código existente pede confirmação: "Tem certeza que deseja excluir este código?" (`RK-5b2436bca3f0`, `RK-2c78478f0b97`, `RK-9f92b8e2a3c0`, e as confirmações `RK-ade9dd1661d1`, `RK-305af19071c6`, `RK-21ac9f1bddea`).
- **FR-33** — Limpar `barr_emb3` é transição de estado explícita (`RK-9f4468b42859`, `RK-75e2169fe930`, `RK-dfe2ca45ec1a`). `barr_emb3` é `Character(14)`, confirmado na estrutura do DBF.

### 4.4 Fornecedores — somente leitura no POC

O legado tem 46 colunas em `forne` e treze campos obrigatórios. Nenhum documento gerado mencionava
a entidade, embora quatorze das setenta regras sejam sobre ela.

**No POC não há tela de cadastro.** Fornecedores vêm semeados. As regras de obrigatoriedade
continuam **implementadas e testadas** — só não têm formulário que as dispare.

- **FR-34** — Consulta de fornecedor por código e por razão social.
- **FR-35** — As regras de obrigatoriedade de `forne` são implementadas na camada de aplicação e cobertas por teste, ainda que sem tela no POC. Treze campos:

  | Campo | Regra | Campo | Regra |
  | --- | --- | --- | --- |
  | `cgc` | `RK-b3e7fcc26f3e` | `cep` | `RK-4697ebd74678` |
  | `cod_com` | `RK-ef82abb7456c` | `cidade` | `RK-854f2452216e` |
  | `categ` | `RK-b5da8c743238` | `uf` | `RK-98835efbf746` |
  | `tiplog` | `RK-e74f29d4f922` | `inscr` | `RK-6aff3b12acb2` |
  | `lograd` | `RK-2ce1876d83ad` | `data_grav` | `RK-353ee013c009` |
  | `bairro` | `RK-1d4194439839` | `sub_trib` | `RK-37afeda868c2` |
  | `Mov_Est` | `RK-f2ca891c315f` | | |

- **FR-36** — O fornecedor da conferência é resolvido a partir do documento e comparado ao esperado (alimenta FR-12).

### 4.5 Etiqueta de embalagem

Sete regras montam uma etiqueta ZPL. Não são validações — são um **requisito de impressão** que os
documentos gerados classificavam como regra de negócio.

- **FR-37** — O sistema gera a etiqueta ZPL com: descrição do produto (`RK-0811a89bc8e6`), embalagem e quantidade por embalagem (`RK-2b3c11b27fef`), código de barras em duas posições (`RK-25721748a2b1`, `RK-3ff169d79617`), o código legível (`RK-1b386e3870da`), dentro dos delimitadores `^XA` / `^XZ` (`RK-b382d85d0edc`, `RK-e8876989538a`).
- **FR-38** — O layout gerado é **byte a byte compatível** com o do legado. Etiqueta é rastreabilidade física: uma mudança de posição invalida leitura no armazém.
- **FR-39** — O sistema **pré-visualiza a etiqueta na tela**, renderizando o ZPL gerado em imagem, com o texto ZPL disponível para inspeção. O envio para a impressora física fica fora do POC. *(Substitui o envio por socket; Q-5 deixa de ser bloqueador.)*
- **FR-40** — Falha na geração da etiqueta **não perde a conferência**: o lançamento permanece gravado e a etiqueta pode ser regerada.

### 4.6 Auditoria — somente leitura

- **FR-41** — Toda operação de escrita gera registro em `log_even` com o schema do legado, recuperado da função `reg_log` de `conferencia.PRG`: `data_eve` (instante), `usuario` (matrícula), `arquivo` (nome da tabela), `chave` (valor da chave primária), `val_ant` e `val_atu`. Inclusão grava `val_ant = 'Registro Incluido'`; exclusão grava `val_atu = 'Registro Excluido'`; alteração grava **apenas os campos que mudaram**, uma linha `campo = valor` por campo. Leitura não gera registro.
- **FR-42** — A trilha é **append-only**, consultável por período e por matrícula, e não é editável pela aplicação.

### 4.7 Painel de docas — andaime de demo

**Tela que o legado não tem.** Existe porque o legado abre direto no formulário de conferência, sem
contexto — o que funciona para quem trabalha ali todo dia e não funciona para quem assiste uma
demonstração. Declarada como andaime: sai quando o POC virar produto, ou vira feature própria com
requisito próprio.

Direção de UX na seção 8.1.

- **FR-43** — O painel mostra as **docas** e, em cada uma, o estado atual: livre, aguardando conferência, em conferência (com quem), ou fechada.
- **FR-44** — Cada doca ocupada mostra: documento, fornecedor, progresso da conferência (itens lançados sobre itens esperados) e **há quanto tempo está aberta**.
- **FR-45** — O painel destaca **exceção**, não estatística: conferência com divergência, conferência aberta há tempo demais, item pendente por código não cadastrado. Não há percentual de utilização — fluxo e gargalo, não medidor.
- **FR-46** — Clicar numa doca entra na tela de conferência daquele documento, já com o contexto carregado.
- **FR-47** — O painel atualiza sozinho, sem o usuário recarregar.
- **FR-48** — O painel **não cria nem altera dado de negócio**. É leitura sobre `conferencia`. Nenhuma regra `RK-…` depende dele.

### 4.8 Ambiente de demonstração — andaime de demo

Atende quem apresenta. Sai inteiro quando o POC virar produto.

- **FR-49** — O sistema sobe com **massa semeada** coerente: fornecedores, produtos com DUN-14 e EAN-13 que casam entre si, documentos abertos em docas diferentes, usuários com perfis distintos de permissão.
- **FR-50** — A massa inclui **estados de exceção plantados**, não só o caminho feliz: uma conferência já fechada, uma com divergência entre nota e recebido, um EAN sem cadastro, um código que existe em dois produtos, um usuário sem permissão de inclusão. São esses casos que demonstram as 70 regras.
- **FR-51** — **Reset da demonstração**: um comando restaura o estado semeado, para apresentar duas vezes seguidas sem dado sujo.
- **FR-52** — **Painel de códigos à mão**: quem apresenta vê a lista dos códigos semeados, com o que cada um provoca (este resolve, este não existe, este é ambíguo), e pode enviá-los para o campo de leitura com um clique.
- **FR-53** — O andaime é **isolado do domínio**: vive atrás de uma flag `MODO_DEMO`, não referencia regra de negócio e sua remoção não toca `Dominio` nem `Aplicacao`. *(AD-21.)*

## 5. Não-objetivos (explícitos)

| Não faremos | Por quê |
| --- | --- |
| Migrar o dado histórico do legado | Fora do escopo declarado. Exige janela de corte e validação com o cliente. |
| Substituir o ERP `estok_sgm` | O RNC marcou a integração como opaca — a lógica não foi recuperada. |
| Ligar a integração ODBC | Mesma razão. Fica atrás de feature flag desligada. *(AD-14.)* |
| Enviar etiqueta para impressora física | Sem hardware no POC. A geração do ZPL fica completa (FR-37, FR-38). |
| Tela de cadastro de fornecedor | 46 colunas para pouco valor de demonstração. As regras ficam implementadas (FR-35). |
| Portar as duas telas mortas do legado | `Form1` de menu e `commandgroup1` não carregam função. |
| SSO / login corporativo | O canvas RNC define `SSO: off`. |
| Funcionalidade de negócio nova | Equivalência primeiro. As seções 4.7 e 4.8 são andaime declarado, não negócio. |
| Aplicativo móvel nativo | O navegador atende. Nativo é outra decisão. |

**Duas regras sem FR, deliberadamente.** `RK-d0605132c1f1` e `RK-a709080069ea` ("Não foi possível
criar uma conexão ODBC!") são tratamento de falha da infraestrutura legada, não comportamento de
produto. Ficam cobertas pelo AD-14. Com elas, as 70 regras do UIR estão alocadas: 68 viram
requisito, 2 viram decisão de arquitetura.

## 6. Escopo do MVP

### 6.1 Dentro

Autenticação e permissão (4.1) · conferência de recebimento completa (4.2) · cadastro DUN-14 (4.3) ·
consulta de fornecedor (4.4) · geração e pré-visualização de etiqueta (4.5) · auditoria consultável
(4.6) · painel de docas (4.7) · ambiente de demonstração (4.8) · `docker compose up` funcionando de
checkout limpo.

### 6.2 Fora do MVP

Migração de dado · integração ODBC · impressão física · cadastro de fornecedor · relatórios
gerenciais · exportação · notificação.

## 7. Métricas de sucesso

O alvo é uma demonstração que convence. As métricas medem isso, não operação.

| Métrica | Alvo | Como medir |
| --- | --- | --- |
| **Demonstração completa sem intervenção** | login → painel → conferência → exceção → fechamento → etiqueta, sem console, sem recarregar, sem dado editado à mão | ensaio cronometrado |
| Equivalência de regra | 70 de 70 com teste citando o `RK-…` | relatório de rastreabilidade do build *(AD-20)* |
| Exceções demonstráveis | os 5 estados plantados (FR-50) alcançáveis pela interface | roteiro de demonstração |
| Repetibilidade | segunda demonstração idêntica à primeira após o reset | executar duas vezes seguidas |
| Reconhecimento | operador do legado reconhece o fluxo sem explicação | mostrar a alguém que usa o sistema atual |

**Contra-métricas** — se qualquer uma acender, o POC não está pronto:

- Quem apresenta precisa avisar "isso ainda não funciona".
- Alguma das 70 regras só existe no papel.
- O painel de docas rouba a atenção do fluxo de conferência. O andaime não pode virar o produto.

## 8. Requisitos não-funcionais

### 8.0 Percepção — o que faz parecer real

- **NFR-1** — Leitura de código com resposta em **até 500 ms** na percepção do operador. Acima disso ele bipa de novo e duplica lançamento.
- **NFR-2** — A tela de conferência é operável **inteiramente por teclado**: o campo de leitura mantém o foco, aceita o código e Enter, e devolve o foco a si mesmo. É assim que um coletor real funciona — ele digita e dá Enter. Mouse é opcional em toda a tela.
- **NFR-3** — Interface legível a **um braço de distância**, em pé. Tipografia grande, alto contraste, nada de informação crítica em texto pequeno.
- **NFR-14** — **Feedback de leitura num único ponto focal.** A literatura de terminais de ponto de venda mostra que espalhar a confirmação pela tela degrada a performance do operador — ele para de bipar para procurar o resultado. Acerto, erro, descrição do produto e quantidade acumulada aparecem no mesmo lugar, sempre.
- **NFR-15** — **Sinal sonoro distinto** para leitura aceita e leitura recusada, além do visual. O operador de doca não olha a tela a cada leitura. Silenciável.
- **NFR-6** — Toda mensagem de erro diz **o que aconteceu e o que fazer**, em português, reaproveitando o texto legado quando existe.

### 8.1 Direção de UX

Referências de mercado, não invenção livre:

- **Painel de docas — Flexport Dashboard 2.0.** A lição que a equipe deles publicou: o supervisor não consegue revisar tudo, só subconjuntos, e o filtro que importa é por urgência — o que está mais perto de vencer. Nosso painel ordena por tempo de doca aberta, não por número de doca.
- **Painel de docas — dashboards de logística.** Mostrar fluxo e gargalo (aguardando → em conferência → fechada) vale mais que percentual de utilização. Daí o FR-45 proibir medidor de ocupação.
- **Tela de conferência — terminais de ponto de venda.** Feedback imediato com sinal visual **e** sonoro a cada leitura; alvos grandes; confirmação num único local. Daí NFR-14 e NFR-15.
- **Listas do supervisor — densidade calma.** Tabela densa, filtro por teclado, sem cartão decorativo. Rosana varre a lista, não navega por ela.

**Movimento.** Animação interativa faz parte da identidade do produto, com uma regra dura: *movimento
nunca precede informação*. O resultado da leitura fica legível em ≤100 ms e a animação roda em
paralelo, interrompível pela leitura seguinte. Valor numérico crítico troca instantâneo, nunca
interpola — um contador rolando de 12 para 18 faz o operador ler errado no meio do caminho. Cinco
momentos recebem tratamento orquestrado: reordenação do painel de docas, transição de rota
doca → conferência, flash de aceite/recusa, reveal da etiqueta e sequência de fechamento. Fora do
caminho crítico da leitura há liberdade de caráter visual — o ambiente é armazém, mas armazém não
obriga tela sem vida.

### 8.2 Dado e segurança

- **NFR-7** — Nenhuma senha em texto puro, em nenhum lugar — banco, log, resposta de API, mensagem de erro. *(AD-7.)* Vale também para os usuários semeados.
- **NFR-8** — Instante gravado em UTC, exibido no fuso do armazém. Uma conferência fechada 23h30 não pode aparecer no dia seguinte. *(AD-19.)*
- **NFR-10** — Conferência fechada é imutável em nível de aplicação **e** de dado.

### 8.3 Entrega

- **NFR-11** — `docker compose up --build` sobe tudo de um checkout limpo mais `.env` preenchido, **já com a massa semeada**. Quem clona o repositório vê a demonstração funcionando sem passo manual.
- **NFR-12** — Dado sobrevive a `docker compose down` + `up`.
- **NFR-13** — `.env.example` documenta toda variável lida, incluindo `MODO_DEMO`.

## 9. Perguntas em aberto

| # | Pergunta | Bloqueia | Dono |
| --- | --- | --- | --- |
| Q-1 | O legado **soma** ou **substitui** quantidade relançada? A regra `RK-8233e231d6fb` avisa mas não revela o efeito da confirmação. Assumimos soma (AD-17). | FR-18 | operador ou `getSourceFile` |
| Q-2 | `vsenha < 3` (`RK-8ffd715ce9ad`) — o que são os níveis 1, 2 e 3 de `usuario.niv_usu`? | FR-2 | Mundial |
| Q-6 | Existe divergência **aceitável** (tolerância) ou toda diferença é exceção? | FR-20 | Mundial |
| Q-7 | O que acontece com um item pendente quando a conferência fecha? Decidimos **fechar com `pendencia = 1`** — ver A-10. | FR-14, FR-22 | Mundial |
| Q-8 | `RK-ea5a22eaf219` (`descri`) e `RK-16bc1acd7b74` (`situacao`) não dizem a **entidade**. Alocamos a primeira a `acesso` (FR-7) e a segunda a `conferencia` (FR-21). | FR-7, FR-21 | `getSourceFile` |
| Q-9 | Quantas docas o armazém de entrada da Mundial tem? Semeamos 4. | FR-43 | Mundial |

**Resolvidas na fonte legada** (ver `achados-fonte-legada.md`): **Q-3** e **Q-4** — todos os
tipos e larguras de `estoq` vieram de `estoq_structure.TXT`, e `barr_emb3` existe como
`Character(14)`. **Q-5** morreu com a recalibração, porque FR-39 virou pré-visualização.

**Q-1 é o único bloqueador de fase que sobra**, e mesmo ele tem agora uma decisão fundamentada
(FR-18) em vez de uma suposição cega. Os `.SCX` são binários e não foram retidos — sem alguém da
Mundial, não há como fechar.

## 10. Índice de suposições

| # | Suposição | Impacto se errada |
| --- | --- | --- |
| A-6 | Um documento é de um fornecedor só (FR-12 trata divergência como exceção). | Modelo errado para carga consolidada. Médio. |
| A-7 | A massa semeada é plausível o bastante para quem conhece o negócio. Produtos e fornecedores são fictícios. | A demonstração perde credibilidade com público que conhece o setor. Médio — corrige-se trocando o seed. |
| A-8 | `qtd_rec` substitui em vez de acumular (FR-18). Fundamentada, não cega — ver `achados-fonte-legada.md` § Q-1. | Contagem errada em relançamento. **Alto**, e é a suposição que sobrou com mais peso. |
| A-9 | `conferencia.situacao` (`char(1)`) usa `A` aguardando · `C` em conferência · `F` fechada. Nenhum artefato do RNC define o domínio — só que o campo é obrigatório. | Dado incompatível com o legado se a Mundial já usa outra convenção. Médio — corrige-se com um `UPDATE`. |
| A-10 | Documento com item pendente **fecha**, marcando `pendencia = 1`, em vez de bloquear. Deriva da jornada UJ-2, cujo propósito é não travar o caminhão, e da existência da coluna `pendencia bit`. | Se o legado bloqueava, o novo sistema fecharia conferências que deveriam ficar abertas. **Alto.** |

**Cinco suposições morreram na leitura da fonte legada.** A-1 (schema de `log_even`) foi
substituída pelo schema real, recuperado da função `reg_log`. A-3 (`barr_emb3`) foi confirmada como
`Character(14)`. A-5 estava **errada**: `esco_imp` não é integração com ERP, é a rotina que escolhe
a impressora Windows. A-2 e A-4 saíram com a recalibração para POC.

## 11. Efeito no spine de arquitetura

A recalibração **desfaz** o conflito que a versão anterior deste PRD registrava: os três itens do
Deferred cuja condição de revisita era "piloto em armazém real" voltam a dormir. Backup testado,
degradação com banco fora do ar e envio físico do ZPL saem do escopo.

A pendência do andaime foi resolvida: o spine ganhou o **AD-21**, que põe seed, reset e painel de
códigos num projeto `Mundial.Demo` removível, e proíbe seed em migration DbUp.

A leitura da fonte legada obrigou a emendar **cinco ADs**, todos com o ID mantido:

| AD | O que mudou |
| --- | --- |
| AD-3 | O `getErModel` deixou de ser a autoridade do schema — provou-se incompleto (6 de 116 colunas de `estoq`, `log_even` vazia). A ordem passa a ser DDL → estrutura do DBF → `reg_log` → MCP. Ganhou a exceção que faltava para `id` e `rowversion`. |
| AD-8 | Permissão é por **tabela**, não por tela. |
| AD-10 | O agregado é o **documento**, não a linha. Finalizar age em todas as linhas de uma vez. |
| AD-14 | `estok_sgm` é o próprio banco `sgm`. Não há ERP externo a isolar; a integração real é a nota fiscal que cria as linhas. |
| AD-17 | Quantidade **substitui** com confirmação, não acumula. |

Nenhum AD foi renumerado nem removido. O spine passou no lint com 21 ADs.

## 12. Registro de recalibração

De **piloto interno em armazém real** para **POC demonstrável com ambientação de armazém**, em
2026-08-10. IDs de FR e NFR mantidos estáveis; nada foi renumerado.

| Item | Antes | Agora |
| --- | --- | --- |
| FR-4 | reset de senha obrigatório na migração | hash, sem migração de usuário |
| FR-34, FR-35 | cadastro completo de fornecedor | consulta; regras implementadas sem tela |
| FR-39 | envio para impressora Zebra por socket | pré-visualização do ZPL na tela |
| FR-41, FR-42 | auditoria de toda escrita | mantido, agora explicitamente só leitura |
| NFR-4 | não perder lançamento com banco fora do ar | **removido** — sem dado real |
| NFR-9 | backup com restauração testada | **removido** — sem dado real |
| NFR-14, NFR-15 | — | **novos** — feedback focal e sonoro |
| Seção 8.1 | — | **nova** — direção de UX com referência de mercado |
| 4.7, 4.8 | — | **novas** — painel de docas e ambiente de demonstração |
| Métricas | contagem idêntica em conferência dupla | demonstração completa sem intervenção |
| Q-5 | bloqueador de fase | resolvida pela recalibração |
| A-2, A-4 | suposições ativas | removidas |

### 12.1 Segunda passada — leitura da fonte legada

Depois da recalibração, a fonte legada foi lida direto pelo MCP do RNC (`getSourceFile`, 11
arquivos retidos). Relatório completo em `achados-fonte-legada.md`.

| Item | Antes | Agora |
| --- | --- | --- |
| FR-5 | permissão por tela | permissão por **tabela** (`readme.txt` + DDL) |
| FR-8 | localiza a conferência | localiza o **documento e seus itens**; o sistema nunca cria conferência |
| FR-18 | soma ao acumulado | **substitui** com confirmação |
| FR-33 | `[ASSUMPTION A-3]` | `barr_emb3` confirmado `Character(14)` |
| FR-41 | schema de auditoria inventado | schema **real**, recuperado de `reg_log` |
| FR-54 | — | **novo** — expiração de sessão por inatividade (`ShutTimer`) |
| UJ-1 | "confirma que quer somar" | corrigido — era invenção minha |
| Visão | "FoxPro de 1994" | 2011, remendado até 2021 |
| Q-3, Q-4 | abertas | **resolvidas** |
| A-1, A-3, A-5 | suposições ativas | **mortas** — A-5 estava errada |
| A-8 | — | **nova** — `qtd_rec` substitui |

Descobertas que não mudaram requisito mas mudam o risco do projeto: o legado **já é SQL Server**
(banco `sgm`, DDL de out/2025) para `acesso`, `conferencia`, `forne` e `usuario` — só `estoq`
continua em DBF. A migração é muito menos arriscada do que o planejamento assumia. E `estoq` tem
**116 campos**, não 6: é a tabela mestre de produtos do supermercado, e a tela do legado edita seis
deles.
