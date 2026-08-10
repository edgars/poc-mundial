# Review — Rubrica do bom spine

**Veredito:** APROVA COM CORREÇÕES. Estrutura e altitude corretas; três lacunas de cobertura.

| Critério | Resultado |
| --- | --- |
| Fixa os pontos reais de divergência do nível abaixo | Parcial → ver R-1 |
| Toda Rule é executável e previne a divergência declarada | Sim, exceto AD-3 (ver review adversarial A-1/A-2) |
| Nada em Deferred permite duas unidades divergirem | Sim |
| Tecnologia nomeada é verificada-atual | Falhava → corrigido (review de versões) |
| Ratifica em vez de contradizer a realidade existente | Sim — contradiz o pacote BMAD original **de propósito**, e o UIR do RNC é a autoridade citada |
| Cobre as capabilities do spec que o dirigiu | Sim — mapa Capability→Arquitetura tem 8 linhas |
| Nenhum AD enfraquece invariante herdado | N/A — sem spine pai |
| Toda dimensão da altitude decidida, adiada ou em aberto | Falhava → ver R-1, R-2 |

## Achados

### R-1 — ALTO — Dimensão de teste ausente por completo

Nem decidida, nem adiada, nem questão em aberto. Silêncio total. Em migração de legado, teste é o
que prova equivalência funcional — a promessa central do produto. **Corrigido com AD-20.**

### R-2 — MÉDIO — Envelope operacional cobre deploy mas não operação

Ambientes e docker-compose estão fixados. Faltam backup/restore e o que acontece quando a impressora
ou o SQL Server cai no meio de uma conferência. Para POC, adiar é legítimo — mas o silêncio não era.
**Corrigido:** duas linhas novas em Deferred, com condição de revisita.

### R-3 — BAIXO — `docs/ux/DESIGN.md` citado como *Binds* de AD-4

AD-4 vincula um documento que o próprio relatório de divergência declara errado (D-03). O AD manda
o construtor seguir uma fonte furada. **Corrigido:** *Binds* passa a apontar para `dataBindings` do
UIR e para os formulários, e a correção do DESIGN.md vira item de trabalho.

## Pontos fortes

- Cada AD carrega evidência do UIR, não opinião — `Prevents` cita a divergência concreta que já
  aconteceu no pacote original.
- AD-6 é a decisão de maior valor do spine: separa 16 das 70 "regras" que não são regra de servidor.
  Sem ela, o time implementaria diálogo de confirmação como validação de API.
- Deferred nomeia a razão de cada adiamento e a condição de revisita — não é lista de desejos.
