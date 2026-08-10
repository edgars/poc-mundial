# PRD Quality Review — Mundial · Conferência de Recebimento

## Overall verdict

**Adequado para seguir para épicos e stories, com três correções aplicadas e três bloqueadores de
fase que precisam de resposta da Mundial antes de codar as áreas afetadas.**

O PRD carrega uma tese real — *preservar o processo, trocar a base* — e a rastreabilidade
`RK-…` dá a ele algo que a maioria dos PRDs não tem: cobertura verificável por script (70/70).
O ponto fraco é done-ness em duas features, e uma métrica que mede atividade em vez da tese.

## Decision-readiness — **adequado**

### Findings

**Forte.** A seção 11 nomeia um conflito com a arquitetura em vez de esconder — o alvo mudou para
piloto e três Deferred do spine acordaram. Um PRD medíocre teria absorvido isso em silêncio.

**Forte.** FR-4 declara que rompe a fidelidade ao legado de propósito e diz por quê. Trade-off com
o custo nomeado: todo usuário migrado precisa de reset.

**Fraco — corrigido.** Q-1 (soma vs substitui) estava listada como pergunta aberta, mas FR-18 já
decidia por soma sem marcar a decisão como provisória. Pergunta retórica com resposta na frente.
→ FR-18 agora aponta explicitamente para Q-1 como pendência.

## Substance over theater — **passa**

Sem persona theater: dois papéis, ambos vindos de `usuario.niv_usu`, ambos aparecendo em FRs.
Sem innovation theater — o documento não alega novidade nenhuma, o que é honesto para uma
modernização.

**NFR theater: ausente, e isso é notável.** NFR-1 diz 500 ms com a razão fisiológica (acima disso o
operador bipa de novo e duplica lançamento). NFR-2 diz "sem mouse" porque o legado é assim. São
limites específicos deste produto, não boilerplate.

A Vision não trocaria de PRD — fala de doca, coletor, FoxPro de 1994 e da pergunta que a supervisora
não consegue responder hoje.

## Strategic coherence — **adequado**

A tese existe e está dita: equivalência funcional primeiro, melhoria depois. A seção 5 (Não-objetivos)
sustenta a tese com sete linhas de "não faremos", cada uma com razão.

**Achado — corrigido.** A métrica "Tempo de conferência ≤ o tempo atual" mede atividade, não a tese.
A tese é equivalência; a métrica que a valida é contagem idêntica na mesma carga — que estava
escondida como contra-métrica. → Promovida a métrica primária.

Contra-métricas presentes e afiadas ("um único caso é falha").

## Done-ness clarity — **fino em duas features**

Aqui sou implacável, porque é o que as stories vão puxar.

**FR-13** ("resolve o DUN-14 correspondente e exibe descrição e embalagem") — não diz o que
acontece quando o EAN mapeia para **mais de um** DUN-14. Os três campos `barr_emb`, `barr_emb2` e
`barr_emb3` tornam isso possível por construção. Consequência não testável.
→ **Corrigido:** FR-13 agora define resultado único e ambiguidade como erro.

**FR-41** ("toda operação … gera registro de auditoria") — "toda operação" não é enumerável, e o
schema é inventado (A-1). Um engenheiro não sabe o que gravar.
→ **Corrigido:** FR-41 lista os campos.

**FR-38** ("byte a byte compatível") — excelente, é dos critérios mais testáveis do documento.

**NFR-4** ("não perde lançamento já confirmado") — a palavra "confirmado" carrega o critério e está
definida em FR-18. Passa.

Nenhuma ocorrência de "graciosamente", "razoável" ou "amigável". Zero adjetivos no lugar de limite.

## Scope honesty — **forte**

Seis suposições indexadas com impacto declarado, e A-4 admite ser a mais séria — a mudança de
desktop com dado local para cliente-servidor. Um PRD complacente teria omitido essa.

Oito perguntas abertas, três marcadas como bloqueadoras de fase. Densidade adequada para o estágio:
o documento não finge saber o que não sabe.

A nota sobre as duas regras ODBC sem FR fecha a contabilidade — sem ela, um revisor contaria 68 e
suporia esquecimento.

## Downstream usability — **forte**

- Glossário presente; `DUN-14`, `EAN-13`, `doca`, `documento`, `matrícula` usados de forma idêntica em todas as FRs e jornadas.
- FR-1 a FR-42 contíguos e únicos. NFR-1 a NFR-13 idem. UJ-1 a UJ-3.
- Cobertura de regra verificável por script: **70/70**, nenhuma chave inventada.
- O termo `acesso` é armadilha herdada — é tabela de permissão **e** coluna que guarda o documento fiscal. O Glossário desarma explicitamente.

## Shape fit — **adequado**

Tamanho compatível com piloto interno. A seção 8 (NFR) só existe por causa do alvo — se fosse POC,
seria excesso. As seções da espinha essencial estão todas presentes; do Adapt-In Menu entraram os
clusters de operação/hardware (impressora, coletor) e integração.

**Ausente e justificado:** não há seção de monetização, compliance ou governança de dado — nada no
UIR ou no contexto os levanta.

## Mechanical notes

- Frontmatter completo.
- Todas as referências cruzadas a ADs resolvem contra o spine (AD-7, AD-8, AD-10, AD-14, AD-15, AD-17, AD-19, AD-20).
- Português consistente; mensagens de erro reproduzem o texto legado literal, acentuação inclusive.
