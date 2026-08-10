---
name: Mundial · Conferência
description: Identidade visual do sistema de conferência de recebimento do Supermercados Mundial — painel de operação em ardósia escura, para operador em pé com coletor na mão.
status: final
created: '2026-08-10'
updated: '2026-08-10'
sources:
  - '_bmad-output/planning-artifacts/prds/prd-poc-mundial-2026-08-10/prd.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-poc-mundial-2026-08-10/ARCHITECTURE-SPINE.md'
  - '_bmad-output/planning-artifacts/epics.md'
companions:
  - 'EXPERIENCE.md'

colors:
  surface-base: '#0F1417'
  surface-raised: '#182027'
  surface-overlay: '#1F2A32'
  border: '#2C3A44'
  border-strong: '#3E5262'
  text: '#E8EEF2'
  text-muted: '#8FA3B0'
  text-disabled: '#5B6D78'
  focus: '#3DD6D0'
  focus-glow: 'rgba(61,214,208,0.13)'
  accept: '#34D399'
  accept-wash: 'rgba(52,211,153,0.16)'
  attention: '#FBBF24'
  attention-wash: 'rgba(251,191,36,0.16)'
  reject: '#F87171'
  reject-wash: 'rgba(248,113,113,0.14)'

typography:
  display:
    fontFamily: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif
    fontSize: '29px'
    fontWeight: '600'
    lineHeight: '1.15'
    letterSpacing: '-0.02em'
  screen-title:
    fontFamily: '{typography.display.fontFamily}'
    fontSize: '23px'
    fontWeight: '600'
    lineHeight: '1.22'
    letterSpacing: '-0.02em'
  body:
    fontFamily: '{typography.display.fontFamily}'
    fontSize: '15px'
    fontWeight: '400'
    lineHeight: '1.5'
  label:
    fontFamily: '{typography.display.fontFamily}'
    fontSize: '10px'
    fontWeight: '500'
    lineHeight: '1.2'
    letterSpacing: '0.14em'
  code:
    fontFamily: ui-monospace, 'SF Mono', Menlo, Consolas, monospace
    fontSize: '29px'
    fontWeight: '400'
    letterSpacing: '0.05em'
  quantity:
    fontFamily: '{typography.code.fontFamily}'
    fontSize: '56px'
    fontWeight: '600'
    lineHeight: '1'
    letterSpacing: '-0.03em'
  data:
    fontFamily: '{typography.code.fontFamily}'
    fontSize: '14px'
    fontWeight: '400'

rounded:
  sm: '2px'
  DEFAULT: '8px'
  md: '10px'
  lg: '12px'
  full: '9999px'

spacing:
  '1': '4px'
  '2': '8px'
  '3': '12px'
  '4': '14px'
  '5': '16px'
  '6': '22px'
  '7': '32px'
  gutter: '{spacing.4}'
  card-padding: '{spacing.5}'
  focal-gap: '{spacing.6}'

motion:
  instant: '0ms'
  flash: '120ms'
  shift: '260ms'
  route: '340ms'
  celebrate: '620ms'
  ease-out: 'cubic-bezier(0.22, 1, 0.36, 1)'
  ease-shift: 'cubic-bezier(0.4, 0, 0.2, 1)'

components:
  campo-leitura:
    background: '{colors.surface-raised}'
    border: '1px solid {colors.focus}'
    boxShadow: '0 0 0 3px {colors.focus-glow}'
    radius: '{rounded.md}'
    padding: '{spacing.4} {spacing.5}'
    typography: '{typography.code}'
  painel-leitura-focal:
    background: '{colors.surface-raised}'
    border: '1px solid {colors.border}'
    radius: '{rounded.md}'
    padding: '{spacing.5}'
    width: '330px'
  selo-aceite:
    background: '{colors.accept-wash}'
    color: '{colors.accept}'
    border: '1px solid rgba(52,211,153,0.4)'
    radius: '{rounded.DEFAULT}'
    typography: '{typography.label}'
  selo-recusa:
    background: '{colors.reject-wash}'
    color: '{colors.reject}'
    border: '1px solid rgba(248,113,113,0.4)'
    radius: '{rounded.DEFAULT}'
    typography: '{typography.label}'
  pilula-estado:
    radius: '{rounded.full}'
    padding: '2px {spacing.2}'
    typography: '{typography.label}'
    fontWeight: '600'
  cartao-doca:
    background: '{colors.surface-raised}'
    border: '1px solid {colors.border}'
    radius: '{rounded.md}'
    padding: '15px'
    minHeight: '215px'
  cartao-doca-atrasada:
    border: '1px solid {colors.attention}'
    boxShadow: '0 0 0 3px {colors.attention-wash}'
  tabela-densa:
    headerBackground: '{colors.surface-overlay}'
    rowBorder: '1px solid {colors.border}'
    rowPadding: '11px {spacing.4}'
    typography: '{typography.body}'
  chip-contexto:
    background: '{colors.surface-raised}'
    border: '1px solid {colors.border}'
    radius: '{rounded.DEFAULT}'
    padding: '9px 13px'
---

# DESIGN.md — Mundial · Conferência

## Brand & Style

Isto é um **painel de operação**, não um aplicativo de escritório. O usuário está em pé, na doca, com
um coletor de código de barras numa das mãos e um caminhão esperando. A tela precisa ser lida de
relance, a um braço de distância, sob luz de galpão.

O registro é o do software de operação contemporâneo — a família visual de quem já usou um painel de
logística moderno. Ardósia escura como ground, cantos suaves o suficiente para não parecer terminal
dos anos noventa, e cor usada com parcimônia: só onde comunica estado ou marca o foco.

O que este produto **não** é: não é dashboard executivo, não tem métrica agregada, não tem ilustração.
Cada elemento na tela existe porque alguém precisa agir sobre ele.

A herança do legado aparece em um lugar só, e de propósito: **as mensagens são as do sistema antigo,
literais**. "Este Código já tem Qtde lançada" continua com essa exata redação. O operador reconhece o
sistema pela fala antes de reconhecer pelo visual.

## Colors

O ground é `{colors.surface-base}`, uma ardósia com viés azul-esverdeado. Três níveis de superfície
separam profundidade sem sombra: base, `{colors.surface-raised}` para cartão e painel, e
`{colors.surface-overlay}` para cabeçalho de tabela.

**`{colors.focus}` — ciano.** A cor mais restrita do sistema. Marca **onde está o foco do teclado** e
nada mais. Como a tela de conferência é operada inteiramente por teclado, o ciano é a resposta à
pergunta "onde eu estou". Nunca use ciano para estado, botão ou destaque decorativo — se aparecer em
dois lugares ao mesmo tempo, o operador perde a referência.

**`{colors.accept}` — verde.** Leitura aceita, item conferido sem divergência. Sempre acompanhado de
texto: o verde confirma, não informa sozinho.

**`{colors.attention}` — âmbar.** Divergência entre nota e recebido, doca que passou do tempo, item
aguardando. É a cor que faz o supervisor agir. **Âmbar não é erro** — divergência é dado legítimo, não
falha.

**`{colors.reject}` — vermelho.** Só recusa de leitura e conflito de gravação. Se o vermelho aparece,
alguma coisa não entrou no sistema. Escasso por construção — vermelho frequente vira ruído.

`{colors.text-muted}` carrega rótulo, metadado e histórico. `{colors.text-disabled}` marca item ainda
não conferido, que existe mas não aconteceu.

As variantes `-wash` são as mesmas cores a baixa opacidade, para fundo de pílula e faixa de linha.
Nunca use uma cor sólida de estado como fundo de área grande.

## Typography

Duas famílias, com divisão de trabalho estrita.

**Sans do sistema** para tudo que se lê como língua: título, corpo, rótulo, nome de produto. Sem
webfont — a fonte do sistema carrega instantaneamente, e num terminal de doca isso importa mais que
personalidade tipográfica.

**Monoespaçada** para tudo que se lê como dado: código de barras, quantidade, matrícula, número de
documento, horário. Além do alinhamento, o mono comunica "isto é um valor exato" — e nas colunas de
quantidade evita que 11 e 41 pareçam a mesma largura.

`{typography.code}` a 29px no campo de leitura: o operador precisa conferir o código bipado sem se
aproximar. `{typography.quantity}` a 56px no painel focal — é o número que decide se a carga entra
certa. `{typography.label}` é o único estilo em caixa alta, com `0.14em` de entrelinhamento, e serve
só a rótulo curto.

Nada de informação crítica abaixo de 14px. Se não couber, corte conteúdo — não reduza o tipo.

## Layout & Spacing

Escala em passos de 4px até `{spacing.5}`, depois saltos maiores. `{spacing.4}` é o gutter padrão
entre cartões e o padding interno de célula de tabela.

**Tela de conferência** — duas colunas: lista de itens à esquerda, ocupando o que sobra, e o painel
focal fixo em 330px à direita. A largura fixa é intencional: o painel focal **não pode mudar de
posição nem de tamanho** entre uma leitura e outra, porque o operador aprende onde olhar.

**Painel de docas** — grade de quatro colunas com `{spacing.3}` de gap, cartões de altura mínima
uniforme. Abaixo de 1100px a grade vira duas colunas; abaixo de 700px, uma. O painel é de parede e de
mesa, não de celular.

Contexto (documento, fornecedor, contagem) vive em chips no topo, nunca em barra lateral — barra
lateral rouba largura da lista, que é onde o olho trabalha.

## Elevation & Depth

**Sem sombra projetada.** A profundidade vem de camadas tonais e de borda: base → raised → overlay.
Sombra sob luz de galpão vira borrão e custa compositor.

A única exceção é o **halo de foco**: `0 0 0 3px {colors.focus-glow}` no campo de leitura, e o
equivalente âmbar no cartão de doca atrasada. Halo não é decoração — é a única pista de "aqui" numa
tela sem cursor de mouse.

## Shapes

`{rounded.DEFAULT}` em pílula, selo e chip. `{rounded.md}` em cartão, painel e campo. `{rounded.full}`
só na pílula de estado, onde a forma de cápsula é o próprio sinal de "isto é um estado, não um botão".

Nada de canto vivo, nada de canto muito arredondado. O arredondamento moderado é o que separa este
produto do terminal legado sem empurrá-lo para o registro de aplicativo de consumo.

## Components

### `campo-leitura`
O elemento mais importante da interface. Fundo `{colors.surface-raised}`, borda ciano de 1px e halo de
3px. Tipografia `{typography.code}`. Cursor em bloco de 12×26px na cor de foco.

Mantém o foco permanentemente. Quando perde — clique acidental, retorno de diálogo — o halo pisca uma
vez em `{motion.flash}` ao recuperá-lo. Nunca fica sem halo enquanto a tela está ativa.

### `painel-leitura-focal`
Coluna fixa de 330px. Ordem vertical imutável: **selo de resultado** no topo, descrição do produto,
embalagem e DUN-14, quantidade em `{typography.quantity}`, e histórico dos lançamentos anteriores no
rodapé.

Essa ordem não muda entre aceite e recusa — só o selo troca de cor e o miolo troca de conteúdo. É a
regra central da tela: **o operador olha sempre para o mesmo lugar**.

### `selo-aceite` / `selo-recusa`
Faixa de largura total no topo do painel focal, caixa alta, `{typography.label}`. Fundo em `-wash`,
texto e borda na cor sólida. Nunca aparecem os dois; nunca aparecem fora do painel focal.

### `pilula-estado`
Cápsula `{rounded.full}` com `{typography.label}` em peso 600. Codifica estado de doca (em conferência,
aguardando, atrasada, livre) e resultado de item (ok, divergência, aguarda). Cor **e** texto sempre —
nunca cor sozinha, porque a distinção verde/âmbar não sobrevive a daltonismo nem a monitor ruim.

### `cartao-doca`
Cartão de 215px de altura mínima. Cabeçalho com número da doca e pílula de estado, documento em
destaque, fornecedor e operador em `{colors.text-muted}`, e no rodapé a barra de progresso com contagem
de itens e tempo de doca aberta.

A variante `cartao-doca-atrasada` acrescenta borda âmbar e halo. É o único cartão que muda de borda.

### `tabela-densa`
Cabeçalho em `{colors.surface-overlay}` com `{typography.label}`. Linhas separadas por borda, não por
faixa alternada. Colunas numéricas alinhadas à direita em `{typography.data}` com `tabular-nums`.

Linha com divergência recebe fundo `{colors.attention-wash}` a 5% — perceptível na varredura, não
gritante.

### `chip-contexto`
Bloco de contexto no topo da tela de conferência. Rótulo em `{typography.label}` e valor em peso 600.
Três por linha, largura igual.

## Do's and Don'ts

**Faça**

- Mantenha o painel focal na mesma posição e largura, sempre.
- Use ciano exclusivamente para foco de teclado.
- Acompanhe toda cor de estado de um texto que diga a mesma coisa.
- Alinhe número à direita e use `tabular-nums` em qualquer coluna de valor.
- Escreva as mensagens com o texto literal do legado, acentuação inclusive.

**Não faça**

- Não use sombra projetada. Profundidade é tonal.
- Não anime dígito de quantidade. O número troca instantâneo; o contêiner pode pulsar.
- Não coloque medidor de ocupação, gauge ou percentual agregado no painel de docas.
- Não use ilustração, gradiente ou ícone decorativo.
- Não reduza tipo abaixo de 14px para fazer conteúdo caber.
- Não mova o painel focal, nem mude sua largura, entre estados.
- Não use vermelho para divergência — divergência é âmbar, e é dado, não erro.
