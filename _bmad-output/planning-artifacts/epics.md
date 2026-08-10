---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - '_bmad-output/planning-artifacts/prds/prd-poc-mundial-2026-08-10/prd.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-poc-mundial-2026-08-10/ARCHITECTURE-SPINE.md'
  - '_bmad-output/planning-artifacts/achados-fonte-legada.md'
  - '_bmad-output/planning-artifacts/uir-gap-report.md'
  - 'bmad-context.md'
excludedDocuments:
  - 'docs/historico-rnc/ux/DESIGN.md — gerado pelo RNC, contém as divergências D-02 e D-03'
  - 'docs/historico-rnc/ux/EXPERIENCE.md — idem'
---

# Mundial · Conferência de Recebimento - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Mundial · Conferência de Recebimento, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR-1: O sistema autentica por **matrícula e senha**.
FR-2: O sistema **recusa acesso** a usuário sem nível suficiente, com "Você não está autorizado a usar este Sistema" (`RK-8ffd715ce9ad`, condição legada `vsenha < 3`).
FR-3: Na definição ou troca de senha, o sistema **exige confirmação** e recusa quando os dois campos divergem: "Você deve Confirmar a senha" (`RK-58fefec22db6`).
FR-4: Toda senha é armazenada com **hash**, inclusive as dos usuários semeados. Nenhum caminho da aplicação lê senha em claro.
FR-5: O sistema aplica **permissão por tabela** com as quatro operações do legado: consultar, incluir, alterar, excluir (`RK-04c918661d8d`, `RK-6022cae899fa`, `RK-fa1ca141cf21`,…
FR-6: A permissão é decidida **no servidor**. A interface esconde o que o usuário não pode fazer, mas isso é conveniência, não controle.
FR-7: `usuario.nome` e `acesso.descri` são **obrigatórios** (`RK-d1a55f1103db`, `RK-ea5a22eaf219`).
FR-54: A sessão **expira por inatividade** e devolve o usuário ao login. O legado faz isso com um `Timer` global (`ShutTimer` em `conferencia.PRG`) que encerra a aplicação após inatividade…
FR-8: O operador **seleciona a doca** e **bipa ou digita o documento**. O sistema localiza o documento e seus itens.
FR-9: Documento inexistente é recusado: "Documento não cadastrado!" (`RK-c0fce5362f62`).
FR-10: Documento já fechado é recusado para edição: "Este Documento já foi conferido!" (`RK-ff51aa26bf33`, `RK-69b41cd017dd` — condição `fechado = .T.`).
FR-11: Documento já lançado gera **aviso com confirmação**, não bloqueio: "Este Documento já foi lançado! Confirma assim mesmo?" (`RK-cc8cfa3658d1`, `RK-45e526801fea`). Se o operador…
FR-12: Fornecedor diferente do esperado gera **aviso com confirmação**: "Fornecedor diferente! Confirma este fornecedor?" (`RK-a7f3c0eb65c1`).
FR-13: O operador **bipa o EAN-13**; o sistema resolve o **DUN-14** correspondente e exibe descrição e embalagem a partir de `estoq`.
FR-14: Código não reconhecido é recusado com "Código Não cadastrado!" e a oferta de cadastrar na hora (`RK-6fef4d31a290`, `RK-798f00f19690`, `RK-dab7d2033e2e`). A oferta só aparece para…
FR-15: Código não cadastrado **para aquele fornecedor** é recusado: "Código Não cadastrado para…" (`RK-732bb9300bad`).
FR-16: EAN-13 que não pertence ao DUN-14 informado é recusado: "Código EAN não é desse DUN-14!" (`RK-3b8ef53b6cf2`).
FR-17: Código que já tem quantidade lançada gera **aviso com confirmação** mostrando o valor atual: "Este Código já tem Qtde lançada (n)! Deseja lançá-lo assim mesmo?" (`RK-8233e231d6fb`,…
FR-18: Quantidade lançada **substitui** o valor de `qtd_rec` daquele item; quando já havia valor, só grava após a confirmação do FR-17, e a recusa aborta sem gravar. **Decisão provisória**…
FR-19: Exclusão de lançamento pede confirmação: "Confirma Exclusão?" (`RK-bdfbdff6c821`).
FR-20: O sistema registra separadamente **quantidade da nota** e **quantidade recebida**, em embalagem e em unidade — `qtd_nf`, `qtd_unid_nf`, `qtd_rec`, `qtd_unid_rec`. A diferença entre…
FR-21: `peso_bruto_col` e `balanca` são **obrigatórios** na conferência (`RK-82c929f4e851`, `RK-c5a64175c9a1`), e `situacao` também (`RK-16bc1acd7b74`).
FR-22: Finalizar pede **uma confirmação explícita**: "Finalizar conferência?" (`RK-fa93a48fbecc`).
FR-23: Ao finalizar, o sistema grava em **transação única**: `fechado`, `matr_fec` (quem fechou), `dt_hora` e `situacao`.
FR-24: Depois de fechada, a conferência é **imutável**. Nenhuma operação de edição a atinge.
FR-25: Dois operadores no mesmo documento: o segundo a gravar recebe **conflito** e revê o estado atual, em vez de sobrescrever a contagem do primeiro.
FR-26: Lista de conferências com busca e paginação, mostrando situação, doca, documento, data e se há divergência.
FR-27: Detalhe da conferência mostra os itens lançados, quem conferiu (`matr_conf`), quem fechou (`matr_fec`) e quando.
FR-28: Cadastro de produto com código, descrição, embalagem e quantidade por embalagem.
FR-29: Produto inexistente é recusado: "Código não cadastrado!" (`RK-5a7aaaa8862d`, `RK-e84d750f340a`).
FR-30: Um código de barras **não pode repetir dentro do mesmo produto**: "Este Código já esta cadastrado!" — validado nos três campos (`RK-a0bb1eeee55d`, `RK-99e9bfdcea75`,…
FR-31: Um código de barras **não pode pertencer a outro produto**: "Código já cadastrado para o Produto…" (`RK-2976e3756f6d`, `RK-ab467d52fa1f`, `RK-f3bda1fa3b77`). A mensagem nomeia o…
FR-32: Apagar um código existente pede confirmação: "Tem certeza que deseja excluir este código?" (`RK-5b2436bca3f0`, `RK-2c78478f0b97`, `RK-9f92b8e2a3c0`, e as confirmações…
FR-33: Limpar `barr_emb3` é transição de estado explícita (`RK-9f4468b42859`, `RK-75e2169fe930`, `RK-dfe2ca45ec1a`). `barr_emb3` é `Character(14)`, confirmado na estrutura do DBF.
FR-34: Consulta de fornecedor por código e por razão social.
FR-35: As regras de obrigatoriedade de `forne` são implementadas na camada de aplicação e cobertas por teste, ainda que sem tela no POC. Treze campos:
FR-36: O fornecedor da conferência é resolvido a partir do documento e comparado ao esperado (alimenta FR-12).
FR-37: O sistema gera a etiqueta ZPL com: descrição do produto (`RK-0811a89bc8e6`), embalagem e quantidade por embalagem (`RK-2b3c11b27fef`), código de barras em duas posições…
FR-38: O layout gerado é **byte a byte compatível** com o do legado. Etiqueta é rastreabilidade física: uma mudança de posição invalida leitura no armazém.
FR-39: O sistema **pré-visualiza a etiqueta na tela**, renderizando o ZPL gerado em imagem, com o texto ZPL disponível para inspeção. O envio para a impressora física fica fora do POC.…
FR-40: Falha na geração da etiqueta **não perde a conferência**: o lançamento permanece gravado e a etiqueta pode ser regerada.
FR-41: Toda operação de escrita gera registro em `log_even` com o schema do legado, recuperado da função `reg_log` de `conferencia.PRG`: `data_eve` (instante), `usuario` (matrícula),…
FR-42: A trilha é **append-only**, consultável por período e por matrícula, e não é editável pela aplicação.
FR-43: O painel mostra as **docas** e, em cada uma, o estado atual: livre, aguardando conferência, em conferência (com quem), ou fechada.
FR-44: Cada doca ocupada mostra: documento, fornecedor, progresso da conferência (itens lançados sobre itens esperados) e **há quanto tempo está aberta**.
FR-45: O painel destaca **exceção**, não estatística: conferência com divergência, conferência aberta há tempo demais, item pendente por código não cadastrado. Não há percentual de…
FR-46: Clicar numa doca entra na tela de conferência daquele documento, já com o contexto carregado.
FR-47: O painel atualiza sozinho, sem o usuário recarregar.
FR-48: O painel **não cria nem altera dado de negócio**. É leitura sobre `conferencia`. Nenhuma regra `RK-…` depende dele.
FR-49: O sistema sobe com **massa semeada** coerente: fornecedores, produtos com DUN-14 e EAN-13 que casam entre si, documentos abertos em docas diferentes, usuários com perfis distintos…
FR-50: A massa inclui **estados de exceção plantados**, não só o caminho feliz: uma conferência já fechada, uma com divergência entre nota e recebido, um EAN sem cadastro, um código que…
FR-51: **Reset da demonstração**: um comando restaura o estado semeado, para apresentar duas vezes seguidas sem dado sujo.
FR-52: **Painel de códigos à mão**: quem apresenta vê a lista dos códigos semeados, com o que cada um provoca (este resolve, este não existe, este é ambíguo), e pode enviá-los para o campo…
FR-53: O andaime é **isolado do domínio**: vive atrás de uma flag `MODO_DEMO`, não referencia regra de negócio e sua remoção não toca `Dominio` nem `Aplicacao`. *(Precisa de um AD novo no…

### NonFunctional Requirements

NFR-1: Leitura de código com resposta em **até 500 ms** na percepção do operador. Acima disso ele bipa de novo e duplica lançamento.
NFR-2: A tela de conferência é operável **inteiramente por teclado**: o campo de leitura mantém o foco, aceita o código e Enter, e devolve o foco a si mesmo. É assim que um coletor real…
NFR-3: Interface legível a **um braço de distância**, em pé. Tipografia grande, alto contraste, nada de informação crítica em texto pequeno.
NFR-14: **Feedback de leitura num único ponto focal.** A literatura de terminais de ponto de venda mostra que espalhar a confirmação pela tela degrada a performance do operador — ele para…
NFR-15: **Sinal sonoro distinto** para leitura aceita e leitura recusada, além do visual. O operador de doca não olha a tela a cada leitura. Silenciável.
NFR-6: Toda mensagem de erro diz **o que aconteceu e o que fazer**, em português, reaproveitando o texto legado quando existe.
NFR-7: Nenhuma senha em texto puro, em nenhum lugar — banco, log, resposta de API, mensagem de erro. *(AD-7.)* Vale também para os usuários semeados.
NFR-8: Instante gravado em UTC, exibido no fuso do armazém. Uma conferência fechada 23h30 não pode aparecer no dia seguinte. *(AD-19.)*
NFR-10: Conferência fechada é imutável em nível de aplicação **e** de dado.
NFR-11: `docker compose up --build` sobe tudo de um checkout limpo mais `.env` preenchido, **já com a massa semeada**. Quem clona o repositório vê a demonstração funcionando sem passo manual.
NFR-12: Dado sobrevive a `docker compose down` + `up`.
NFR-13: `.env.example` documenta toda variável lida, incluindo `MODO_DEMO`.

### Additional Requirements

Extraídos do `ARCHITECTURE-SPINE.md` (21 ADs) e de `achados-fonte-legada.md`. Cada um afeta o que uma story precisa fazer.

**Sem starter template.** O spine não indica scaffold pronto — a solução .NET e o app Angular nascem do `dotnet new` e do `ng new`. Isso vira a primeira story do Épico 1.

- **AR-1** — Solução em 6 projetos com dependências apontando para dentro: `Dominio` (zero referências), `Aplicacao`, `Infraestrutura`, `Api`, `Migrations`, `Demo`. Teste de arquitetura que falha o build se a direção inverter. *(AD-1, AD-21)*
- **AR-2** — Schema vem da fonte legada nesta ordem de autoridade: DDL SQL Server retido → `estoq_structure.TXT` → `reg_log` no PRG → MCP. Larguras são contrato. *(AD-3)*
- **AR-3** — Toda tabela ganha `id INT IDENTITY` e mantém a chave natural como `UNIQUE NOT NULL`. Busca por chave natural é endpoint próprio. *(AD-2)*
- **AR-4** — `conferencia` ganha `rowversion`; `UPDATE` compara versão e devolve `409` em conflito. *(AD-17)*
- **AR-5** — Migrations em DbUp, script numerado `NNNN_descricao.sql`, SQL literal. Nenhum DDL fora do DbUp. **Seed nunca em migration.** *(AD-9, AD-21)*
- **AR-6** — Dapper com SQL explícito em arquivo `.sql` embutido; um repositório por agregado; sem query builder. *(AD-9)*
- **AR-7** — Cada regra vira método com `[RegraNegocio("RK-…")]`, e cada uma tem teste citando o mesmo `ruleKey` no nome. Relatório de rastreabilidade a cada build. *(AD-5, AD-20)*
- **AR-8** — Contrato de listagem único: `?pagina&tamanho&busca&ordem`, resposta `{itens,total,pagina,tamanho}`, `tamanho` máx. 200. *(AD-15)*
- **AR-9** — Erro em `application/problem+json` (RFC 9457) com extensão `ruleKey`. *(AD-11)*
- **AR-10** — Um dono de escrita por tabela; os demais leem por port read-only. `estoq` é escrita só pelo cadastro DUN-14. *(AD-16)*
- **AR-11** — Instante em UTC no banco e na API; exibição em `America/Sao_Paulo` via `TZ_APLICACAO`. Nenhum `DateTime.Now` — só o port `IRelogio`. *(AD-19)*
- **AR-12** — Autenticação JWT; permissão por **tabela** via policy, revalidada no servidor a cada requisição. *(AD-8)*
- **AR-13** — Nomes de tabela e coluna preservam o legado; tipos, casos de uso e rotas em português; mensagens em pt-BR. *(AD-12, diretiva do autor)*
- **AR-14** — Angular 22 standalone com signals, sem `NgModule`. *(AD-13)*
- **AR-15** — `docker-compose.yml` com `web`, `api`, `db`; volume nomeado; serviços por nome, nunca `localhost`; CORS só para a origem do frontend; `.env.example` completo.
- **AR-16** — Andaime de demo em `Mundial.Demo`, referenciado por ninguém, ativo só com `MODO_DEMO=true`; apagar o projeto deixa a solução compilando. *(AD-21)*
- **AR-17** — A origem das linhas de `conferencia` é um port de entrada (`IIntegracaoNotaFiscalPort`), servido no POC pelo seeder. Nenhum caminho da aplicação cria conferência. *(AD-14, AD-10)*
- **AR-18** — Stack fixada e verificada: .NET 10, C# 14, Dapper 2.1.79, dbup-sqlserver 7.2.0, FluentValidation 12.1.1, Microsoft.Data.SqlClient 7.0.2, Angular 22.1.x, **TypeScript 6.x**, Node 24 LTS, SQL Server 2022.

### UX Design Requirements

Extraídos da seção 8.1 do PRD (direção de UX ancorada em Flexport Dashboard 2.0, literatura de dashboards de logística e literatura de terminais de ponto de venda) e dos NFRs de percepção. Os `docs/ux/` gerados pelo RNC foram **excluídos** (hoje em `docs/historico-rnc/ux/`) por conterem as divergências D-02 e D-03.

- **UX-DR1** — Componente **PainelLeituraFocal**: acerto, erro, descrição do produto, embalagem e quantidade acumulada aparecem sempre no **mesmo lugar** da tela. Espalhar a confirmação degrada a performance do operador. *(NFR-14)*
- **UX-DR2** — Componente **CampoLeitura**: mantém o foco, aceita código + Enter, devolve o foco a si mesmo, nunca perde foco por clique acidental. É como um coletor real opera. *(NFR-2)*
- **UX-DR3** — Serviço **SinalSonoro**: dois sons distintos, aceite e recusa, silenciável e persistente na preferência. *(NFR-15)*
- **UX-DR4** — Componente **DialogoConfirmacao**: um único diálogo padrão serve as 9 regras de `MessageBox`, com o texto legado literal e foco inicial no botão seguro. *(AD-6)*
- **UX-DR5** — Escala tipográfica e de contraste para leitura **a um braço de distância, em pé**; nenhuma informação crítica em texto pequeno. *(NFR-3)*
- **UX-DR6** — Painel de docas ordenado por **tempo de doca aberta**, não por número da doca. *(FR-45)*
- **UX-DR7** — O painel mostra **fluxo e gargalo** — aguardando → em conferência → fechada — e destaca exceção: divergência, doca aberta há tempo demais, item pendente. Percentual agregado de ocupação é **desaconselhado**: com 4 docas, "75% ocupado" esconde o que importa (*qual* doca, *há quanto tempo*) e não gera ação. Contagem simples ("3 de 4 ocupadas") é aceitável. *(FR-45)*
- **UX-DR8** — Componente **PilulaEstado**: estado de doca e presença de divergência codificados em **forma e cor**, não só em número, legíveis de relance.
- **UX-DR9** — Componente **TabelaDensa** para o supervisor: densidade alta, filtro operável por teclado, sem cartão decorativo. Rosana varre a lista, não navega por ela.
- **UX-DR10** — Componente **PreviaEtiqueta**: renderiza o ZPL gerado como imagem, com o texto ZPL disponível para inspeção. *(FR-39)*
- **UX-DR11** — Componente **PainelCodigos** (andaime): lista os códigos semeados com o efeito de cada um, e envia ao campo de leitura com um clique. *(FR-52)*
- **UX-DR12** — Toda mensagem em pt-BR, reaproveitando o texto legado literal — acentuação inclusive. *(NFR-6, diretiva do autor)*
- **UX-DR13** — **Movimento nunca precede informação.** O resultado de uma leitura fica legível em **≤100 ms**; qualquer animação roda em paralelo ou depois dela, e é interrompida pela leitura seguinte. Valores numéricos críticos (quantidade, total) **trocam instantâneo, nunca interpolam** — um contador rolando de 12 para 18 faz o operador ler errado no meio do caminho; o contêiner pode pulsar, o dígito não. Só `transform` e `opacity`, para não sair do compositor. `prefers-reduced-motion` sempre respeitado. *(NFR-1)*
- **UX-DR14** — **Movimento é convite, não enfeite.** Animação que responde a um gesto ou revela mudança de estado real é bem-vinda e faz parte da identidade do produto. Fora do caminho crítico da leitura há liberdade de caráter visual — o ambiente é armazém, mas armazém não obriga tela sem vida.
- **UX-DR15** — **Sistema de movimento** com tokens de duração e easing compartilhados, aplicado em cinco momentos: **(a)** reordenação do painel de docas quando o tempo de doca muda, com deslocamento contínuo dos cards; **(b)** transição de rota doca → conferência via `withViewTransitions()`, em que o card da doca vira o cabeçalho da tela; **(c)** flash de aceite/recusa no painel focal, junto com a informação já legível; **(d)** reveal da etiqueta ZPL na pré-visualização; **(e)** sequência de fechamento do documento, o único momento que aguenta algo mais elaborado por acontecer uma vez por conferência. Usa a API nova do Angular (`animate.enter` / `animate.leave`), mais leve e acelerada por hardware que o sistema antigo.

### FR Coverage Map

Todos os 54 FRs mapeados, nenhum órfão, nenhum em dois épicos.

| FR | Épico | O que entrega |
| --- | --- | --- |
| FR-1 … FR-4 | 1 | autenticação por matrícula e senha, com hash |
| FR-5, FR-6 | 1 | permissão por tabela, decidida no servidor |
| FR-7 | 1 | obrigatoriedade de `usuario.nome` e `acesso.descri` |
| FR-54 | 1 | expiração de sessão por inatividade |
| FR-8 … FR-12 | 2 | abertura do documento e as confirmações de exceção |
| FR-13 … FR-19 | 2 | leitura, resolução DUN-14, lançamento e exclusão |
| FR-20, FR-21 | 2 | divergência nota × recebido e campos obrigatórios |
| FR-22 … FR-25 | 2 | fechamento atômico, imutabilidade, conflito |
| FR-26, FR-27 | 2 | lista e detalhe de conferência |
| FR-36 | 2 | fornecedor do documento, que alimenta FR-12 |
| FR-28 … FR-33 | 3 | cadastro de produto e os três códigos de embalagem |
| FR-37 … FR-40 | 3 | geração do ZPL, fidelidade byte a byte, prévia |
| FR-34, FR-35 | 4 | consulta de fornecedor e suas 13 regras de obrigatoriedade |
| FR-41, FR-42 | 4 | trilha de auditoria no schema do legado |
| FR-43 … FR-48 | 4 | painel de docas |
| FR-49 … FR-53 | 5 | massa semeada, exceções plantadas, reset, códigos à mão, isolamento |

**Cobertura de regra por épico** — as 70 regras do UIR:

| Épico | Regras | Origem |
| --- | --- | --- |
| 1 | 10 | login (4) e obrigatoriedade de `acesso`/`usuario` (6) |
| 2 | 24 | fluxo de conferência, incluindo 9 confirmações de UI |
| 3 | 27 | cadastro DUN-14 (20) e etiqueta ZPL (7) |
| 4 | 13 | obrigatoriedade de `forne` |
| 5 | 0 | andaime não implementa regra |
| — | 2 | erro ODBC — sem FR por decisão (AD-14) |

## Epic List

**Cinco épicos.** O desenho técnico já está validado (21 ADs, lint limpo) e a direção de UX está
fixada, então não há fronteira de risco que justifique fatiar mais. Cada épico entrega algo que
alguém consegue fazer, e nenhum depende de épico futuro para funcionar.

Não há épico de "banco de dados" nem de "componentes": schema, migrations e design system nascem
dentro do épico que primeiro precisa deles.

### Epic 1: Entrar no sistema e ser reconhecido

O operador abre a aplicação, entra com matrícula e senha, e o sistema sabe quem ele é e o que ele
pode fazer. É o épico que também levanta o chão — solução em 6 projetos, schema de `usuario` e
`acesso` vindo do DDL legado, migrations DbUp, `docker compose up` funcionando, contrato de erro, e
a base visual e de movimento que a tela de login já exige.

**FRs cobertos:** FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-54
**Standalone:** sim — login e permissão completos, sem depender de nada adiante.

### Epic 2: Conferir uma carga na doca

O coração do produto. O operador abre um documento, bipa produto, lança quantidade, trata as
exceções que o legado tratava, e fecha a conferência de forma irreversível. Vinte FRs e o maior
bloco de regras do sistema.

**FRs cobertos:** FR-8 a FR-27, FR-36
**Standalone:** sim — lê `estoq` e `conferencia` semeados; não precisa do cadastro do Épico 3 para
funcionar.

### Epic 3: Manter códigos de embalagem e imprimir etiqueta

O supervisor cadastra e corrige os códigos de barras de embalagem do produto, e o sistema gera a
etiqueta ZPL correspondente. Os dois andam juntos porque giram no mesmo dado: o cadastro define o
código, a etiqueta o materializa no mundo físico.

**FRs cobertos:** FR-28 a FR-33, FR-37 a FR-40
**Standalone:** sim — escreve em `estoq`, sem depender do fluxo de conferência.

### Epic 4: Enxergar a operação do armazém

A visão que o legado nunca deu. O supervisor vê as docas, o que está aberto há tempo demais, onde há
divergência, o que foi feito e por quem, e consulta fornecedor. Inclui o painel de docas — a tela
inventada, declarada como andaime.

**FRs cobertos:** FR-34, FR-35, FR-41 a FR-48
**Standalone:** sim — leitura sobre dado que já existe; o painel funciona com massa semeada.

### Epic 5: Apresentar a demonstração

Fecha o POC como peça apresentável: os cinco estados de exceção plantados e alcançáveis pela
interface, reset entre apresentações, painel de códigos à mão, e o andaime isolado num projeto
removível.

**FRs cobertos:** FR-49 a FR-53
**Standalone:** é o último por natureza — planta exceções nos fluxos que os épicos anteriores
construíram.

---

## Epic 1: Entrar no sistema e ser reconhecido

O operador abre a aplicação, entra com matrícula e senha, e o sistema sabe quem ele é e o que pode fazer. Este épico também levanta o chão — solução, schema vindo do DDL legado, migrations, `docker compose up` e a base visual que a primeira tela já exige.

### Story 1.1: Subir a aplicação de um checkout limpo

As a **pessoa que recebe o repositório**,
I want **rodar um comando e ver a aplicação no ar**,
So that **eu consiga avaliar o resultado sem montar ambiente**.

**Acceptance Criteria:**

**Given** um clone limpo do repositório e um `.env` preenchido a partir do `.env.example`
**When** eu executo `docker compose up --build`
**Then** os serviços `web`, `api` e `db` sobem e a aplicação responde no navegador
**And** o `db` usa volume nomeado, e os serviços se referenciam por nome — nunca `localhost`
**And** o CORS libera apenas a origem do frontend

**Given** a solução recém-criada
**When** eu inspeciono a estrutura
**Then** existem os projetos `Mundial.Dominio`, `Mundial.Aplicacao`, `Mundial.Infraestrutura`, `Mundial.Api`, `Mundial.Migrations` e `Mundial.Demo`
**And** `Mundial.Dominio` não tem referência de projeto para nenhum outro
**And** um teste de arquitetura falha o build se a direção de dependência inverter *(AD-1)*

**Given** o `.env.example`
**When** eu comparo com as variáveis lidas pela aplicação
**Then** toda variável está documentada, incluindo `MODO_DEMO` e `TZ_APLICACAO`

**Given** as versões fixadas no spine
**When** eu verifico os arquivos de projeto
**Then** .NET 10, Dapper 2.1.79, dbup-sqlserver 7.2.0, FluentValidation 12.1.1, Microsoft.Data.SqlClient 7.0.2, Angular 22.1.x, **TypeScript 6.x** e Node 24 estão em uso *(AR-18)*

### Story 1.2: Ter usuários e permissões no banco

As a **operador cadastrado no legado**,
I want **que minha matrícula e minhas permissões existam no sistema novo**,
So that **eu consiga entrar e trabalhar com o que já era meu**.

**Acceptance Criteria:**

**Given** o DDL legado de `usuario` e `acesso`
**When** as migrations DbUp rodam
**Then** as tabelas existem com os tipos e larguras exatos da fonte: `matric char(5)`, `nome char(35)`, `niv_usu nchar(1)`, `arquivo char(10)`, `descri char(30)`, e as quatro flags `bit`
**And** a coluna de senha se chama `senha_hash` e não `senha` *(AD-3 exceção 1, AD-7)*
**And** cada tabela ganha `id INT IDENTITY` mantendo a chave natural como `UNIQUE NOT NULL` *(AD-2)*
**And** nenhum `CREATE` ou `ALTER` existe fora do DbUp *(AD-9)*

**Given** `usuario.nome` ou `acesso.descri` vazios
**When** tento gravar
**Then** a operação é recusada *(`RK-d1a55f1103db`, `RK-ea5a22eaf219`)*

**Given** o sistema recém-instalado com `MODO_DEMO=true`
**When** o seeder roda
**Then** existem usuários com perfis distintos: um com todas as permissões, um sem permissão de inclusão, e um sem autorização de acesso *(FR-49)*
**And** o seed vive em `Mundial.Demo`, nunca em migration *(AD-21)*

### Story 1.3: Entrar com matrícula e senha

As a **operador de recebimento**,
I want **entrar com minha matrícula e senha**,
So that **o sistema saiba quem está conferindo a carga**.

**Acceptance Criteria:**

**Given** uma matrícula que não existe
**When** tento entrar
**Then** vejo "Matrícula não cadastrada! Favor contactar supervisor" *(`RK-046f5592ef5b`)*

**Given** uma matrícula válida e senha errada
**When** tento entrar
**Then** vejo "Senha inválida" *(`RK-f8293cf9dbb3`)*

**Given** credenciais corretas
**When** entro
**Then** recebo um token e chego à tela inicial *(FR-1, FR-4)*
**And** o valor da senha não aparece em log, resposta de API ou mensagem de erro *(NFR-7)*
**And** a senha é verificada por hash, nunca por comparação de texto *(AD-7)*

**Given** a primeira tela do produto
**When** ela é construída
**Then** existem tokens de cor, tipografia, espaçamento, duração e easing compartilhados
**And** a escala tipográfica e o contraste atendem leitura a um braço de distância, em pé *(NFR-3, UX-DR5)*
**And** os tokens de movimento respeitam `prefers-reduced-motion` *(UX-DR13)*
**And** toda mensagem ao usuário está em pt-BR, reaproveitando o texto legado literal onde ele existe — acentuação inclusive *(UX-DR12, NFR-6, AR-13)*
**And** fora do caminho crítico de leitura, a tela tem liberdade de caráter visual *(UX-DR14)*

### Story 1.4: Ser barrado quando não tenho autorização

As a **supervisor de segurança**,
I want **que usuários sem nível suficiente não entrem**,
So that **só quem foi autorizado opere o recebimento**.

**Acceptance Criteria:**

**Given** um usuário cujo nível é insuficiente
**When** ele tenta entrar com credenciais corretas
**Then** o acesso é negado com "Você não está autorizado a usar este Sistema" *(`RK-8ffd715ce9ad`)*
**And** a decisão acontece no servidor, nunca só na interface *(AD-8)*

**Given** que o significado dos níveis de `usuario.niv_usu` é desconhecido (Q-2)
**When** a regra é implementada
**Then** o limiar fica em um único ponto configurável, com `// TODO` citando Q-2
**And** o comportamento padrão reproduz a condição legada `vsenha < 3`

### Story 1.5: Confirmar a senha ao defini-la

As a **usuário definindo ou trocando minha senha**,
I want **digitar a senha duas vezes**,
So that **eu não fique trancado fora por um erro de digitação**.

**Acceptance Criteria:**

**Given** os campos de senha e confirmação com valores diferentes
**When** tento salvar
**Then** vejo "Você deve Confirmar a senha" e nada é gravado *(`RK-58fefec22db6`)*

**Given** os dois campos iguais e não vazios
**When** salvo
**Then** a senha é gravada com hash e passo a entrar com ela

### Story 1.6: Ver e fazer apenas o que minha permissão alcança

As a **operador com permissões limitadas**,
I want **que o sistema respeite o que posso consultar, incluir, alterar e excluir**,
So that **eu não execute por engano algo que não é meu papel**.

**Acceptance Criteria:**

**Given** um usuário sem linha em `acesso` para uma tabela
**When** ele tenta qualquer operação sobre ela
**Then** a operação é negada *(FR-5)*
**And** as quatro flags são avaliadas individualmente: `consultar` *(`RK-04c918661d8d`)*, `incluir` *(`RK-6022cae899fa`)*, `alterar` *(`RK-fa1ca141cf21`)* e `excluir` *(`RK-be780ff12c0e`)*

**Given** um usuário com `consultar = 1` e `incluir = 0` para `estoq`
**When** ele abre a tela de cadastro de código
**Then** ele vê os dados e não vê a ação de incluir
**And** se a requisição de inclusão for enviada mesmo assim, o servidor a recusa *(FR-6)*

**Given** uma tela que lê duas tabelas diferentes
**When** a permissão é avaliada
**Then** as duas permissões correspondentes são exigidas — `acesso.arquivo` é nome de **tabela**, não de tela *(AD-8)*

**Given** qualquer erro de permissão
**When** a resposta é devolvida
**Then** ela vem em `application/problem+json` conforme RFC 9457 *(AD-11)*

### Story 1.7: Ser desconectado depois de muito tempo parado

As a **supervisor**,
I want **que uma sessão esquecida em terminal de doca expire**,
So that **ninguém opere no crachá de outro**.

**Acceptance Criteria:**

**Given** uma sessão sem interação por mais que o período configurado
**When** o período vence
**Then** o usuário é devolvido à tela de login, com a razão dita: "Sessão encerrada por inatividade" *(FR-54)*
**And** nenhum lançamento já gravado é perdido

**Given** um operador que estava numa conferência quando a sessão expirou
**When** ele entra de novo
**Then** volta para a mesma conferência, no estado em que ela ficou

**Given** o período de inatividade
**When** configuro o sistema
**Then** ele é ajustável por variável de ambiente, com padrão na ordem de grandeza do legado (horas, não minutos)

---

## Epic 2: Conferir uma carga na doca

O coração do produto. O operador abre um documento, bipa produto, lança quantidade, trata as exceções que o legado tratava, e fecha a conferência de forma irreversível.

### Story 2.1: Ter documentos e produtos para conferir

As a **operador**,
I want **que os documentos da nota fiscal e os produtos já estejam no sistema**,
So that **eu tenha o que conferir quando o caminhão chegar**.

**Acceptance Criteria:**

**Given** o DDL legado de `conferencia` e `forne`, e a estrutura do DBF de `estoq`
**When** as migrations rodam
**Then** `conferencia` existe com PK composta `filial + orig_des + tipo_doc + SERIE + numero + codigo` e os defaults do legado
**And** `estoq` existe com `CODIGO char(5)`, `DESCRI char(60)`, `CODBARR/2/3 char(13)`, `BARR_EMB/2/3 char(14)`, `EMBALAG char(10)`, `EMBALQT numeric(9,4)` *(AR-2)*
**And** `forne` existe com PK `codfor` e os treze campos obrigatórios do DDL — a conferência precisa resolver o fornecedor do documento *(FR-36)*
**And** `conferencia` ganha `rowversion` *(AD-3 exceção 2, AD-17)*
**And** nenhuma coluna fantasma foi criada — `dataenvironment`, `timer1`, `listnf`, `listprod`, `codigo1` não existem no schema *(AD-3)*

**Given** que o sistema nunca cria conferência
**When** o código é escrito
**Then** a origem das linhas é o port `IIntegracaoNotaFiscalPort`, servido no POC pelo seeder *(AR-17)*
**And** nenhum endpoint da aplicação insere em `conferencia`

**Given** `MODO_DEMO=true`
**When** o seeder roda
**Then** existem documentos em docas diferentes, com itens e `qtd_nf` preenchida, e produtos cujos EAN-13 e DUN-14 casam entre si *(FR-49)*

### Story 2.2: Abrir o documento da carga que chegou

As a **operador de recebimento**,
I want **informar a doca e o documento e ver o que deveria ter vindo**,
So that **eu comece a conferência com a lista certa na frente**.

**Acceptance Criteria:**

**Given** um documento existente e aberto
**When** informo doca e documento
**Then** vejo o fornecedor e os itens esperados com suas quantidades de nota *(FR-8)*

**Given** um documento que não existe
**When** informo o número
**Then** vejo "Documento não cadastrado!" *(FR-9, `RK-c0fce5362f62`)*

**Given** um documento já fechado
**When** tento abri-lo para conferir
**Then** vejo "Este Documento já foi conferido!" e não consigo editar *(`RK-ff51aa26bf33`, `RK-69b41cd017dd`)*

**Given** um documento já lançado
**When** tento abri-lo
**Then** vejo "Este Documento já foi lançado! Confirma assim mesmo?" e a recusa interrompe a operação *(FR-11, `RK-cc8cfa3658d1`, `RK-45e526801fea`)*

**Given** um documento cujo fornecedor difere do esperado
**When** abro
**Then** vejo "Fornecedor diferente! Confirma este fornecedor?" e a recusa interrompe *(FR-12, FR-36, `RK-a7f3c0eb65c1`)*

**Given** as confirmações acima
**When** elas aparecem
**Then** usam o mesmo componente `DialogoConfirmacao`, com o texto legado literal e foco inicial no botão seguro *(UX-DR4)*
**And** nenhuma delas bloqueia no servidor — são decisões do operador *(AD-6)*

### Story 2.3: Bipar o produto e ver na hora o que é

As a **operador com o coletor na mão**,
I want **bipar o código e ver imediatamente que produto é**,
So that **eu conte a mercadoria certa sem tirar a mão do coletor**.

**Acceptance Criteria:**

**Given** o campo de leitura
**When** a tela abre e a cada leitura concluída
**Then** o foco está e permanece nele, aceitando código seguido de Enter, sem exigir mouse *(NFR-2, UX-DR2)*

**Given** um EAN-13 que resolve para exatamente um DUN-14
**When** bipo
**Then** vejo descrição, embalagem e quantidade por embalagem em **até 500 ms** *(NFR-1)*
**And** a confirmação aparece sempre no mesmo lugar da tela, junto com erro, descrição e quantidade acumulada *(NFR-14, UX-DR1)*
**And** ouço o som de aceite *(NFR-15, UX-DR3)*

**Given** um código que casa com mais de um DUN-14
**When** bipo
**Then** a leitura é recusada e os candidatos são exibidos — o sistema não escolhe por mim *(FR-13)*

**Given** um código que não existe
**When** bipo
**Then** ouço o som de recusa e vejo "Código Não cadastrado!" *(FR-14, `RK-6fef4d31a290`, `RK-798f00f19690`)*
**And** o item fica marcado como **pendente** e eu sigo conferindo o resto da carga, sem travar o caminhão
**And** *(a oferta de cadastrar na hora — `RK-dab7d2033e2e` — chega na Story 3.1, quando existe onde cadastrar; até lá esta tela só recusa e marca pendência, e o Épico 2 funciona sem o Épico 3)*

**Given** um código não cadastrado para aquele fornecedor
**When** bipo
**Then** vejo "Código Não cadastrado para…" *(`RK-732bb9300bad`)*

**Given** um EAN-13 que não pertence ao DUN-14 informado
**When** bipo
**Then** vejo "Código EAN não é desse DUN-14!" *(FR-16, `RK-3b8ef53b6cf2`)*

**Given** qualquer animação nesta tela
**When** uma leitura acontece
**Then** a informação fica legível em ≤100 ms e o movimento roda em paralelo, interrompível pela leitura seguinte *(UX-DR13)*

### Story 2.4: Lançar a quantidade recebida

As a **operador**,
I want **registrar quanto realmente chegou de cada item**,
So that **a diferença em relação à nota fique registrada em vez de ser forçada**.

**Acceptance Criteria:**

**Given** um item com `qtd_rec` igual a zero
**When** informo a quantidade e confirmo
**Then** o valor é gravado no item *(FR-18)*

**Given** um item que já tem quantidade lançada
**When** informo uma nova quantidade
**Then** vejo "Este Código já tem Qtde lançada (n)! Deseja lança-lo assim mesmo?" com o valor atual *(FR-17, `RK-8233e231d6fb`, `RK-5960908935ee`)*
**And** ao confirmar, o novo valor **substitui** o anterior *(AD-17)*
**And** ao recusar, nada é gravado

**Given** um lançamento gravado
**When** consulto o item
**Then** vejo `qtd_nf`, `qtd_unid_nf`, `qtd_rec` e `qtd_unid_rec` separadamente, e a divergência entre nota e recebido é exibida como dado, não como erro *(FR-20)*

**Given** os campos obrigatórios da conferência
**When** salvo
**Then** `peso_bruto_col`, `balanca` e `situacao` são exigidos *(FR-21)*
**And** `situacao` usa a convenção `A` aguardando · `C` em conferência · `F` fechada; ao primeiro lançamento a linha passa de `A` para `C` `[ASSUMPTION A-9]` *(`RK-82c929f4e851`, `RK-c5a64175c9a1`, `RK-16bc1acd7b74`)*

**Given** o valor numérico da quantidade na tela
**When** ele muda
**Then** o dígito troca instantâneo, sem interpolação — o contêiner pode pulsar, o número não *(UX-DR13)*

### Story 2.5: Apagar um lançamento errado

As a **operador que lançou o item errado**,
I want **remover o lançamento com uma confirmação**,
So that **eu corrija sem apagar coisa certa por acidente**.

**Acceptance Criteria:**

**Given** um item com quantidade lançada
**When** peço para excluir o lançamento
**Then** vejo "Confirma Exclusão?" *(FR-19, `RK-bdfbdff6c821`)*
**And** ao confirmar, `qtd_rec` volta a zero e a mudança é registrada na auditoria
**And** ao recusar, nada muda

**Given** um documento já fechado
**When** tento excluir um lançamento
**Then** a operação é recusada *(FR-24)*

### Story 2.6: Finalizar a conferência

As a **operador que terminou de contar**,
I want **fechar o documento de forma irreversível**,
So that **ninguém altere a contagem depois que eu assinei**.

**Acceptance Criteria:**

**Given** um documento aberto
**When** peço para finalizar
**Then** vejo "Finalizar conferência?" e a recusa cancela a operação *(FR-22, `RK-fa93a48fbecc`)*

**Given** a confirmação aceita
**When** o fechamento acontece
**Then** `fechado`, `matr_fec`, `dt_hora` e `situacao` são gravados em **transação única**, em **todas as linhas do documento** *(FR-23, AD-10)*
**And** `matr_fec` recebe a matrícula de quem fechou
**And** `dt_hora` é gravado em UTC e exibido no fuso do armazém *(AD-19)*
**And** `situacao` recebe `'F'`, segundo a convenção `A` aguardando · `C` em conferência · `F` fechada `[ASSUMPTION A-9]`

**Given** um documento com itens pendentes por código não cadastrado
**When** finalizo
**Then** o fechamento **acontece**, e a confirmação informa antes quantos itens ficam pendentes
**And** `pendencia` recebe `1` nas linhas correspondentes `[ASSUMPTION A-10]`
**And** o documento aparece com marca de pendência nas consultas e no painel
  *(Bloquear o fechamento contrariaria a jornada UJ-2, cujo propósito é o operador não travar o caminhão. A coluna `pendencia bit` existe no DDL legado justamente para registrar isso. Ver Q-7.)*

**Given** um documento fechado
**When** qualquer operação de escrita é tentada sobre ele
**Then** é recusada, tanto pela aplicação quanto pela camada de dado *(NFR-10, FR-24)*

**Given** o momento do fechamento
**When** ele conclui
**Then** uma sequência de movimento marca a conclusão — o único momento do fluxo que comporta algo mais elaborado, por acontecer uma vez por conferência *(UX-DR15e)*

### Story 2.7: Não perder a contagem de outro operador

As a **operador**,
I want **ser avisado quando alguém alterou o documento enquanto eu trabalhava**,
So that **minha contagem não apague a de outra pessoa em silêncio**.

**Acceptance Criteria:**

**Given** dois operadores com o mesmo documento aberto
**When** o segundo tenta gravar depois do primeiro
**Then** ele recebe `409 Conflict` em `application/problem+json` e vê o estado atual antes de decidir *(AD-17, AD-11)*
**And** nenhuma contagem é sobrescrita em silêncio *(FR-25)*

### Story 2.8: Consultar conferências

As a **operador ou supervisor**,
I want **encontrar uma conferência e ver o que foi lançado**,
So that **eu confira o que já foi feito sem depender de quem estava na doca**.

**Acceptance Criteria:**

**Given** a lista de conferências
**When** consulto
**Then** uso `?pagina&tamanho&busca&ordem` e recebo `{itens,total,pagina,tamanho}`, com `tamanho` máximo de 200 *(AD-15)*
**And** vejo situação, doca, documento, data e se há divergência *(FR-26)*

**Given** uma conferência específica
**When** abro o detalhe
**Then** vejo os itens lançados, quem conferiu (`matr_conf`), quem fechou (`matr_fec`) e quando *(FR-27)*

**Given** a lista para o supervisor
**When** ela é construída
**Then** usa o componente `TabelaDensa`: densidade alta, filtro operável por teclado, sem cartão decorativo *(UX-DR9)*

---

## Epic 3: Manter códigos de embalagem e imprimir etiqueta

O supervisor cadastra e corrige os códigos de barras de embalagem do produto, e o sistema gera a etiqueta ZPL correspondente. O cadastro define o código; a etiqueta o materializa no mundo físico.

### Story 3.1: Cadastrar os códigos de embalagem de um produto

As a **supervisor**,
I want **informar os códigos de barras de embalagem de um produto**,
So that **o operador consiga bipar a caixa na conferência**.

**Acceptance Criteria:**

**Given** um produto existente
**When** abro o cadastro
**Then** vejo código, descrição, embalagem, quantidade por embalagem e os três campos `barr_emb`, `barr_emb2` e `barr_emb3` *(FR-28)*
**And** cada campo aceita no máximo 14 caracteres — largura é contrato *(AR-2)*

**Given** um código de produto que não existe
**When** informo
**Then** vejo "Código não cadastrado!" *(FR-29, `RK-5a7aaaa8862d`, `RK-e84d750f340a`)*

**Given** o cadastro
**When** gravo
**Then** apenas `estoq` é escrita, e apenas por este caso de uso — o fluxo de conferência lê por port read-only *(AD-16)*

**Given** um operador na tela de conferência que bipou um código inexistente e tem permissão de inclusão
**When** a recusa aparece
**Then** ele recebe "Código Não cadastrado! Deseja Cadastrar agora?" e, ao aceitar, chega a este cadastro com o código já preenchido *(FR-14, `RK-dab7d2033e2e`)*
**And** ao concluir, volta à conferência e consegue bipar o código que acabou de criar
**And** sem permissão de inclusão, a oferta não aparece — o item permanece pendente como na Story 2.3 *(FR-5)*

### Story 3.2: Impedir código repetido dentro do mesmo produto

As a **supervisor**,
I want **ser impedido de repetir o mesmo código nos três campos**,
So that **a leitura na doca não fique ambígua**.

**Acceptance Criteria:**

**Given** um código já presente em outro dos três campos do mesmo produto
**When** informo em `barr_emb`
**Then** vejo "Este Código já esta cadastrado!" *(FR-30, `RK-a0bb1eeee55d`, `RK-99e9bfdcea75`)*

**Given** o mesmo cenário em `barr_emb2`
**When** informo
**Then** vejo "Este Código já esta cadastrado" *(`RK-f9e0b12a76af`, `RK-4ca8df36a760`)*

**Given** o mesmo cenário em `barr_emb3`
**When** informo
**Then** vejo "Este Código já esta cadastrado" *(`RK-41493150036e`, `RK-ab62193a2b2d`)*

**Given** um campo vazio
**When** gravo
**Then** a validação de duplicidade não dispara — vazio não conflita

### Story 3.3: Impedir código que já é de outro produto

As a **supervisor**,
I want **saber quando o código já pertence a outro produto**,
So that **duas mercadorias diferentes não respondam ao mesmo código de barras**.

**Acceptance Criteria:**

**Given** um código já cadastrado em outro produto
**When** informo em qualquer um dos três campos
**Then** vejo "Código já cadastrado para o Produto…" com o produto que já o usa *(FR-31, `RK-2976e3756f6d`, `RK-ab467d52fa1f`, `RK-f3bda1fa3b77`)*
**And** o valor não é gravado

### Story 3.4: Apagar um código de embalagem

As a **supervisor**,
I want **remover um código com confirmação explícita**,
So that **eu não apague por engano um código em uso na doca**.

**Acceptance Criteria:**

**Given** um campo de código preenchido
**When** limpo o valor e tento gravar
**Then** vejo "Tem certeza que deseja excluir este código?" *(FR-32, `RK-5b2436bca3f0`, `RK-2c78478f0b97`, `RK-9f92b8e2a3c0`, `RK-ade9dd1661d1`, `RK-305af19071c6`, `RK-21ac9f1bddea`)*
**And** ao recusar, o valor anterior permanece

**Given** `barr_emb3` sendo esvaziado
**When** confirmo
**Then** a transição de estado é registrada explicitamente *(`RK-9f4468b42859`, `RK-75e2169fe930`, `RK-dfe2ca45ec1a`, FR-33)*

### Story 3.5: Gerar a etiqueta da embalagem

As a **supervisor**,
I want **que o sistema monte a etiqueta ZPL do produto**,
So that **a embalagem seja identificável no armazém como sempre foi**.

**Acceptance Criteria:**

**Given** um produto com descrição, embalagem, quantidade por embalagem e código de barras
**When** peço a etiqueta
**Then** o ZPL contém a descrição *(`RK-0811a89bc8e6`)*, a embalagem com a quantidade *(`RK-2b3c11b27fef`)*, o código de barras nas duas posições *(`RK-25721748a2b1`, `RK-3ff169d79617`)* e o código legível *(`RK-1b386e3870da`)*
**And** está delimitado por `^XA` e `^XZ` *(FR-37, `RK-b382d85d0edc`, `RK-e8876989538a`)*

**Given** a etiqueta gerada
**When** comparo com a saída do legado para o mesmo produto
**Then** o resultado é **byte a byte idêntico** — posição alterada invalida leitura no armazém *(FR-38)*

**Given** a geração
**When** o código é escrito
**Then** ela vive em `Mundial.Infraestrutura.Etiquetas`, não na camada de aplicação *(AD-6)*

### Story 3.6: Ver a etiqueta antes de imprimir

As a **supervisor**,
I want **ver como a etiqueta vai sair**,
So that **eu confira o conteúdo sem gastar papel**.

**Acceptance Criteria:**

**Given** uma etiqueta gerada
**When** abro a pré-visualização
**Then** vejo a etiqueta renderizada como imagem, e o texto ZPL disponível para inspeção *(FR-39, UX-DR10)*

**Given** a pré-visualização abrindo
**When** ela aparece
**Then** o conteúdo é revelado com movimento, sem atrasar a informação *(UX-DR15d)*

**Given** uma falha na geração da etiqueta
**When** ela ocorre
**Then** a conferência e o lançamento permanecem gravados, e a etiqueta pode ser gerada de novo *(FR-40)*

---

## Epic 4: Enxergar a operação do armazém

A visão que o legado nunca deu. O supervisor vê as docas, o que está aberto há tempo demais, onde há divergência, o que foi feito e por quem.

### Story 4.1: Registrar tudo que muda

As a **supervisor**,
I want **que toda alteração fique registrada**,
So that **eu consiga responder quem mudou o quê e quando**.

**Acceptance Criteria:**

**Given** o schema recuperado da função `reg_log` do legado
**When** as migrations rodam
**Then** `log_even` existe com `data_eve`, `usuario`, `arquivo`, `chave`, `val_ant` e `val_atu` *(AR-2)*

**Given** uma inclusão
**When** ela é gravada
**Then** o registro traz `val_ant = 'Registro Incluido'` *(FR-41)*

**Given** uma exclusão
**When** ela é gravada
**Then** o registro traz `val_atu = 'Registro Excluido'`

**Given** uma alteração
**When** ela é gravada
**Then** `val_ant` e `val_atu` trazem **apenas os campos que mudaram**, uma linha `campo = valor` por campo

**Given** uma leitura
**When** ela acontece
**Then** nenhum registro de auditoria é gerado

**Given** a trilha
**When** consulto por período e por matrícula
**Then** vejo os registros, e nenhum caminho da aplicação permite editá-los *(FR-42)*

### Story 4.2: Consultar fornecedor

As a **supervisor**,
I want **encontrar um fornecedor por código ou razão social**,
So that **eu confira com quem é a carga sem sair do sistema**.

**Acceptance Criteria:**

**Given** a consulta de fornecedor
**When** busco por código ou razão social
**Then** vejo os dados do fornecedor, usando o contrato de listagem padrão *(FR-34, AD-15)*

**Given** que o POC não tem tela de cadastro de fornecedor
**When** as regras de obrigatoriedade são implementadas
**Then** os treze campos obrigatórios são validados na camada de aplicação e cobertos por teste, cada um citando seu `ruleKey` *(FR-35, AR-7)*
**And** as regras são `RK-b3e7fcc26f3e`, `RK-ef82abb7456c`, `RK-b5da8c743238`, `RK-e74f29d4f922`, `RK-2ce1876d83ad`, `RK-1d4194439839`, `RK-4697ebd74678`, `RK-854f2452216e`, `RK-98835efbf746`, `RK-6aff3b12acb2`, `RK-353ee013c009`, `RK-37afeda868c2`, `RK-f2ca891c315f`

### Story 4.3: Ver o painel de docas

As a **supervisor**,
I want **ver de relance o que está acontecendo em cada doca**,
So that **eu saiba onde agir sem perguntar a ninguém**.

**Acceptance Criteria:**

**Given** o painel
**When** abro
**Then** vejo cada doca com seu estado: livre, aguardando conferência, em conferência (com quem) ou fechada *(FR-43)*

**Given** uma doca ocupada
**When** olho o cartão
**Then** vejo documento, fornecedor, progresso (itens lançados sobre esperados) e **há quanto tempo está aberta** *(FR-44)*

**Given** o estado de cada doca
**When** ele é exibido
**Then** usa o componente `PilulaEstado`, codificando estado em **forma e cor**, legível de relance *(UX-DR8)*

**Given** uma doca no painel
**When** clico nela
**Then** entro na tela de conferência daquele documento com o contexto já carregado *(FR-46)*

**Given** o painel
**When** verifico o que ele escreve
**Then** ele **não cria nem altera** dado de negócio — é leitura sobre `conferencia` *(FR-48)*
**And** consome os mesmos endpoints de leitura que o produto usa *(AD-21 corolário)*

### Story 4.4: Enxergar a exceção antes do resto

As a **supervisor**,
I want **que o painel destaque o que está errado**,
So that **meu olho vá direto ao problema e não à estatística**.

**Acceptance Criteria:**

**Given** conferências com divergência entre nota e recebido
**When** abro o painel
**Then** elas aparecem destacadas *(FR-45)*

**Given** uma doca aberta há mais tempo que o limite
**When** abro o painel
**Then** ela aparece destacada

**Given** um item pendente por código não cadastrado
**When** abro o painel
**Then** a doca correspondente indica a pendência

**Given** o painel
**When** verifico o conteúdo
**Then** ele mostra **fluxo e gargalo** — aguardando → em conferência → fechada — e não exibe percentual agregado de ocupação *(UX-DR7)*

### Story 4.5: Ver o painel se reorganizar sozinho

As a **supervisor com o painel aberto na parede**,
I want **que ele se atualize e reordene sem eu tocar**,
So that **a informação na tela seja a de agora**.

**Acceptance Criteria:**

**Given** o painel aberto
**When** o estado de uma doca muda no servidor
**Then** a tela reflete a mudança sem eu recarregar *(FR-47)*

**Given** que a ordem é por tempo de doca aberta
**When** uma doca ultrapassa outra
**Then** os cartões se deslocam de forma contínua até a nova posição *(UX-DR6, UX-DR15a)*

**Given** a navegação do painel para a conferência
**When** clico numa doca
**Then** a transição usa `withViewTransitions()` e o cartão da doca se torna o cabeçalho da tela *(UX-DR15b)*

**Given** qualquer animação do painel
**When** ela roda
**Then** usa apenas `transform` e `opacity`, e respeita `prefers-reduced-motion` *(UX-DR13)*

---

## Epic 5: Apresentar a demonstração

Fecha o POC como peça apresentável: exceções plantadas e alcançáveis, reset entre apresentações, códigos à mão, e o andaime isolado num projeto removível.

### Story 5.1: Isolar o andaime do produto

As a **pessoa que vai transformar este POC em produto**,
I want **que tudo que é de demonstração viva separado**,
So that **remover o andaime seja apagar uma pasta, não refatorar**.

**Acceptance Criteria:**

**Given** a solução
**When** inspeciono as referências
**Then** `Mundial.Demo` referencia `Aplicacao` e **não é referenciado por ninguém** *(AD-21)*
**And** nenhum tipo de `Dominio` ou `Aplicacao` conhece `Mundial.Demo`

**Given** `MODO_DEMO=false`
**When** a aplicação sobe
**Then** nenhum recurso de demonstração é registrado, e o produto funciona normalmente *(FR-53)*

**Given** que eu apague o projeto `Mundial.Demo` e a flag
**When** compilo a solução
**Then** ela compila

**Given** as migrations
**When** verifico seu conteúdo
**Then** nenhuma contém dado semeado — migration é schema, seed é dado de demonstração *(AR-5)*

### Story 5.2: Plantar os casos que valem demonstrar

As a **pessoa que apresenta**,
I want **que os caminhos de exceção estejam prontos para acontecer**,
So that **a demonstração mostre as regras, não só o caminho feliz**.

**Acceptance Criteria:**

**Given** `MODO_DEMO=true` e o sistema recém-semeado
**When** percorro a interface
**Then** consigo alcançar, sem editar dado à mão: uma conferência **já fechada**, uma com **divergência** entre nota e recebido, um **EAN sem cadastro**, um **código que existe em dois produtos** e um **usuário sem permissão de inclusão** *(FR-50)*

**Given** a massa semeada
**When** examino a coerência
**Then** todo EAN-13 semeado resolve para um DUN-14 existente, exceto os plantados deliberadamente como exceção *(FR-49)*

**Given** os produtos e fornecedores semeados
**When** alguém do setor olha
**Then** os nomes e códigos são plausíveis para supermercado — descrição, embalagem e quantidade coerentes entre si *(A-7)*

### Story 5.3: Resetar entre uma apresentação e outra

As a **pessoa que apresenta duas vezes seguidas**,
I want **devolver o sistema ao estado inicial com um comando**,
So that **a segunda demonstração seja igual à primeira**.

**Acceptance Criteria:**

**Given** uma demonstração já executada, com dado alterado
**When** aciono o reset
**Then** o estado semeado é restaurado por completo *(FR-51)*

**Given** o reset executado
**When** repito a demonstração do começo
**Then** o resultado é idêntico ao da primeira vez

**Given** `MODO_DEMO=false`
**When** procuro o reset
**Then** ele não existe — nem na interface, nem como endpoint

### Story 5.4: Ter os códigos à mão durante a apresentação

As a **pessoa que apresenta sem coletor de código de barras**,
I want **ver os códigos semeados e o que cada um provoca**,
So that **eu demonstre acerto e exceção sem decorar número**.

**Acceptance Criteria:**

**Given** `MODO_DEMO=true`
**When** abro o painel de códigos
**Then** vejo a lista dos códigos semeados, cada um com o efeito que provoca — este resolve, este não existe, este é ambíguo, este já tem quantidade lançada *(FR-52, UX-DR11)*

**Given** um código na lista
**When** clico nele
**Then** ele é enviado ao campo de leitura como se tivesse sido bipado, e o foco volta ao campo *(UX-DR2)*

**Given** o painel de códigos
**When** a demonstração roda
**Then** ele não ocupa o centro da tela nem compete com o painel de leitura focal *(NFR-14)*

---

## Validação final

Cinco verificações. Duas acharam problema; os dois foram corrigidos antes de fechar.

### 1. Cobertura de requisito

| Dimensão | Resultado | Verificação |
| --- | --- | --- |
| Requisitos funcionais | **54/54** | por script, contra a lista de FRs |
| Requisitos de UX | **15/15** | por script, contra os UX-DRs |
| Regras do UIR | **68/70** | por script, contra `getUirModule` |

As duas regras não cobertas são `RK-d0605132c1f1` e `RK-a709080069ea` — erro de conexão ODBC, sem FR por decisão registrada no AD-14. Nenhuma chave inventada.

### 2. Arquitetura

**Starter template:** não há. O spine não indica scaffold pronto, então a Story 1.1 cria a solução do zero e fixa a direção de dependência com teste de arquitetura.

**Criação de tabela sob demanda:** nenhuma story cria schema que não usa.

| Tabela | Criada em | Por que ali |
| --- | --- | --- |
| `usuario`, `acesso` | 1.2 | primeira story que precisa autenticar |
| `conferencia`, `estoq`, `forne` | 2.1 | primeira story que confere carga |
| `log_even` | 4.1 | primeira story que consulta a trilha |

**Correção aplicada:** `forne` não era criada em story nenhuma, embora a Story 2.2 exiba o fornecedor do documento e a 4.2 o consulte. Entrou na 2.1.

### 3. Qualidade das stories

- 30 stories, 102 blocos Given/When/Then.
- Nenhum critério usa "graciosamente", "razoável" ou "amigável" — todos têm consequência verificável.
- Cada story cabe numa sessão de um agente de desenvolvimento.
- Toda regra citada leva sua chave `RK-…`, o que permite conferir contra a fonte com `getRule()` antes de fechar *(AD-5)*.

### 4. Estrutura dos épicos

Épicos entregam capacidade de usuário, não marco técnico. Não existe épico de banco, de API ou de componentes.

**Sobreposição de arquivo — avaliada, sem consolidação necessária:**

| Arquivo | Épico que escreve | Épico que lê |
| --- | --- | --- |
| `conferencia` | 2 | 4 |
| `estoq` | 3 | 2 |
| `log_even` | 4 | 4 |

A separação é a que o AD-16 exige — um dono de escrita por tabela. Não é churn: cada épico tem fronteira de escrita distinta.

### 5. Dependências

**Entre épicos.** Cada épico funciona sem os posteriores. O Épico 2 lê `estoq` e `forne` semeados, sem depender do cadastro do Épico 3. O Épico 4 lê dado que já existe.

**Correção aplicada:** a Story 2.3 prometia a oferta de cadastrar o código na hora — que só existe no Épico 3. Era dependência para a frente, e quebrava a independência do Épico 2. O FR-14 foi dividido pela costura natural: o Épico 2 entrega a recusa e a marcação de pendência (o operador segue conferindo o resto da carga), e o Épico 3 acrescenta a oferta inline com retorno à conferência.

**Dentro de cada épico.** Cada story se apoia apenas nas anteriores. A 1.1 cria os seis projetos, incluindo `Mundial.Demo`, o que permite a 1.2 semear usuários sem depender da 5.1 — que endurece o isolamento com teste, mas não o inaugura.

### Pendências que seguem para a implementação

| Item | Onde aparece | Situação |
| --- | --- | --- |
| **Q-1** — quantidade soma ou substitui | Story 2.4, AD-17, FR-18 | decisão fundamentada, sem confirmação humana; muda AC se a Mundial disser o contrário |
| **Q-2** — níveis de `usuario.niv_usu` | Story 1.4 | limiar em ponto único configurável, com `// TODO` |
| **Q-6** — existe tolerância de divergência | Story 2.4 | toda diferença é registrada; tolerância não implementada |
| **Q-7** — destino do item pendente ao fechar | Stories 2.3, 2.6 | pendência é marcada; o que acontece no fechamento não está decidido |
| **Q-9** — quantas docas | Story 4.3 | seed usa 4 |
