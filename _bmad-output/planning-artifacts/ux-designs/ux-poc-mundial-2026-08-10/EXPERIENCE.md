---
name: Mundial · Conferência
description: Contrato de experiência — arquitetura de informação, comportamento, estados, interação e acessibilidade do sistema de conferência de recebimento.
status: final
created: '2026-08-10'
updated: '2026-08-10'
sources:
  - '_bmad-output/planning-artifacts/prds/prd-poc-mundial-2026-08-10/prd.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-poc-mundial-2026-08-10/ARCHITECTURE-SPINE.md'
  - '_bmad-output/planning-artifacts/epics.md'
companions:
  - 'DESIGN.md'
---

# EXPERIENCE.md — Mundial · Conferência

## Foundation

**Form-factor: web em desktop e terminal de doca.** Navegador em tela fixa no armazém, e navegador
em desktop para o supervisor. Não há aplicativo nativo nem layout de celular — o coletor de código de
barras é um periférico de teclado, não um dispositivo de tela.

**Sem UI system de terceiros.** Angular 22 com componentes standalone e signals (`AD-13`). Os
componentes nomeados aqui são próprios, e seus tokens visuais vivem em `DESIGN.md`.

**Modalidade de entrada dominante: teclado.** O coletor de código de barras digita uma sequência e
emite Enter — para a aplicação, é indistinguível de alguém digitando muito rápido. Toda a tela de
conferência precisa funcionar sem mouse. O mouse existe para o supervisor, não para a doca.

**Idioma único: Português do Brasil.** Não há i18n. As mensagens de erro reaproveitam o texto literal
do sistema legado.

## Information Architecture

Cinco superfícies. Cada uma nasce de uma necessidade declarada, e cada necessidade tem uma superfície.

| Superfície | Rota | Quem chega | Necessidade que atende |
| --- | --- | --- | --- |
| **Entrada** | `/entrar` | operador e supervisor | ser reconhecido pela matrícula |
| **Painel de docas** | `/docas` | ambos | saber o que está acontecendo e onde agir |
| **Conferência** | `/conferencia/:documento` | operador | registrar o que chegou |
| **Códigos de embalagem** | `/codigos` | supervisor | cadastrar e corrigir DUN-14, ver a etiqueta |
| **Consultas** | `/consultas` | supervisor | conferências anteriores, fornecedor, auditoria |

**O painel de docas é a superfície-âncora.** Depois de entrar, todo mundo cai nele. Ele responde
"onde eu preciso estar" e é o único caminho normal para a conferência — chegar direto pela URL
funciona, mas não é o fluxo desenhado.

A conferência **não tem tela de criação**. Documentos chegam da integração da nota fiscal, antes de o
caminhão encostar (`AD-14`). Não existe botão "nova conferência", e a ausência é deliberada.

## Voice and Tone

**Direto, sem eufemismo, sem desculpa.** O operador tem caminhão esperando.

**As mensagens do legado são citadas literalmente.** "Este Documento já foi conferido!", "Código Não
cadastrado!", "Tem certeza que deseja excluir este código?" — com a acentuação e a pontuação
originais, inclusive onde estão irregulares. Isso não é preguiça: é o que faz o operador reconhecer o
sistema. Mensagem nova segue a mesma cadência: curta, afirmativa, sem "por favor".

**Erro diz o que aconteceu e o que fazer.** "Código não cadastrado" sozinho é metade da informação —
o painel focal completa com "Item marcado como pendente", que é a consequência.

**Nunca culpe o usuário.** "Código não cadastrado", não "você bipou um código inválido".

**Número é sempre explícito.** "Este Código já tem Qtde lançada (12)" — o valor entra na frase, porque
é sobre ele que a pessoa decide.

## Component Patterns

### `CampoLeitura`
Recebe código e Enter. **Mantém o foco perpetuamente**: ao montar, após cada leitura, ao fechar
diálogo, ao voltar de outra aba. Se perder o foco por clique fora, recupera assim que qualquer tecla
imprimível for pressionada — o operador não deve precisar clicar de volta.

Limpa-se sozinho após cada leitura processada. Nunca acumula texto de duas leituras.

Aceita colagem, para o `PainelCodigos` do modo demonstração poder injetar código programaticamente
(`FR-52`).

### `PainelLeituraFocal`
Ponto único de verdade sobre a última leitura. Estrutura fixa em qualquer estado: selo, descrição,
embalagem, quantidade, histórico.

**Nunca muda de posição, largura ou ordem interna.** É a regra de comportamento mais importante do
sistema: a literatura de terminais de ponto de venda mostra que espalhar a confirmação faz o operador
parar de bipar para procurar o resultado.

Mantém os dois últimos lançamentos no rodapé, como memória curta.

### `SinalSonoro`
Dois sons curtos e distintos: aceite e recusa. Distintos em altura, não só em duração — o operador
diferencia sem olhar.

Toca **junto** com a atualização visual, nunca antes. Silenciável, e a preferência persiste por
usuário. Silenciado, a recusa ganha um pulso adicional no selo para compensar.

### `DialogoConfirmacao`
Um único componente serve as nove confirmações herdadas do legado. Título com o texto literal, duas
ações, e **foco inicial no botão seguro** — que é o que cancela, exceto em "Finalizar conferência?",
onde a ação esperada é confirmar.

Fecha com `Esc` (equivale a recusar) e confirma com `Enter`. Ao fechar, devolve o foco ao
`CampoLeitura`.

Não bloqueia no servidor: confirmação é decisão do operador, e a regra correspondente nunca vira
validação de API (`AD-6`).

### `PilulaEstado`
Estado em cor **e** texto. Sem ícone. Usada em cartão de doca e em linha de item.

### `TabelaDensa`
Lista do supervisor. Navegação por teclado com setas, `Enter` abre o registro, e o filtro recebe foco
com `/`. Ordenação por clique no cabeçalho. Sem paginação visual pesada: rolagem com carga por
demanda, respeitando o contrato `?pagina&tamanho&busca&ordem` (`AD-15`).

Separação por borda, nunca faixa alternada. A linha com foco de teclado recebe **filete ciano de 2px
à esquerda**, não fundo colorido — fundo brigaria com o âmbar da linha divergente, e as duas
informações precisam coexistir na mesma linha.

### `CartaoDoca`
Clicável inteiro, não só um link interno. Alvo grande porque também é operado em tela de parede com
toque.

### `PreviaEtiqueta`
Renderiza o ZPL gerado como imagem, com o texto ZPL disponível em painel colapsado para inspeção.
Fica **lado a lado** com o formulário de códigos, não em modal — o supervisor corrige o código e vê a
etiqueta mudar sem trocar de contexto.

### Erro de formulário
O erro aparece **no campo que o causou**, abaixo dele, e nunca num resumo no topo. As regras de
código de barras são de duplicidade cruzada entre três campos: saber *qual* dos três conflita é a
informação, e um resumo a perderia.

### `PainelCodigos` (andaime de demonstração)
Lista os códigos semeados com o efeito de cada um. Vive numa gaveta lateral colapsável, **fora do
centro da tela**, para não competir com o painel focal. Só existe com `MODO_DEMO=true`.

## State Patterns

### Estados de leitura
| Estado | O que aparece | Som |
| --- | --- | --- |
| **Ocioso** | painel focal mostra a última leitura ou vazio instrutivo | — |
| **Aceito** | selo verde, descrição, embalagem, quantidade | aceite |
| **Recusado** | selo vermelho, motivo, consequência | recusa |
| **Ambíguo** | selo vermelho, lista dos DUN-14 candidatos, escolha por teclado | recusa |
| **Confirmação pendente** | diálogo sobreposto; painel focal congela no estado anterior | — |

**Nunca existe estado de carregamento visível na leitura.** Se a resposta demorar mais que 500 ms, o
requisito foi violado — a resposta é corrigir o desempenho, não exibir um spinner.

### Estado vazio
| Superfície | Vazio |
| --- | --- |
| Painel de docas | "Nenhuma doca em operação" + última conferência fechada |
| Conferência | "Bipe o primeiro item" no painel focal |
| Consultas | "Nenhuma conferência no período" + atalho para ampliar o período |
| Códigos | "Informe o código do produto" |

### Estado de conferência
`aguardando` → `em conferência` → `fechada`. A transição para `fechada` é irreversível e atinge todas
as linhas do documento de uma vez (`AD-10`). Documento fechado renderiza em modo somente leitura, sem
ações desabilitadas espalhadas — a tela inteira muda de modo, e diz isso no topo.

### Conflito de gravação
Dois operadores no mesmo documento: o segundo recebe `409` e vê um comparativo do que mudou, com uma
ação única — recarregar e refazer o lançamento. Nunca há opção de "forçar" (`AD-17`).

### Sessão expirada
Volta para `/entrar` com a razão dita ("Sessão encerrada por inatividade"). Ao reentrar, retorna à
mesma conferência. Nenhum lançamento já gravado se perde (`FR-54`).

## Interaction Primitives

### Teclado — tela de conferência
| Tecla | Ação |
| --- | --- |
| qualquer caractere | vai para o `CampoLeitura`, mesmo sem foco |
| `Enter` | processa a leitura |
| `Esc` | limpa o campo; no diálogo, recusa |
| `Tab` | percorre a lista de itens; nunca sai do `CampoLeitura` por acidente |
| `F2` | finaliza a conferência (abre a confirmação) |
| `↑` `↓` | navega a lista de itens quando ela tem foco |

### Movimento
Cinco momentos orquestrados, e uma regra que os governa: **movimento nunca precede informação**.

| Momento | Comportamento | Duração |
| --- | --- | --- |
| **Flash de leitura** | selo e borda do painel focal pulsam na cor do resultado | `{motion.flash}` |
| **Entrada de item na lista** | linha desliza para a posição, opacidade de 0 a 1 | `{motion.shift}` |
| **Reordenação do painel de docas** | cartões deslocam-se continuamente à nova posição quando o tempo muda a ordem | `{motion.shift}` |
| **Transição doca → conferência** | o cartão da doca torna-se o cabeçalho da tela | `{motion.route}` |
| **Fechamento do documento** | sequência de conclusão — o único momento que comporta algo mais elaborado, por ocorrer uma vez por conferência | `{motion.celebrate}` |
| **Reveal da etiqueta** | a prévia surge de cima para baixo, como impressão saindo | `{motion.route}` |

Regras duras:

- Informação legível em **≤100 ms**; a animação roda em paralelo e é **interrompida** pela leitura seguinte.
- **Dígito de quantidade nunca interpola.** Um contador rolando de 12 para 18 faz o operador ler errado no meio do caminho. O contêiner pulsa; o número troca instantâneo.
- Apenas `transform` e `opacity`.
- `prefers-reduced-motion: reduce` desliga deslocamento e sequência de fechamento; mantém a troca de cor do flash, que é informação.

### Foco
Foco visível sempre, com o halo ciano. Ao fechar qualquer sobreposição, o foco retorna ao elemento que
a abriu — na conferência, sempre o `CampoLeitura`.

## Accessibility Floor

- **Operável só por teclado**, ponta a ponta, na tela de conferência. Não é acessibilidade opcional: é o modo primário de uso.
- Estado nunca comunicado só por cor. Toda `PilulaEstado` traz texto.
- Contraste mínimo de **7:1** para texto de corpo, acima do AA — a leitura acontece a um braço de distância sob luz de galpão.
- Nenhum texto crítico abaixo de 14px.
- Resultado de leitura anunciado em região `aria-live="assertive"`; mudança de lista em `aria-live="polite"`.
- Diálogo com `role="dialog"`, `aria-modal`, foco preso enquanto aberto, retorno de foco garantido.
- `prefers-reduced-motion` respeitado.
- Som é sempre redundante — nunca a única forma de saber o resultado.

## Key Flows

### KF-1 · Cleber confere uma carga de bebida

1. Cleber entra com matrícula `04127`. Cai no painel de docas.
2. A doca 1 mostra `000148372/1` · Bebidas Primavera, aguardando. Ele clica; o cartão vira o cabeçalho da tela de conferência.
3. O `CampoLeitura` já está com foco. Ele bipa `7891234567897`.
4. Em menos de meio segundo: selo verde, "Refrigerante Cola 2L", "CX c/ 6", som de aceite. Ele digita `40`, Enter.
5. A linha entra na lista com a pílula `ok`. Foco volta ao campo sozinho.
6. Bipa o próximo. Cerveja Pilsen: a nota diz 120, ele conta 114. Lança 114 — a linha recebe pílula âmbar `−6`, e nada reclama. **Divergência é dado.**
7. Terceira leitura: o sistema avisa que o código já tem 12 lançadas e pergunta se quer substituir. Ele conferiu de novo: confirma 18.
8. **Climax** — `F2`. "Finalizar conferência?" Ele confirma. A sequência de fechamento roda, o documento passa a somente leitura, e o cabeçalho registra quem fechou e quando. Acabou: 22 minutos, sem tirar a mão do coletor.

### KF-2 · Cleber bate num código que não existe

1. Quarta leitura. Som de recusa, selo vermelho, "Código Não cadastrado!".
2. Logo abaixo, a consequência: "Item marcado como pendente".
3. **Climax** — Cleber tem permissão de inclusão, então aparece "Deseja Cadastrar agora?". Ele aceita, chega ao cadastro de códigos com o valor já preenchido, completa e volta.
4. Bipa de novo. Agora resolve. O caminhão não parou.

*(Sem permissão de inclusão, o passo 3 não existe: o item fica pendente e ele segue com o resto da carga. É o mesmo desfecho do legado, sem a ligação para a supervisora.)*

### KF-3 · Rosana revisa o turno

1. Sete da manhã. Rosana entra e cai no painel de docas.
2. A doca 2 está no topo — não porque é a segunda, mas porque está aberta há 3h41. Anel âmbar, pílula `atrasada`.
3. Ela vê que Marcos lançou 7 de 20 itens. Abre com um clique.
4. Vai para consultas e filtra o turno pelo teclado. A tabela densa mostra o que fechou, quem fechou, e onde houve divergência.
5. **Climax** — abre a conferência de ontem à noite. Tudo somente leitura, e a trilha diz quem alterou o quê. É a primeira vez que ela consegue responder isso sem perguntar a ninguém.

## Inspiração e anti-padrões

**Inspiração.** Flexport Dashboard 2.0 — a equipe deles publicou que o supervisor não revisa tudo, só
subconjuntos, e o filtro que importa é urgência; daí o painel ordenar por tempo de doca aberta e não
por número. Literatura de terminais de ponto de venda — feedback imediato, visual e sonoro, num único
local; daí o `PainelLeituraFocal`.

**Anti-padrões, com o motivo.**

| Não fazer | Por quê |
| --- | --- |
| Percentual de ocupação no painel | com 4 docas, "75%" esconde *qual* e *há quanto tempo* — e não gera ação |
| Spinner na leitura de código | acima de 500 ms o requisito já foi violado; spinner mascara o problema |
| Contador animado de quantidade | o operador lê o número no meio da interpolação e registra errado |
| Confirmação em toast | as nove confirmações herdadas exigem decisão; toast pode ser perdido |
| Vermelho para divergência | divergência é dado legítimo, não falha |
| Desabilitar ações num documento fechado | a tela inteira muda de modo e diz isso; ação cinza espalhada confunde |
