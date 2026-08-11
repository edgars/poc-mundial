# Prompt para o Claude Design — Dossiê de Modernização (entregável em PDF)

> **Como usar:** cole este arquivo inteiro no Claude Design, com o design system do projeto já
> configurado. Tudo abaixo da linha `=== CONTEÚDO ===` é fonte de verdade verificada no repositório
> e no ambiente em execução: não inventar número, endereço, chave de regra nem trecho de código.
> Se algo faltar, deixar um marcador visível `[a confirmar]` em vez de preencher.

---

## 1. Encomenda

Produza um **documento executivo-técnico em PDF**, em português do Brasil, entregável ao cliente
**Supermercados Mundial**, contando todo o processo de modernização do sistema legado de conferência
de recebimento (Visual FoxPro) para a nova aplicação web (.NET 10 + Angular 22 + SQL Server).

**Leitores, nesta ordem:**

1. Diretoria e TI do cliente — precisam entender o que foi feito, o que foi provado e o que vem depois.
2. Time técnico do cliente — precisa conseguir auditar: qual regra do legado virou qual código.
3. Time comercial da Skalena — usa o documento como evidência de método.

**Extensão alvo:** 28 a 40 páginas A4, sendo a tabela de regras (§ 9) o anexo mais longo.

## 2. Instruções de design

- Use o **design system configurado**. Nada de tema genérico de template.
- **Capa** com título, cliente, data (11 de agosto de 2026), versão 1.0 e a marca da Skalena.
- **Sumário** com numeração de páginas.
- **Cabeçalho/rodapé** discretos: título curto à esquerda, número de página à direita.
- **Blocos de código** em monoespaçada com marcação de linguagem visível (FoxPro / C# / SQL / bash /
  JSON). Blocos "antes/depois" devem ficar lado a lado quando couber na página; em coluna única
  quando não couber — nunca quebrar um bloco no meio.
- **Tabelas** em largura total, zebra sutil, cabeçalho fixo quando a tabela passar de uma página.
  A tabela de regras (§ 9) é larga: reduza o corpo da fonte e permita quebra de linha nas células
  de código.
- **Capturas de tela**: uma por bloco, com legenda numerada ("Figura n — …") e borda leve. Os
  arquivos estão em `tools/capturas/` (24 PNGs, listados em § 10 com as legendas prontas).
- **Destaques**: use caixas de destaque para os quatro números-chave — 70 regras capturadas,
  68 implementadas com teste, 102 testes automatizados, 24 telas verificadas.
- **Diagramas** (crie-os): (a) a cadeia legado → RNC → UIR → BMAD → código; (b) as camadas ports &
  adapters; (c) o fluxo de deploy pull na Tencent. Estilo do design system, sem clip-art.
- Tom: direto, técnico, sem superlativo de marketing. Nada de "solução inovadora"; prefira
  "o sistema faz X, e a prova é Y".

---

=== CONTEÚDO ===

# Modernização do Sistema de Conferência de Recebimento

## Supermercados Mundial · Prova de Conceito

---

## 1. Sumário executivo

O Supermercados Mundial conferia a mercadoria recebida em um sistema Visual FoxPro escrito em 2011 e
remendado até 2021 — a tela `Dun14.scx`, mais o módulo de recebimento e uma base parte em SQL Server,
parte em arquivos DBF num compartilhamento de rede. O sistema funciona, mas nenhum time consegue
evoluí-lo com segurança: a regra de negócio está dentro dos eventos das telas, não existe teste, e o
conhecimento saiu da empresa junto com quem escreveu.

Esta POC demonstra o caminho de saída. Em vez de reescrever "pelo que a gente lembra do processo", o
legado foi **lido por engenharia reversa automatizada (RNC)**, que produziu uma representação
intermediária (**UIR**) com as telas, o modelo de dados e **70 regras de negócio com chave estável**.
Essas 70 regras viraram requisito, arquitetura e código pelo método **BMAD**, e cada uma carrega no
código-fonte a mesma chave que o RNC extraiu do FoxPro — o que torna a equivalência **auditável**,
não uma promessa.

**O que foi entregue:**

| Resultado | Número |
| --- | --- |
| Regras de negócio recuperadas do legado | 70 |
| Regras implementadas, cada uma com teste citando a mesma chave | 68 |
| Regras deliberadamente descartadas, com justificativa registrada | 2 (erro de conexão ODBC) |
| Testes automatizados verdes | 102 (92 de regra + 10 de arquitetura) |
| Telas verificadas com captura de tela | 24 |
| Endpoints REST | 21 |
| Decisões de arquitetura registradas | 22 |
| Ambiente público, rodando de ponta a ponta | `https://poc-mundial.exai.extreme.digital` |

**O que a POC não é:** não é piloto de produção. Não há migração do dado histórico do legado, não há
backup formal, não há integração ligada com o ERP e não há impressão física de etiqueta — cada uma
dessas exclusões está registrada como decisão, não como esquecimento (§ 12).

---

## 2. O legado, como ele é

| Elemento | Situação encontrada |
| --- | --- |
| `Dun14.scx` | Tela de cadastro de códigos de barras. Formulário binário do Visual FoxPro; a lógica vive nos eventos dos campos. |
| Módulo de recebimento (`Rec_nf.scx`, `conferencia.PRG`) | Conferência da nota fiscal. Abre as tabelas com `Set Exclusive On` — dois operadores no mesmo documento não era cenário possível. |
| `conferencia`, `acesso`, `forne`, `usuario` | Já em SQL Server, acessadas via ODBC (`estok_sgm`). |
| `estoq` (116 colunas) | Ainda em DBF, num compartilhamento de rede (`\\10.1.1.9\estoq200`). A tela editava 6 dessas colunas. |
| Regra de negócio | Dentro do evento da tela, misturada com `Messagebox`, navegação de cursor e gravação. |
| Teste automatizado | Nenhum. |

O padrão que se repete no código legado é este: condição, mensagem, decisão do usuário e gravação na
mesma linha de execução.

```foxpro
If qtd_rec > 0
   If Messagebox('Este Código já tem Qtde lançada ('+Trans(qtd_rec)+')!'+Chr(13)+ ;
                 'Deseja lança-lo assim mesmo?',4+32+256,sistema) = 7
      This.Value = 0
      Return .F.                && aborta no "Não"
   Endif
Endif
Replace qtd_rec With This.Value
```

Enquanto isso continuar assim, qualquer mudança de processo é uma aposta: não há onde testar a regra
sem abrir a tela e digitar.

---

## 3. RNC — engenharia reversa do legado

O **RNC** é a plataforma de engenharia reversa que lê o sistema legado e produz uma descrição
normalizada dele. Não é um conversor de código: ele não traduz FoxPro para C#. Ele
**extrai o que o sistema decide** — telas, campos, modelo de dados, regras, mensagens — e devolve
isso num formato que humanos revisam e agentes consomem.

**O que foi usado, e para quê:**

| Recurso do RNC | Uso nesta POC | O que resolveu |
| --- | --- | --- |
| Workspace com retenção `RETAINED` (11 arquivos) | Guardou o código-fonte legado e os DDLs originais dentro do workspace | Permitiu conferir hipótese contra a fonte, em vez de deduzir |
| `listUirModules` / `getUirModule` | Inventário dos módulos do legado e o conteúdo de cada um | Base do relatório de divergências entre o UIR e o pacote automático |
| `getModuleRules` / `getRule(RK-…)` | Catálogo de regras e consulta de uma regra pela chave | Cada regra no código novo pode ser reaberta na fonte legada |
| `getSourceFile` | Leitura do fonte retido | Resolveu ambiguidades que o modelo ER não resolvia |
| `getErModel` | Modelo entidade-relacionamento inferido | Serviu de ponto de partida — e mostrou seus próprios limites |
| **MCP Server** | Expõe tudo isso como ferramentas para o agente de codificação | O agente consulta o legado **durante** a implementação, sem sair do editor |

**Por que o MCP Server importa.** MCP (Model Context Protocol) é o protocolo que liga o agente de
codificação a fontes externas de contexto. Com o RNC exposto por MCP, a pergunta "o que exatamente o
legado fazia aqui?" é respondida no meio da tarefa, com a fonte real, em vez de depender da memória
de quem está escrevendo. É o que permite que a regra `RK-8233e231d6fb` no arquivo C# seja mais que um
comentário: ela é um ponteiro verificável para o trecho de FoxPro que a originou.

**E onde o RNC precisou de gente.** O relatório honesto faz parte do método:

- O pacote de documentação **gerado automaticamente** pelo RNC (`docs/historico-rnc/`) tinha **7
  divergências** relevantes contra o UIR — registradas em `uir-gap-report.md`. Ele foi preservado por
  rastreabilidade e **marcado como não utilizável para construir**.
- `getErModel` devolveu **6 das 116 colunas** de `estoq`, todas com tipo `UNKNOWN`. Foi a leitura do
  DDL retido que mostrou que `BARR_EMB3` é `Character(14)` e não decimal — e revelou a simetria que
  explica o domínio: **três slots de EAN-13** (unidade de venda) e **três slots de DUN-14**
  (embalagem).
- A lógica de `Rec_nf.scx` é binária e **não foi retida**. A pergunta "a quantidade conferida soma ou
  substitui?" não tinha resposta textual. Foi decidida por evidência estrutural (§ 12), com a decisão
  e a incerteza registradas.

---

## 4. UIR — a representação intermediária

O **UIR** (Universal Intermediate Representation) é o artefato que o RNC entrega e que todo o resto
consome. Para esta POC ele continha:

- **Telas e campos** do legado, com os eventos onde a lógica morava.
- **Modelo de dados** com os tipos reais das colunas.
- **70 regras de negócio**, cada uma com: chave estável `RK-…`, mensagem literal, condição extraída,
  severidade e o campo/tela de origem.
- **Código-fonte retido**, consultável pela chave.

A chave estável é o detalhe que faz o método funcionar. `RK-8233e231d6fb` não é um número de
requisito que alguém escolheu: é a identidade daquela decisão do legado, e ela viaja intacta pelo
PRD, pela arquitetura, pelo código, pelo teste e pela mensagem que o operador lê na tela. É por isso
que a tabela do § 9 pode existir.

---

## 5. BMAD — do UIR ao código

**BMAD** é o método de desenvolvimento assistido por agentes usado no projeto: em vez de mandar um
agente "fazer o sistema", ele conduz o trabalho por artefatos encadeados, cada um revisável antes do
próximo. Fases usadas aqui:

| Fase | Artefato produzido | Conteúdo |
| --- | --- | --- |
| Análise | Product brief + achados da fonte legada | O que o sistema faz e o que a leitura do FoxPro desmentiu |
| Requisitos | **PRD** | **54 requisitos funcionais**, **12 não-funcionais**, 3 jornadas de usuário |
| Arquitetura | **Spine** | **22 decisões de arquitetura** (`AD-1`…`AD-22`), cada uma com o que ela previne |
| Planejamento | Épicos e stories | **5 épicos, 30 stories** com critério de aceite testável |
| Implementação | Código + testes | Cada story fecha com teste citando a chave da regra |

**Como o UIR alimentou o BMAD.** As 70 regras entraram como insumo obrigatório, não como sugestão:
54 FRs saíram delas (algumas agrupam mais de uma regra), e a alocação foi conferida no fim — **68
regras viraram requisito, 2 viraram decisão de arquitetura**, nenhuma ficou órfã e nenhuma chave foi
inventada. Quando o pacote automático divergia do UIR, o UIR ganhou; quando o UIR era omisso, a
pergunta virou item aberto com decisão registrada.

**Por que BMAD e não "pedir para a IA escrever".** Três motivos concretos, todos visíveis no
resultado:

1. **A regra tem dono.** Cada linha de código carrega a chave da regra que a justifica. Sem isso, o
   agente escreve algo plausível e ninguém consegue provar que é o comportamento do legado.
2. **A arquitetura vira trava, não sugestão.** As decisões do spine viraram **teste de arquitetura**
   que roda no CI (§ 6). O agente não consegue violar a decisão sem quebrar o build.
3. **A incerteza fica visível.** O que não deu para provar (§ 12) está escrito como pergunta aberta,
   com a decisão tomada e o critério para revê-la.

---

## 6. A aplicação nova

**Stack:** .NET 10 (LTS) · C# 14 · Dapper · SQL Server 2022 · Angular 22 · TypeScript 6 ·
migrações com DbUp · Docker Compose.

**Arquitetura: ports & adapters.** Quatro projetos, com a dependência apontando sempre para dentro:

| Projeto | Papel | Depende de |
| --- | --- | --- |
| `Mundial.Dominio` | As regras. Entidades, invariantes, mensagens do legado | **nada** |
| `Mundial.Aplicacao` | Casos de uso: ordem das operações, permissão, auditoria | só o domínio |
| `Mundial.Infraestrutura` | Dapper, SQL Server, geração de ZPL | domínio e aplicação |
| `Mundial.Api` | HTTP: rota, autenticação, tradução de erro | tudo acima |

Isso não é convenção de boa vontade — é verificado. Dez **testes de arquitetura** falham o build se
alguém violar a decisão. Exemplos reais da suíte:

- `Mundial.Dominio` não pode ter nenhuma referência de projeto.
- O domínio não pode conter as palavras `Dapper`, `SqlConnection` ou `Microsoft.AspNetCore`.
- Nenhum `DateTime.Now` no código — só o port `IRelogio` (o armazém trabalha em UTC e exibe no fuso).
- Nenhum `CREATE TABLE` / `ALTER TABLE` fora do projeto de migrações.
- Todo endpoint exige autorização, salvo os públicos declarados.
- Nenhum endpoint aceita matrícula vinda do corpo da requisição — a identidade vem do token.

**A regra saiu da tela.** Comparando com o § 2, a mesma decisão agora mora em três lugares
separados, e o texto que o operador lê continua sendo o do legado:

```csharp
// DOMÍNIO — decide. Não pergunta, não grava.
[RegraNegocio("RK-8233e231d6fb", "Este Código já tem Qtde lançada (")]
public ResultadoRegra AvaliarLancamento()
    => QtdRec > 0
        ? ResultadoRegra.Confirma("RK-8233e231d6fb",
            $"Este Código já tem Qtde lançada ({QtdRec:0.###})!\nDeseja lança-lo assim mesmo?")
        : ResultadoRegra.Ok;

// APLICAÇÃO — o "Não" do Messagebox virou ausência de confirmação
var aviso = item.AvaliarLancamento();
if (!aviso.Passou && !confirmado) return aviso;
item.Lancar(quantidade, quantidade);

// API — o Messagebox virou status HTTP
if (!r.Passou) return Problema(r);   // 422 + ruleKey
```

| Legado | Aplicação nova |
| --- | --- |
| `Messagebox(...) = 7` decide | `confirmado: false` no corpo da requisição decide |
| Regra dentro do evento da tela | Regra testável sem banco e sem navegador |
| `Set Exclusive On` impedia concorrência | Controle de versão por linha (`rowversion`) e HTTP 409 |
| Texto no meio do `If` | Mesmo texto, preservado literal, com chave `RK-…` |

---

## 7. A API REST

Toda a aplicação web fala com o servidor por uma **API REST** — 21 endpoints, JSON, sem estado de
sessão no servidor. A mesma API atende o navegador, o coletor de dados e qualquer integração futura.

**Contratos que valem para tudo:**

- **Erro é RFC 9457** (`application/problem+json`), com uma extensão do projeto: **`ruleKey`**. O
  cliente recebe *qual regra do legado* recusou a operação, não só um texto.
- **Listagem tem um formato só** (`AD-15`): `?pagina=&tamanho=&busca=&ordem=` devolvendo
  `{ itens, total, pagina, tamanho }`. Um cliente HTTP serve todos os recursos.
- **Concorrência é otimista** (`AD-17`): o cliente devolve a versão que leu; se outro operador
  gravou no meio, a resposta é 409, não sobrescrita silenciosa.
- **O tempo viaja em UTC** e é exibido no fuso do armazém.

Exemplo real, capturado do ambiente em execução — regra de negócio atravessando a rede:

```http
POST /api/conferencia/lancamentos?documento=000147901/1
Authorization: Bearer <jwt>
{"codigo":"06120","quantidade":38,"confirmado":false}

HTTP/1.1 422 Unprocessable Content
content-type: application/problem+json

{"title":"Confirmação necessária","status":422,
 "detail":"Este Código já tem Qtde lançada (60)!\nDeseja lança-lo assim mesmo?",
 "ruleKey":"RK-8233e231d6fb","tipo":"ExigeConfirmacao"}
```

**Autorização espelha o legado.** O sistema antigo guardava permissão por tabela e operação
(`consultar`, `incluir`, `alterar`, `excluir`) na tabela `acesso`. Isso virou **uma policy por tabela
e operação**: o token carrega as permissões reais do usuário e cada endpoint exige a sua
(`conferenci:consultar`, `estoq:alterar`, `log_even:consultar`…). Operador sem a permissão recebe
403 — inclusive quando chama a API diretamente, fora da tela.

**Documentação executável.** A API publica **OpenAPI 3.1** e uma interface **Swagger UI**:

| Recurso | Endereço |
| --- | --- |
| Swagger UI | `/api/docs` |
| Especificação OpenAPI | `/api/openapi/v1.json` |

No Swagger, o botão **Authorize** aceita **matrícula e senha** (fluxo *password* do OAuth 2.0) e
guarda o token: dá para exercitar qualquer endpoint sem escrever uma linha de código. Quem já tem um
token pode colá-lo. O cadeado aparece só nas rotas protegidas, e a descrição de cada uma informa a
permissão exigida. É com isso que o time do cliente audita o comportamento sem depender da interface.

---

## 8. Identidade: hoje, e a recomendação para as próximas modernizações

**Hoje**, a POC autentica como o legado autenticava — matrícula e senha na tabela `usuario`, com o
hash calculado pela aplicação — e emite um **JWT** com validade configurável. Isso foi deliberado:
equivalência primeiro, para que a POC prove o processo do armazém sem mudar a forma de entrar.

**A recomendação para a modernização completa é adotar OpenID Connect** (OIDC, sobre OAuth 2.0) com
o provedor de identidade corporativo — Entra ID, Keycloak, Okta ou equivalente:

| Ganho | Por quê |
| --- | --- |
| Senha sai da aplicação | O sistema deixa de guardar hash de senha; quem trata identidade é quem já trata |
| Entrada única (SSO) | O operador usa a mesma credencial do resto da empresa |
| MFA e política de senha | Passam a ser configuração do provedor, não código a manter |
| Desligamento imediato | Demissão revoga o acesso a todos os sistemas de uma vez |
| Auditoria central | Tentativa de acesso fica no log corporativo, não só no banco da aplicação |
| Escala para vários legados | Cada sistema modernizado entra no mesmo provedor, sem replicar cadastro |

**O custo dessa troca é baixo, por construção.** A API já valida *bearer token* e autoriza por
claim; migrar significa trocar o emissor do token e mapear grupo do diretório → permissão de tabela.
Nenhuma regra de negócio muda. A recomendação é fazer isso **antes** de o segundo sistema legado ser
modernizado — é quando o custo de manter cadastros paralelos começa a compor.

---

## 9. As regras de negócio capturadas

**Como ler esta tabela.** Cada linha é uma regra que o RNC extraiu do legado. A coluna *Código
FoxPro retido* traz a expressão original, como o RNC a recuperou; a coluna *Implementação C#* traz o
arquivo, a linha e a expressão equivalente no sistema novo.

Três observações de honestidade, que devem aparecer no PDF junto da tabela:

1. O pacote gerado pelo RNC enumera as regras por número de requisito e nem sempre carrega a chave
   `RK-…` junto da expressão; o casamento abaixo é feito **pela mensagem**. Quando o legado repetia a
   mesma mensagem em mais de um ponto da tela, aparecem **todas as expressões retidas** daquela
   mensagem.
2. Regras de obrigatoriedade de campo vêm do **DDL do legado** (`NOT NULL`), não de código FoxPro —
   nesses casos a coluna mostra a definição da coluna.
3. As duas regras de erro de conexão ODBC (`RK-d0605132c1f1`, `RK-a709080069ea`) **não foram
   implementadas de propósito** — detalhe no fim da seção.

| # | Chave RNC | Mensagem / regra do legado | Código FoxPro retido (condição) | Implementação C# |
| --- | --- | --- | --- | --- |
| 1 | `RK-58fefec22db6` | Você deve Confirmar a senha | `Thisform.senha3.Value#This.Value And !Empty(Thisform.senha3.Value)` | src/Mundial.Aplicacao/Autenticacao.cs:18<br>`return (null, ResultadoRegra.Recusa("RK-58fefec22db6", "Você deve Confirmar a senha"));` |
| 2 | `RK-046f5592ef5b` | Matrícula não cadastrada! Favor contactar supervisor | `Reccount()=0` | src/Mundial.Aplicacao/Autenticacao.cs:37<br>`if (usuario is null) return (null, ResultadoRegra.Recusa("RK-046f5592ef5b",` |
| 3 | `RK-f8293cf9dbb3` | Senha inválida | `This.Value#senha` | src/Mundial.Aplicacao/Autenticacao.cs:41<br>`return (null, ResultadoRegra.Recusa("RK-f8293cf9dbb3", "Senha inválida"));` |
| 4 | `RK-e84d750f340a` | Código não cadastrado! | `!Empty(This.Value)` <br> `Messagebox('Código Não cadastrado!'+Chr(13)+'Deseja Cadastrar agora?',4+32+256,sistema) = 6` <br> `Reccount() = 0` | src/Mundial.Aplicacao/Cadastro.cs:57<br>`return ResultadoRegra.Recusa("RK-e84d750f340a", "Código não cadastrado!");` |
| 5 | `RK-2976e3756f6d` | Código já cadastrado para o Produto  | `_tally > 0` | src/Mundial.Aplicacao/Cadastro.cs:98<br>`return ResultadoRegra.Recusa( slot switch { 0 => "RK-2976e3756f6d", 1 => "RK-ab467d52fa1f", _ => "RK-f3bda1fa3b77" },` |
| 6 | `RK-ab467d52fa1f` | Código já cadastrado para o Produto  | `_tally > 0` | src/Mundial.Aplicacao/Cadastro.cs:98<br>`return ResultadoRegra.Recusa( slot switch { 0 => "RK-2976e3756f6d", 1 => "RK-ab467d52fa1f", _ => "RK-f3bda1fa3b77" },` |
| 7 | `RK-f3bda1fa3b77` | Código já cadastrado para o Produto  | `_tally > 0` | src/Mundial.Aplicacao/Cadastro.cs:98<br>`return ResultadoRegra.Recusa( slot switch { 0 => "RK-2976e3756f6d", 1 => "RK-ab467d52fa1f", _ => "RK-f3bda1fa3b77" },` |
| 8 | `RK-5a7aaaa8862d` | Código não cadastrado! | `!Empty(This.Value)` <br> `Messagebox('Código Não cadastrado!'+Chr(13)+'Deseja Cadastrar agora?',4+32+256,sistema) = 6` <br> `Reccount() = 0` | src/Mundial.Aplicacao/Cadastro.cs:31<br>`return ResultadoRegra.Recusa("RK-5a7aaaa8862d", "Informe o código do produto.");` |
| 9 | `RK-bdfbdff6c821` | Confirma Exclusão? | `messagebox('Confirma Exclusão?',4+32+256,sistema) = 6` | src/Mundial.Aplicacao/Conferencia.cs:132<br>`return ResultadoRegra.Confirma("RK-bdfbdff6c821", "Confirma Exclusão?");` |
| 10 | `RK-fa93a48fbecc` | Finalizar conferência? | `Messagebox('Finalizar conferência?',4+32+256,sistema) = 6` | src/Mundial.Dominio/Documento.cs:67<br>`return ResultadoRegra.Confirma("RK-fa93a48fbecc", aviso);` |
| 11 | `RK-c0fce5362f62` | Documento não cadastrado! | `Reccount()=0` | src/Mundial.Aplicacao/Conferencia.cs:30<br>`return (null, ResultadoRegra.Recusa("RK-c0fce5362f62", "Documento não cadastrado!"));` |
| 12 | `RK-6fef4d31a290` | Código Não cadastrado! | `!Empty(This.Value)` <br> `Messagebox('Código Não cadastrado!'+Chr(13)+'Deseja Cadastrar agora?',4+32+256,sistema) = 6` <br> `Reccount() = 0` | src/Mundial.Aplicacao/Conferencia.cs:47<br>`public async Task<LeituraResultado> Executar(Documento documento, string codigoBipado, bool podeIncluir = false, CancellationToken ct = default) {` |
| 13 | `RK-798f00f19690` | Código Não cadastrado! | `!Empty(This.Value)` <br> `Messagebox('Código Não cadastrado!'+Chr(13)+'Deseja Cadastrar agora?',4+32+256,sistema) = 6` <br> `Reccount() = 0` | src/Mundial.Aplicacao/Conferencia.cs:59<br>`if (achados.Count == 0) return new("recusado", "RK-798f00f19690", "Código Não cadastrado!", null, null,` |
| 14 | `RK-dab7d2033e2e` | Código Não cadastrado! Deseja Cadastrar agora? | `Reccount()=0` | src/Mundial.Aplicacao/Conferencia.cs:49<br>`public async Task<LeituraResultado> Executar(Documento documento, string codigoBipado, bool podeIncluir = false, CancellationToken ct = default) {` |
| 15 | `RK-732bb9300bad` | Código Não cadastrado para | `Reccount()=0` | src/Mundial.Aplicacao/Conferencia.cs:72<br>`if (item is null) return new("recusado", "RK-732bb9300bad",` |
| 16 | `RK-ff51aa26bf33` | Este Documento já foi conferido! | `!Empty(This.Value)` <br> `fechado = .T.` | src/Mundial.Dominio/Documento.cs:28<br>`public ResultadoRegra AvaliarAbertura() => Fechado ? ResultadoRegra.Recusa("RK-69b41cd017dd", "Este Documento já foi conferido!")` |
| 17 | `RK-69b41cd017dd` | Este Documento já foi conferido! | `!Empty(This.Value)` <br> `fechado = .T.` | src/Mundial.Aplicacao/Conferencia.cs:95<br>`return ResultadoRegra.Recusa("RK-69b41cd017dd", "Este Documento já foi conferido!");` |
| 18 | `RK-cc8cfa3658d1` | Este Documento já foi lançado! | `Found()` <br> `Messagebox('Este Documento já foi lançado!'+Chr(13)+'Confirma assim mesmo?',4+32+256,sistema) = 7` | src/Mundial.Dominio/Documento.cs:39<br>`public ResultadoRegra AvaliarRelancamento() => ItensLancados > 0 && !Fechado ? ResultadoRegra.Confirma("RK-45e526801fea",` |
| 19 | `RK-45e526801fea` | Este Documento já foi lançado! | `Found()` <br> `Messagebox('Este Documento já foi lançado!'+Chr(13)+'Confirma assim mesmo?',4+32+256,sistema) = 7` | src/Mundial.Dominio/Documento.cs:43<br>`=> ItensLancados > 0 && !Fechado ? ResultadoRegra.Confirma("RK-45e526801fea",` |
| 20 | `RK-a7f3c0eb65c1` | Fornecedor diferente! | `Messagebox('Fornecedor diferente!'+Chr(13)+'Confirma este fornecedor?',4+32+256,sistema) = 7` | src/Mundial.Dominio/Documento.cs:52<br>`&& !string.Equals(fornecedorEsperado, CodigoFornecedor, StringComparison.OrdinalIgnoreCase) ? ResultadoRegra.Confirma("RK-a7f3c0eb65c1",` |
| 21 | `RK-b3e7fcc26f3e` | cgc is required | `cgc IS NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:52<br>`Texto(Cgc, "RK-b3e7fcc26f3e", "cgc");` |
| 22 | `RK-ef82abb7456c` | cod_com is required | `cod_com IS NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:53<br>`Texto(CodCom, "RK-ef82abb7456c", "cod_com");` |
| 23 | `RK-b5da8c743238` | categ is required | `categ IS NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:54<br>`Texto(Categoria, "RK-b5da8c743238", "categ");` |
| 24 | `RK-e74f29d4f922` | tiplog is required | `tiplog IS NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:55<br>`Texto(TipoLogradouro, "RK-e74f29d4f922", "tiplog");` |
| 25 | `RK-2ce1876d83ad` | lograd is required | `lograd IS NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:56<br>`Texto(Logradouro, "RK-2ce1876d83ad", "lograd");` |
| 26 | `RK-1d4194439839` | bairro is required | `bairro IS NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:57<br>`Texto(Bairro, "RK-1d4194439839", "bairro");` |
| 27 | `RK-4697ebd74678` | cep is required | DDL do legado: `cep       CHAR(9)  NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:58<br>`Texto(Cep, "RK-4697ebd74678", "cep");` |
| 28 | `RK-854f2452216e` | cidade is required | DDL do legado: `cidade    CHAR(25) NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:59<br>`Texto(Cidade, "RK-854f2452216e", "cidade");` |
| 29 | `RK-98835efbf746` | uf is required | DDL do legado: `uf        CHAR(2)  NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:60<br>`Texto(Uf, "RK-98835efbf746", "uf");` |
| 30 | `RK-6aff3b12acb2` | inscr is required | DDL do legado: `inscr     CHAR(18) NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:61<br>`Texto(Inscricao, "RK-6aff3b12acb2", "inscr");` |
| 31 | `RK-353ee013c009` | data_grav is required | DDL do legado: `data_grav DATETIME2 NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:62<br>`if (DataGravacao is null) faltas.Add(ResultadoRegra.Recusa("RK-353ee013c009", "data_grav is required"));` |
| 32 | `RK-37afeda868c2` | sub_trib is required | DDL do legado: `sub_trib  BIT NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:63<br>`if (SubstituicaoTributaria is null) faltas.Add(ResultadoRegra.Recusa("RK-37afeda868c2", "sub_trib is required"));` |
| 33 | `RK-f2ca891c315f` | Mov_Est is required | DDL do legado: `Mov_Est   BIT NOT NULL` | src/Mundial.Dominio/Fornecedor.cs:64<br>`if (MovimentaEstoque is null) faltas.Add(ResultadoRegra.Recusa("RK-f2ca891c315f", "Mov_Est is required"));` |
| 34 | `RK-8233e231d6fb` | Este Código já tem Qtde lançada ( | `Messagebox('Este Código já tem Qtde lançada ('+Trans(qtd_rec)+')!'+Chr(13)+'Deseja lança-lo assim mesmo?',4+32+256,sistema) = 7` <br> `qtd_rec > 0` | src/Mundial.Dominio/ItemConferencia.cs:36<br>`=> QtdRec > 0 ? ResultadoRegra.Confirma("RK-8233e231d6fb",` |
| 35 | `RK-5960908935ee` | Este Código já tem Qtde lançada ( | `Messagebox('Este Código já tem Qtde lançada ('+Trans(qtd_rec)+')!'+Chr(13)+'Deseja lança-lo assim mesmo?',4+32+256,sistema) = 7` <br> `qtd_rec > 0` | src/Mundial.Dominio/ItemConferencia.cs:33<br>`public ResultadoRegra AvaliarLancamento() => QtdRec > 0 ? ResultadoRegra.Confirma("RK-8233e231d6fb",` |
| 36 | `RK-ea5a22eaf219` | descri is required | `descri IS NOT NULL` | src/Mundial.Aplicacao/Cadastro.cs:33<br>`return ResultadoRegra.Recusa("RK-ea5a22eaf219", "descri is required");` |
| 37 | `RK-fa1ca141cf21` | alterar is required | `alterar IS NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:29<br>`if (alterar is null) faltas.Add(ResultadoRegra.Recusa("RK-fa1ca141cf21", "alterar is required"));` |
| 38 | `RK-6022cae899fa` | incluir is required | `incluir IS NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:30<br>`if (incluir is null) faltas.Add(ResultadoRegra.Recusa("RK-6022cae899fa", "incluir is required"));` |
| 39 | `RK-be780ff12c0e` | excluir is required | `excluir IS NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:31<br>`if (excluir is null) faltas.Add(ResultadoRegra.Recusa("RK-be780ff12c0e", "excluir is required"));` |
| 40 | `RK-04c918661d8d` | consultar is required | `consultar IS NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:32<br>`if (consultar is null) faltas.Add(ResultadoRegra.Recusa("RK-04c918661d8d", "consultar is required"));` |
| 41 | `RK-82c929f4e851` | peso_bruto_col is required | DDL do legado: `peso_bruto_col DECIMAL(11,3) NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:42<br>`if (pesoBrutoCol is null) faltas.Add(ResultadoRegra.Recusa("RK-82c929f4e851", "peso_bruto_col is required"));` |
| 42 | `RK-c5a64175c9a1` | balanca is required | DDL do legado: `balanca BIT NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:43<br>`if (balanca is null) faltas.Add(ResultadoRegra.Recusa("RK-c5a64175c9a1", "balanca is required"));` |
| 43 | `RK-16bc1acd7b74` | situacao is required | DDL do legado: `situacao  CHAR(1)  NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:44<br>`if (situacao is null or ' ') faltas.Add(ResultadoRegra.Recusa("RK-16bc1acd7b74", "situacao is required"));` |
| 44 | `RK-d1a55f1103db` | nome is required | `nome IS NOT NULL` | src/Mundial.Dominio/Obrigatorios.cs:15<br>`faltas.Add(ResultadoRegra.Recusa("RK-d1a55f1103db", "nome is required"));` |
| 45 | `RK-a0bb1eeee55d` | Este Código já esta cadastrado! | `!empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb2) and !empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb3) and !empty(this.value)` | src/Mundial.Dominio/Produto.cs:25<br>`public ResultadoRegra AvaliarDuplicidadeInterna(int slot, string? valor) { if (string.IsNullOrWhiteSpace(valor)) return ResultadoRegra.Ok;` |
| 46 | `RK-99e9bfdcea75` | Este Código já esta cadastrado! | `!empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb2) and !empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb3) and !empty(this.value)` | src/Mundial.Dominio/Produto.cs:46<br>`{ 0 => "RK-99e9bfdcea75",` |
| 47 | `RK-f9e0b12a76af` | Este Código já esta cadastrado | `!empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb2) and !empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb3) and !empty(this.value)` | src/Mundial.Dominio/Produto.cs:27<br>`public ResultadoRegra AvaliarDuplicidadeInterna(int slot, string? valor) { if (string.IsNullOrWhiteSpace(valor)) return ResultadoRegra.Ok;` |
| 48 | `RK-4ca8df36a760` | Este Código já esta cadastrado | `!empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb2) and !empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb3) and !empty(this.value)` | src/Mundial.Dominio/Produto.cs:47<br>`0 => "RK-99e9bfdcea75", 1 => "RK-4ca8df36a760",` |
| 49 | `RK-41493150036e` | Este Código já esta cadastrado | `!empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb2) and !empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb3) and !empty(this.value)` | src/Mundial.Dominio/Produto.cs:29<br>`public ResultadoRegra AvaliarDuplicidadeInterna(int slot, string? valor) { if (string.IsNullOrWhiteSpace(valor)) return ResultadoRegra.Ok;` |
| 50 | `RK-ab62193a2b2d` | Este Código já esta cadastrado | `!empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb2) and !empty(this.value)` <br> `(this.value = barr_emb or this.value = barr_emb3) and !empty(this.value)` | src/Mundial.Dominio/Produto.cs:48<br>`_ => "RK-ab62193a2b2d"` |
| 51 | `RK-5b2436bca3f0` | Tem certeza que deseja excluir este código? | `messagebox('Tem certeza que deseja excluir este código?',4+32+256,sistema) = 6` <br> `muda and empty(this.value) and val(estoq.barr_emb)>0` <br> `muda and empty(this.value) and val(estoq.barr_emb2)>0` | src/Mundial.Dominio/Produto.cs:69<br>`{ 0 => "RK-5b2436bca3f0",` |
| 52 | `RK-2c78478f0b97` | Tem certeza que deseja excluir este código? | `messagebox('Tem certeza que deseja excluir este código?',4+32+256,sistema) = 6` <br> `muda and empty(this.value) and val(estoq.barr_emb)>0` <br> `muda and empty(this.value) and val(estoq.barr_emb2)>0` | src/Mundial.Dominio/Produto.cs:70<br>`0 => "RK-5b2436bca3f0", 1 => "RK-2c78478f0b97",` |
| 53 | `RK-9f92b8e2a3c0` | Tem certeza que deseja excluir este código? | `messagebox('Tem certeza que deseja excluir este código?',4+32+256,sistema) = 6` <br> `muda and empty(this.value) and val(estoq.barr_emb)>0` <br> `muda and empty(this.value) and val(estoq.barr_emb2)>0` | src/Mundial.Dominio/Produto.cs:71<br>`_ => "RK-9f92b8e2a3c0"` |
| 54 | `RK-ade9dd1661d1` | Tem certeza que deseja excluir este código? | `messagebox('Tem certeza que deseja excluir este código?',4+32+256,sistema) = 6` <br> `muda and empty(this.value) and val(estoq.barr_emb)>0` <br> `muda and empty(this.value) and val(estoq.barr_emb2)>0` | src/Mundial.Dominio/Produto.cs:59<br>`public ResultadoRegra AvaliarExclusao(int slot, string? novoValor) => string.IsNullOrWhiteSpace(novoValor) && !string.IsNullOrWhiteSpace(Dun[slot]) ? ResultadoRegra.Confi` |
| 55 | `RK-305af19071c6` | Tem certeza que deseja excluir este código? | `messagebox('Tem certeza que deseja excluir este código?',4+32+256,sistema) = 6` <br> `muda and empty(this.value) and val(estoq.barr_emb)>0` <br> `muda and empty(this.value) and val(estoq.barr_emb2)>0` | src/Mundial.Dominio/Produto.cs:60<br>`public ResultadoRegra AvaliarExclusao(int slot, string? novoValor) => string.IsNullOrWhiteSpace(novoValor) && !string.IsNullOrWhiteSpace(Dun[slot]) ? ResultadoRegra.Confi` |
| 56 | `RK-21ac9f1bddea` | Tem certeza que deseja excluir este código? | `messagebox('Tem certeza que deseja excluir este código?',4+32+256,sistema) = 6` <br> `muda and empty(this.value) and val(estoq.barr_emb)>0` <br> `muda and empty(this.value) and val(estoq.barr_emb2)>0` | src/Mundial.Dominio/Produto.cs:61<br>`public ResultadoRegra AvaliarExclusao(int slot, string? novoValor) => string.IsNullOrWhiteSpace(novoValor) && !string.IsNullOrWhiteSpace(Dun[slot]) ? ResultadoRegra.Confi` |
| 57 | `RK-9f4468b42859` | Transicao de estado: barr_emb3 = '' | `barr_emb3 = ''` <br> `campo = '^FO110,335^A0R,55,35^FD'+vcodbarr+'^FS'` <br> `campo = '^FO210,270^BCR,200,Y,N,N^FD'+vcodbarr+'^FS'` | src/Mundial.Dominio/Produto.cs:79<br>`public bool TerceiroSlotVazio() => string.IsNullOrWhiteSpace(Dun[2]); /// <summary>` |
| 58 | `RK-75e2169fe930` | Transicao de estado: barr_emb3 = '' | `barr_emb3 = ''` <br> `campo = '^FO110,335^A0R,55,35^FD'+vcodbarr+'^FS'` <br> `campo = '^FO210,270^BCR,200,Y,N,N^FD'+vcodbarr+'^FS'` | src/Mundial.Dominio/Produto.cs:80<br>`public bool TerceiroSlotVazio() => string.IsNullOrWhiteSpace(Dun[2]); /// <summary>` |
| 59 | `RK-dfe2ca45ec1a` | Transicao de estado: barr_emb3 = '' | `barr_emb3 = ''` <br> `campo = '^FO110,335^A0R,55,35^FD'+vcodbarr+'^FS'` <br> `campo = '^FO210,270^BCR,200,Y,N,N^FD'+vcodbarr+'^FS'` | src/Mundial.Dominio/Produto.cs:81<br>`public bool TerceiroSlotVazio() => string.IsNullOrWhiteSpace(Dun[2]); /// <summary>` |
| 60 | `RK-3b8ef53b6cf2` | Código EAN não é desse DUN-14! | `This.Value # produto.codbarr And This.Value # produto.codbarr2 And This.Value # produto.codbarr3 AND VAL(produto.codbarr) # 0` | src/Mundial.Dominio/Produto.cs:92<br>`? ResultadoRegra.Ok : ResultadoRegra.Recusa("RK-3b8ef53b6cf2", "Código EAN não é desse DUN-14!");` |
| 61 | `RK-8ffd715ce9ad` | Você não está autorizado a usar este Sistema | `vsenha < 3` | src/Mundial.Dominio/Usuario.cs:27<br>`return nivel < NivelMinimo ? ResultadoRegra.Recusa("RK-8ffd715ce9ad", "Você não está autorizado a usar este Sistema")` |
| 62 | `RK-b382d85d0edc` | campo = '^XA' | `campo = '^XA'` | src/Mundial.Infraestrutura/Etiquetas/GeradorZpl.cs:16<br>`public string Gerar(Produto produto, string codigoBarras) { var descricao = produto.Descricao.Trim();` |
| 63 | `RK-0811a89bc8e6` | ^FO510,40 descrição do produto | `campo = '^FO510,40^A0R,150,36^FD'+estoq.Descri+'^FS'` | src/Mundial.Infraestrutura/Etiquetas/GeradorZpl.cs:17<br>`public string Gerar(Produto produto, string codigoBarras) { var descricao = produto.Descricao.Trim();` |
| 64 | `RK-2b3c11b27fef` | ^FO420,360 embalagem com quantidade | `campo = '^FO420,360^A0R,100,50^FD'+Alltrim(estoq.embalag)+' c/ '+Trans(estoq.embalqt)+'^FS'` | src/Mundial.Infraestrutura/Etiquetas/GeradorZpl.cs:18<br>`public string Gerar(Produto produto, string codigoBarras) { var descricao = produto.Descricao.Trim();` |
| 65 | `RK-25721748a2b1` | ^FO210,270 código de barras | `campo = '^FO210,270^BCR,200,Y,N,N^FD'+vcodbarr+'^FS'` | src/Mundial.Infraestrutura/Etiquetas/GeradorZpl.cs:19<br>`public string Gerar(Produto produto, string codigoBarras) { var descricao = produto.Descricao.Trim();` |
| 66 | `RK-3ff169d79617` | ^FO220,270 código de barras | `campo = '^FO220,270^BCR,200,Y,N,N^FD'+vcodbarr+'^FS'` | src/Mundial.Infraestrutura/Etiquetas/GeradorZpl.cs:20<br>`public string Gerar(Produto produto, string codigoBarras) { var descricao = produto.Descricao.Trim();` |
| 67 | `RK-1b386e3870da` | ^FO110,335 código legível | `campo = '^FO110,335^A0R,55,35^FD'+vcodbarr+'^FS'` | src/Mundial.Infraestrutura/Etiquetas/GeradorZpl.cs:21<br>`public string Gerar(Produto produto, string codigoBarras) { var descricao = produto.Descricao.Trim();` |
| 68 | `RK-e8876989538a` | campo = '^XZ' | `campo = '^XZ'` | src/Mundial.Infraestrutura/Etiquetas/GeradorZpl.cs:22<br>`public string Gerar(Produto produto, string codigoBarras) { var descricao = produto.Descricao.Trim();` |

### 9.1 As duas regras descartadas, e por quê

| Chave | Mensagem no legado | Condição FoxPro | Decisão |
| --- | --- | --- | --- |
| `RK-d0605132c1f1` | Não foi possível criar uma conexão ODBC! | `gncom < 1` | Descartada — `AD-14` |
| `RK-a709080069ea` | Não foi possível criar uma conexão ODBC! | `lnResp < 0` | Descartada — `AD-14` |

As duas eram tratamento de falha da infraestrutura do FoxPro: `gncom` é o *handle* devolvido por
`SQLStringConnect()` e `lnResp` é o retorno de `SQLExec()`. Três razões para não portá-las:

1. **Não decidem nada de negócio** — nenhum dado gravado, nenhuma escolha do operador, nenhum estado
   do documento envolvido.
2. **A condição não existe no destino** — no .NET não há *handle* numérico para testar; a conexão
   abre ou lança exceção.
3. **O sistema do outro lado é este mesmo** — a conexão `estok_sgm` apontava para o próprio banco
   `sgm`. Não havia sistema externo cujo erro merecesse virar regra.

O critério aplicado: **regra de negócio se enuncia sem citar tecnologia.** "Código que já tem
quantidade lançada exige confirmação" sobrevive à troca de linguagem e de banco. "Se o handle ODBC
vier menor que 1, avise" morre junto com o ODBC. O que sobrevive dessas duas é uma preocupação
operacional — banco indisponível durante a conferência — que pertence ao tratamento de erro e à
observabilidade, não ao catálogo de regras.

### 9.2 A prova de que a tabela está correta

A rastreabilidade não é conferida à mão. Um verificador roda no repositório e cruza as três pontas —
regra do UIR, atributo no código, teste citando a chave:

```
$ python3 tools/rastreabilidade.py
RASTREABILIDADE ruleKey -> teste  (AD-5, AD-20)
==============================================================
regras no UIR .............. 70
implementadas no código .... 68
com teste citando a chave .. 68

2 regra(s) do UIR ainda sem implementação:
  - RK-a709080069ea
  - RK-d0605132c1f1

Toda regra implementada tem teste citando a mesma chave.
```

E a suíte inteira:

```
tests/Mundial.Testes ............. 92 testes, 0 falhas
tests/Mundial.Testes.Arquitetura . 10 testes, 0 falhas
```

---

## 10. Verificação visual: 24 telas

As capturas abaixo saíram da aplicação **em execução**, não de protótipo. Um roteiro automatizado
percorre os mesmos passos do documento de teste manual e fotografa cada um — se a tela mudar, a
captura muda junto. Arquivos em `tools/capturas/`.

| Figura | Arquivo | Título | Legenda |
| --- | --- | --- | --- |
| 1 | `01-tela-de-entrada.png` | Tela de entrada | A primeira tela. Foco já no campo de matrícula — quem opera na doca digita e tecla Enter. |
| 2 | `02-matricula-nao-cadastrada.png` | Matrícula não cadastrada | Matrícula que não existe. A mensagem é o texto literal do legado, e a chave da regra é `RK-046f5592ef5b`. |
| 3 | `03-senha-invalida.png` | Senha inválida | Senha errada. "Senha inválida" — `RK-f8293cf9dbb3`. Não revela se a matrícula existe. |
| 4 | `04-nivel-insuficiente.png` | Nível insuficiente | Paulo tem nível 1. Barrado antes de qualquer tela — `RK-8ffd715ce9ad`. |
| 5 | `05-painel-de-docas.png` | Painel de docas | Quatro docas. A doca 2 vem primeiro, com anel âmbar, porque está aberta há mais de três horas. |
| 6 | `06-conferencia-aberta.png` | Conferência aberta | Doca 1, Bebidas Primavera. Três itens lançados, um pendente, e a cerveja com divergência. |
| 7 | `07-leitura-aceita.png` | Leitura aceita | Suco de uva aceito. Selo verde, descrição, embalagem e o campo de quantidade preenchido. |
| 8 | `08-item-lancado.png` | Item lançado | Lançado. A linha entra com pílula ok, a pendência some e o foco volta ao campo de leitura. |
| 9 | `09-confirmacao-de-requantidade.png` | Confirmação de requantidade | Ao gravar 38 sobre os 40 existentes, `RK-8233e231d6fb` pede confirmação. |
| 10 | `10-substituiu-nao-somou.png` | Substituiu, não somou | Confirmado: o valor virou 38 — substituiu os 40, não somou para 78. |
| 11 | `11-codigo-nao-cadastrado.png` | Código não cadastrado | Código inexistente. Som diferente, selo vermelho. Cleber não tem permissão de inclusão. |
| 12 | `12-codigo-de-outro-fornecedor.png` | Código de outro fornecedor | Produto existe no cadastro, mas não está nesta nota. `RK-732bb9300bad` nomeia o fornecedor. |
| 13 | `13-leitura-ambigua.png` | Leitura ambígua | O mesmo código responde a dois produtos. O sistema recusa e mostra os candidatos. |
| 14 | `14-finalizar-conferencia.png` | Finalizar conferência | F2 finaliza. `RK-fa93a48fbecc` pergunta uma vez e avisa quantos itens ficam pendentes. |
| 15 | `15-documento-fechado.png` | Documento fechado | Fechado é irreversível: faixa âmbar, campo de leitura some, nada de botão. |
| 16 | `16-doca-aguardando.png` | Doca aguardando | Doca 3 vista pela supervisora. Nada lançado ainda — todos os itens em aguarda. |
| 17 | `17-oferta-de-cadastrar.png` | Oferta de cadastrar | Mesmo código inexistente, agora com permissão de inclusão: a recusa vira oferta. |
| 18 | `18-somente-leitura.png` | Somente leitura | Documento fechado, em modo consulta, com quem fechou registrado. |
| 19 | `19-cadastro-de-codigos.png` | Cadastro de códigos | Os três slots de DUN-14, com prévia da etiqueta e o ZPL cru para inspeção. |
| 20 | `20-codigo-de-outro-produto.png` | Código de outro produto | Código que já pertence a outro produto. O erro aparece no campo que o causou. |
| 21 | `21-codigo-repetido-no-produto.png` | Código repetido no produto | Mesmo código em dois slots do mesmo produto. Outra regra, outro campo. |
| 22 | `22-consulta-de-conferencias.png` | Consulta de conferências | A tabela densa do supervisor: densidade alta, sem cartão decorativo. |
| 23 | `23-consulta-de-fornecedores.png` | Consulta de fornecedores | Fornecedores com as treze regras de obrigatoriedade rodando sobre o dado gravado. |
| 24 | `24-trilha-de-auditoria.png` | Trilha de auditoria | A trilha no formato do legado, recuperada da função `reg_log` do FoxPro: quem, quando, o quê. |

---

## 11. Infraestrutura, publicação e pipeline

### 11.1 O ambiente na Tencent Cloud

| Item | Configuração |
| --- | --- |
| Provedor / região | Tencent Cloud · `sa-saopaulo` |
| Máquina | CVM `S5.LARGE8` (Ubuntu Server 24.04 LTS) |
| Provisionamento | **Terraform**, do zero: VPC, sub-rede, security group, CVM, disco e DNS |
| Configuração inicial | `cloud-init` — instala Docker, escreve o compose e os segredos, sobe o banco |
| Proxy / TLS | **Caddy**, certificado automático; tudo servido na mesma origem |
| DNS | DNSPod, zona `exai.extreme.digital` |
| **Endereço público da POC** | **`https://poc-mundial.exai.extreme.digital`** |
| Banco | SQL Server 2022 em contêiner, com volume nomeado — sobrevive a `down`/`up` |
| Rede | Porta 1433 e porta da API **não** expostas; só 80/443 pelo proxy. SSH liberado para **um único IP** |

Nada é clicado no console: `terraform apply` reconstrói o ambiente inteiro. Os segredos são gerados
**na primeira inicialização da máquina** e nunca passam pelo estado do Terraform.

### 11.2 O pipeline

O deploy é **pull**, não push: a máquina busca o código; o GitHub não entra na máquina.

```
push na main
   │
   ├── GitHub Actions: roda a suíte de testes
   │
   └── na Tencent: agente (systemd timer, a cada minuto)
          compara origin/main com o que está implantado
          │
          ├── nada que roda mudou (só .md, docs, terraform)? registra e sai
          │
          └── mudou: constrói as imagens, marca a anterior para rollback,
              sobe migrações + api + web, faz health check
                 ├── passou  → grava /deploy.json {situacao: ok, commit}
                 └── falhou  → volta para a imagem anterior e grava "revertido"

GitHub Actions então lê https://poc-mundial.exai.extreme.digital/deploy.json
até ver o próprio commit com situação "ok" — e roda a fumaça de fora:
saúde da API, frontend respondendo, dado presente.
```

**Por que pull.** Empurrar do GitHub exigiria abrir a porta 22 para as faixas de IP dos *runners* —
milhares, que mudam — e guardar uma chave privada nos segredos do repositório. O modelo pull mantém o
SSH fechado para o mundo e a chave fora do repositório.

**Três propriedades que valem citar ao cliente:**

- **Rollback automático:** health check falhou, a versão anterior volta sozinha. A imagem anterior
  fica na máquina, então o retorno não depende de reconstruir nada.
- **O banco nunca é recriado** no deploy. Só migrações, API e front trocam.
- **A confirmação vem de fora.** O pipeline não confia no seu próprio log: ele lê o `/deploy.json`
  publicado pela máquina e exige o SHA do commit. Se a POC não estiver servindo aquele commit, o
  build falha.

### 11.3 Como rodar na máquina de qualquer pessoa

```bash
cp .env.example .env
docker compose up --build
```

Sobem banco, migrações, API e front. A massa de demonstração é semeada sozinha quando o banco está
vazio — a POC funciona a partir de um clone limpo, sem passo manual. Há ainda um endpoint de
**reset** entre apresentações, que devolve o ambiente ao estado inicial.

---

## 12. Decisões abertas, riscos e próximos passos

### 12.1 A decisão que precisa de confirmação do cliente

**A quantidade conferida substitui ou soma?** A lógica ficou num formulário binário que não foi
retido — não há prova textual. Decidimos **substitui**, com confirmação explícita, por evidência
estrutural:

- `qtd_rec` é campo escalar do item da nota, ao lado de `qtd_nf`. É um valor conferido, não um
  acumulador de eventos.
- Não existe tabela de lançamentos — nada no schema registra bipagens individuais.
- O aviso do legado só faz sentido protegendo ação destrutiva: somar não precisaria de confirmação.
- O legado abria as tabelas com `Set Exclusive On`, o que enfraquece a hipótese de acúmulo por
  vários operadores.

**É reversível:** confirmando-se o contrário com a operação, muda-se um requisito e uma decisão de
arquitetura, juntos.

### 12.2 Fora do escopo desta POC, por decisão registrada

| Item | Quando revisitar |
| --- | --- |
| Migração do dado histórico (DBF → SQL Server) | Quando o cliente definir a janela de corte |
| Integração ODBC com o ERP | Quando alguém com acesso ao ERP documentar o contrato |
| Envio físico da etiqueta para a Zebra | Quando houver impressora disponível para teste (a montagem do ZPL já está pronta e testada) |
| Tabela `entrada` e módulo `esco_imp` | Quando o escopo de integração for definido |

### 12.3 Próximos passos recomendados

1. **Confirmar a regra de substituição** com a operação do armazém (§ 12.1).
2. **Adotar OpenID Connect** antes do segundo legado modernizado (§ 8).
3. **Tratamento global de erro na API**: falha de infraestrutura deve devolver o mesmo contrato de
   erro do resto do sistema, com mensagem legível ao operador — é o que sobra, como NFR, das duas
   regras ODBC descartadas.
4. **Planejar a migração de dado** com janela de corte e validação assistida.
5. **Definir o piloto**: uma doca real, operadores reais, por um período determinado, com o legado
   ainda disponível como retorno.

---

## 13. O que este projeto demonstra

- **Legado ilegível pode ser lido.** 70 regras saíram de dentro de formulários FoxPro e viraram
  catálogo consultável — sem depender de quem escreveu o sistema.
- **Equivalência pode ser provada, não prometida.** A chave da regra atravessa UIR, requisito,
  código, teste e mensagem de tela. Qualquer pessoa audita com um comando.
- **Agente com método entrega diferente de agente sem método.** O que trava o resultado é o
  encadeamento de artefatos e os testes de arquitetura, não a habilidade do modelo.
- **A modernização começa a pagar antes de terminar.** A POC roda de ponta a ponta, num endereço
  público, publicada por pipeline, com rollback — e o mesmo caminho serve para o próximo sistema.

---

## Anexo A — Glossário

| Termo | O que é |
| --- | --- |
| **RNC** | Plataforma de engenharia reversa que lê o legado e produz o UIR |
| **UIR** | Representação intermediária: telas, dados e regras com chave estável |
| **MCP Server** | Protocolo que expõe o RNC como ferramenta para o agente de codificação |
| **BMAD** | Método de desenvolvimento assistido por agentes, orientado a artefatos encadeados |
| **`RK-…`** | Chave estável de uma regra de negócio do legado |
| **`AD-…`** | Decisão de arquitetura registrada no *spine* |
| **`FR-…` / `NFR-…`** | Requisito funcional / não funcional do PRD |
| **DUN-14 / EAN-13** | Código de barras da embalagem / da unidade de venda |
| **ZPL** | Linguagem de impressão das etiquetadoras Zebra |
| **RFC 9457** | Padrão de corpo de erro HTTP (`application/problem+json`) |
| **OIDC** | OpenID Connect — camada de identidade sobre OAuth 2.0 |
| **Ports & adapters** | Arquitetura com domínio isolado de banco, HTTP e telas |

## Anexo B — Onde conferir cada afirmação

| Afirmação | Fonte no repositório |
| --- | --- |
| 70 regras, 68 implementadas com teste | `tools/rastreabilidade.py` |
| Regras no código | atributo `[RegraNegocio("RK-…", "…")]` |
| Condições do legado | `docs/historico-rnc/prd.md` (pacote gerado pelo RNC, preservado) |
| Divergências do pacote automático | `_bmad-output/planning-artifacts/uir-gap-report.md` |
| Leitura da fonte legada | `_bmad-output/planning-artifacts/achados-fonte-legada.md` |
| Requisitos | `_bmad-output/planning-artifacts/prds/…/prd.md` |
| Decisões de arquitetura | `…/architecture/…/ARCHITECTURE-SPINE.md` |
| Épicos e stories | `_bmad-output/planning-artifacts/epics.md` |
| Testes de arquitetura | `tests/Mundial.Testes.Arquitetura/Invariantes.cs` |
| Roteiro de teste manual | `ROTEIRO-DE-TESTE.md` |
| Infraestrutura | `infra/terraform/`, `infra/deploy/` |
| Pipeline | `.github/workflows/deploy.yml` |
| Capturas | `tools/capturas/` |
