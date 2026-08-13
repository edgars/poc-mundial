# Mundial · Conferência de Recebimento

Modernização do sistema de conferência de recebimento de mercadoria do **Supermercados Mundial** —
do legado em Visual FoxPro (`dun14.SCX`, escrito em 2011, remendado até 2021) para uma base que a
Mundial consiga manter, auditar e evoluir.

Este repositório contém o **planejamento**. O código ainda não foi escrito.

## O que o sistema faz

O operador de doca confere a carga que chega: bipa o documento fiscal, bipa cada produto, registra a
quantidade recebida, fecha a conferência de forma irreversível e imprime a etiqueta da embalagem. O
supervisor cadastra códigos de barras, libera exceções e acompanha o que foi conferido.

Não é um CRUD. É uma estação de trabalho de armazém, e o valor está em preservar esse processo
inteiro — inclusive as 70 regras de negócio que o RNC recuperou do código original.

## Como isto foi produzido

```
legado Visual FoxPro  →  RNC (engenharia reversa)  →  UIR  →  planejamento BMAD  →  código
                                                              ▲ você está aqui
```

O **RNC** leu o legado e produziu o UIR — telas, modelo de dados, 70 regras de negócio com chaves
estáveis `RK-…`, e o código-fonte retido. O **BMAD** transformou isso em requisitos, arquitetura e
stories. Cada regra implementada carrega sua chave, e pode ser conferida contra a fonte legada com
`getRule()` do MCP do RNC.

## Documentos de planejamento

Tudo em **`_bmad-output/planning-artifacts/`**:

| Documento | Conteúdo |
| --- | --- |
| **`prds/prd-poc-mundial-2026-08-10/prd.md`** | 54 requisitos funcionais, 12 não-funcionais, 3 jornadas de usuário |
| **`architecture/architecture-poc-mundial-2026-08-10/ARCHITECTURE-SPINE.md`** | 21 decisões de arquitetura (`AD-1`…`AD-21`) |
| **`epics.md`** | 5 épicos, 30 stories com critérios de aceite testáveis |
| `achados-fonte-legada.md` | O que a leitura do FoxPro original revelou — e o que desmentiu |
| `uir-gap-report.md` | As 7 divergências entre o UIR e o pacote gerado automaticamente |

⚠️ **`docs/historico-rnc/` não serve para construir.** É o pacote gerado automaticamente,
preservado por rastreabilidade. Contém erros que produziriam o aplicativo errado — veja
`docs/README.md`.

## Stack

| Camada | Tecnologia |
| --- | --- |
| Frontend | Angular 22 · TypeScript 6 · Node 24 LTS |
| Backend | .NET 10 (LTS) · C# 14 · Dapper 2.1.79 |
| Banco | SQL Server 2022 · migrations com DbUp |
| Entrega | Docker Compose |

Arquitetura em **ports & adapters**, com o domínio isolado e as 70 regras testáveis sem banco.

## Observabilidade

API e navegador emitem OpenTelemetry para um coletor OTLP (SigNoz). Configuração inteira no
`.env`, com os nomes padrão do OTEL — veja `.env.example`. Deixar `OTEL_EXPORTER_OTLP_ENDPOINT`
vazio desliga tudo, e a aplicação sobe igual.

| Sinal | Origem | O que aparece |
| --- | --- | --- |
| Traces · API | ASP.NET Core, HttpClient, SqlClient, spans de negócio | request completo, incluindo a query e a regra (`regra.chave`) que recusou |
| Traces · migrações | um span por script DbUp (`mundial-migracoes`) | qual script demorou, e quanto o banco levou para responder na subida |
| Traces · navegador | carregamento, chamadas à API, cliques, troca de rota | trace único do clique até o SQL; navegação nomeada pelo padrão da rota |
| Métricas · API | ASP.NET Core, runtime, processo, negócio | latência, throughput, GC, memória residente, descritores abertos |
| Métricas · navegador | Web Vitals e erros | LCP, FCP, TTFB, INP, CLS e contagem de erro por origem |
| Logs | `ILogger` da API, erros do navegador, marcador de deploy | cada linha com `trace_id`, ligada ao trace |

As métricas de negócio (`Metricas.cs`) respondem o que o span responde caro: `mundial.leituras`
por desfecho, `mundial.regras.recusas` por chave de regra, `mundial.lancamentos`,
`mundial.finalizacoes` com e sem divergência, `mundial.conferencia.itens`. Rótulo só de conjunto
fechado — documento, produto e matrícula ficam de fora, porque em métrica cada valor distinto
vira uma série que não morre mais.

`/api/saude` fica fora de trace **e** de métrica: o healthcheck sonda a cada 10 s, e contá-lo
fazia da mediana da API a mediana da sonda.

Cada deploy emite um log OTLP como serviço `mundial-deploy`, com commit, situação e duração. É o
que permite responder "isso começou depois de qual versão?". A versão está em todo sinal, em
`service.version`: vem do commit, gravado na imagem pelo build (`--build-arg VERSAO`), não do
`.env` — variável nova no `.env` da máquina não chega sozinha.

O navegador posta em `/otlp` na própria origem, nunca no coletor: o ingest recusa preflight CORS
e o bearer não pode viver no JavaScript. Quem acrescenta o `Authorization` é o nginx do container
web (`web/entrypoint.sh`), no compose local e na máquina publicada. O bloco casa `/otlp/(.*)`,
então serve aos três sinais.

O texto do SQL e os parâmetros de query ficam fora dos spans de propósito — levariam número de
nota, código de fornecedor e matrícula para fora da máquina. Pelo mesmo motivo a sessão do
navegador (`session.id`) identifica a visita, nunca o operador.

## Como construir

Você precisa de um agente de codificação com o BMAD instalado neste repositório.

```bash
npx bmad-method install     # se ainda não estiver instalado
```

Depois, em ordem:

1. **`bmad-ux`** — produz o contrato de UX (design e experiência).
2. **`bmad-sprint-planning`** — verifica a prontidão e gera o acompanhamento de sprint.
3. **`bmad-build`** — implementa uma story por vez, na ordem dos épicos.

Rode cada skill em uma janela de contexto nova. Se estiver perdido, invoque **`bmad-help`**.

### Regras que valem para quem implementa

- O **spine de arquitetura** é vinculante. Uma story nunca contraria um `AD`.
- Toda regra de negócio implementada cita seu `RK-…`, e tem um teste que cita a mesma chave.
- Nomes de tabela e coluna vêm da fonte legada, sem renomear. A única exceção autorizada é de
  segurança (`senha` → `senha_hash`).
- A aplicação é toda em **Português do Brasil**, reaproveitando o texto literal das mensagens do
  legado.

## Estado atual

Planejamento concluído em 2026-08-10. Cobertura verificada por script: **54/54** requisitos
funcionais e **68/70** regras do UIR mapeadas em stories — as duas restantes são erro de conexão
ODBC do legado, sem equivalente no sistema novo.

Cinco perguntas seguem em aberto, registradas na seção 9 do PRD. A de maior peso é **Q-1**: se a
quantidade relançada soma ao acumulado ou substitui. Decidimos "substitui" com base na estrutura do
dado, mas sem confirmação de alguém que opere o legado.

---

_Legado analisado por RNC · planejamento pelo método BMAD._
