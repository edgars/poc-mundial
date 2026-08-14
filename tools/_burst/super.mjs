// Super rajada: mais de mil traces atravessando todas as camadas — navegador (Angular/OTel web),
// proxy, API (.NET), domínio e banco (spans do SqlClient em cada chamada). Herda os seletores,
// as personas e a massa semeada do pesada.mjs; o que muda é volume, variedade e contagem.
//
// Cada requisição HTTP à API vira um trace do lado servidor. As que nascem no navegador levam
// traceparent e se juntam ao trace da página — por isso as duas camadas são contadas separado.
// Script descartável, fora do repositório. Reseta a massa antes e depois: não deixa rastro na
// demonstração.
import { chromium } from 'playwright';

const WEB = 'https://poc-mundial.exai.extreme.digital';
const API = `${WEB}/api`;
const espera = (ms) => new Promise((r) => setTimeout(r, ms));

// ------------------------------------------------------------------ contadores
const conta = { api: 0, browserNav: 0, browserInterac: 0, sessoes: 0, erros: 0 };
const porStatus = new Map();

async function chamar(caminho, opcoes = {}) {
  conta.api++;
  try {
    const r = await fetch(`${API}${caminho}`, { ...opcoes, signal: AbortSignal.timeout(20000) });
    porStatus.set(r.status, (porStatus.get(r.status) ?? 0) + 1);
    return r;
  } catch (e) {
    conta.erros++;
    porStatus.set('falha-rede', (porStatus.get('falha-rede') ?? 0) + 1);
    return null;
  }
}

// Limitador de concorrência: a máquina tem 4 vCPU e o SQL Server divide com a API.
// Rajada não pode virar negação de serviço da própria POC.
async function emLotes(tarefas, tamanho) {
  const saida = [];
  for (let i = 0; i < tarefas.length; i += tamanho) {
    saida.push(...(await Promise.all(tarefas.slice(i, i + tamanho).map((t) => t()))));
  }
  return saida;
}

async function token(matricula, senha = 'mundial') {
  const r = await chamar('/entrar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ matricula, senha }),
  });
  if (!r || !r.ok) return null;
  return (await r.json()).token;
}
const auth = (t) => ({ Authorization: `Bearer ${t}` });
const jsonAuth = (t) => ({ ...auth(t), 'Content-Type': 'application/json' });

const inicio = Date.now();
console.log('== 0. reset pré-rajada ==');
let tRosana = await token('04310');
{
  const r = await chamar('/demo/reset', { method: 'POST', headers: auth(tRosana) });
  console.log('  ', r?.status, await r?.text());
}

// ------------------------------------------------------- 1. camada navegador
console.log('== 1. camada navegador: sessões reais em Chromium ==');
const navegador = await chromium.launch();

const codigosDemo = [
  '7891234567897', '7891234500013', '7891234511019', '7891234522015',
  '7899876500019', '7894455000012', '7890000111222', '7899999000123',
];
const docas = ['000148372/1', '000147901/1', '000148415/2', '000147744/1'];
const personas = ['04127', '04310', '04982'];

async function ir(page, url) {
  conta.browserNav++;
  await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 }).catch(() => {});
}
async function entrar(page, matricula, senha = 'mundial') {
  await ir(page, `${WEB}/entrar`);
  await page.fill('#mat', matricula).catch(() => {});
  await page.fill('#sen', senha).catch(() => {});
  conta.browserInterac++;
  await page.click('button[type=submit]').catch(() => {});
  await espera(600);
}
async function bipar(page, codigo) {
  conta.browserInterac++;
  await page.fill('#leitura', codigo).catch(() => {});
  await page.press('#leitura', 'Enter').catch(() => {});
  await espera(450);
}

// Sessão de trabalho: login, ronda pelas docas, bipagens, consultas. Cada navegação é um
// trace de carregamento de rota; cada bipagem, um trace de clique com a chamada de API dentro.
async function sessaoTrabalho(persona, doc, bipagens) {
  const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
  const page = await ctx.newPage();
  conta.sessoes++;
  try {
    await entrar(page, persona);
    await page.waitForURL('**/docas', { timeout: 15000 }).catch(() => {});
    await espera(400);
    await ir(page, `${WEB}/conferencia/${encodeURIComponent(doc)}`);
    for (let i = 0; i < bipagens; i++) {
      await bipar(page, codigosDemo[(i + persona.length) % codigosDemo.length]);
    }
    await ir(page, `${WEB}/docas`);
    await ir(page, `${WEB}/consultas`);
    conta.browserInterac++;
    await page.click('button:has-text("Fornecedores")').catch(() => {});
    await espera(400);
    conta.browserInterac++;
    await page.click('button:has-text("Auditoria")').catch(() => {});
    await espera(400);
    await ir(page, `${WEB}/codigos?codigo=${persona}`);
    await ir(page, `${WEB}/docas`);
  } finally {
    await ctx.close();
  }
}

// Sessão de recusa: motivos diferentes de 401, para o SigNoz ter trace de erro de verdade.
async function sessaoLoginsRuins() {
  const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
  const page = await ctx.newPage();
  conta.sessoes++;
  try {
    await entrar(page, '99999');
    await entrar(page, '04127', 'errada');
    await entrar(page, '05001');
    await entrar(page, '04982', 'tambem-errada');
  } finally {
    await ctx.close();
  }
}

const sessoes = [];
for (let i = 0; i < 18; i++) {
  const persona = personas[i % personas.length];
  const doc = docas[i % docas.length];
  sessoes.push(() => sessaoTrabalho(persona, doc, 5));
}
sessoes.push(() => sessaoLoginsRuins());
sessoes.push(() => sessaoLoginsRuins());
// Quatro navegadores por vez: mais que isso e a máquina de 4 vCPU começa a competir consigo mesma.
await emLotes(sessoes, 4);
await navegador.close();
console.log(`   ${conta.sessoes} sessões · ${conta.browserNav} navegações · ${conta.browserInterac} interações`);

// ------------------------------------------------------ 2. camada API/banco
console.log('== 2. camada API/banco: ondas de leitura ==');
const tCleber = await token('04127');
const tMarcos = await token('04982');
tRosana = await token('04310');

const leituras = [
  ['/docas', tCleber], ['/docas', tRosana], ['/docas', tMarcos],
  ['/conferencias?pagina=0&tamanho=10', tRosana],
  ['/conferencias?pagina=1&tamanho=5', tRosana],
  ['/conferencias?pagina=0&tamanho=25', tRosana],
  ['/conferencias?busca=PRIMAVERA', tRosana],
  ['/conferencias?busca=LATICINIOS', tRosana],
  ['/conferencias?busca=HIGIENE', tRosana],
  ['/fornecedores', tRosana], ['/fornecedores?busca=HIGIENE', tRosana],
  ['/fornecedores?busca=PRIMAVERA', tRosana],
  ['/auditoria', tRosana],
  ['/produtos/04127', tCleber], ['/produtos/04982', tCleber], ['/produtos/05310', tCleber],
  ['/produtos/05877', tCleber], ['/produtos/06120', tCleber], ['/produtos/06430', tCleber],
  ['/produtos/07001', tCleber],
  ['/produtos/04127/etiqueta', tCleber],
  ['/produtos/04127/etiqueta?codigoBarras=7891234567897', tCleber],
  ['/produtos/05877/etiqueta', tCleber],
  ['/saude', null],
  ['/demo/codigos', null],
];

const ONDAS = 26;
for (let onda = 0; onda < ONDAS; onda++) {
  await emLotes(
    leituras.map(([caminho, tok]) => () => chamar(caminho, { headers: tok ? auth(tok) : {} })),
    10,
  );
  if ((onda + 1) % 5 === 0) console.log(`   onda ${onda + 1}/${ONDAS} · ${conta.api} chamadas até aqui`);
}

// ------------------------------------------------- 3. escrita: domínio e banco
console.log('== 3. camada de domínio: lançamentos, estornos, fechamentos ==');
// Lançar exige a versão corrente do item (concorrência otimista), então cada escrita
// custa um GET antes — que é justamente o par leitura/escrita que se quer ver no trace.
async function lancar(doc, tok, matricula, quantidade, giro = 0) {
  const r = await chamar(`/conferencia?documento=${encodeURIComponent(doc)}`, { headers: auth(tok) });
  if (!r || !r.ok) return;
  const corpo = await r.json();
  const itens = corpo.itens ?? [];
  if (itens.length === 0) return;
  // Prefere o item ainda pendente (é o lançamento que o operador faria); na falta dele, gira
  // pelos demais, para as escritas não baterem sempre na mesma linha do banco.
  const item = itens.find((i) => i.pendencia === true) ?? itens[giro % itens.length];
  await chamar(`/conferencia/lancamentos?documento=${encodeURIComponent(doc)}`, {
    method: 'POST',
    headers: jsonAuth(tok),
    body: JSON.stringify({ codigo: item.codigo, quantidade, matricula, confirmado: true, versao: item.versao }),
  });
  return item.codigo;
}

for (let rodada = 0; rodada < 12; rodada++) {
  for (const [doc, tok, mat] of [
    ['000148372/1', tCleber, '04127'],
    ['000147901/1', tCleber, '04127'],
    ['000148415/2', tRosana, '04310'],
  ]) {
    const codigo = await lancar(doc, tok, mat, 10 + rodada, rodada);
    if (codigo) {
      await chamar(
        `/conferencia/lancamentos?documento=${encodeURIComponent(doc)}&codigo=${codigo}&confirmado=true`,
        { method: 'DELETE', headers: auth(tok) },
      );
    }
  }
  if ((rodada + 1) % 4 === 0) console.log(`   rodada ${rodada + 1}/12 · ${conta.api} chamadas até aqui`);
}

// ------------------------------------------------------- 4. casos de borda
console.log('== 4. casos de borda: 401, 403, 404, 409, 422 ==');
await emLotes(Array.from({ length: 12 }, () => () => chamar('/docas')), 6);            // 401
await emLotes(Array.from({ length: 8 }, () => () => chamar('/auditoria', { headers: auth(tCleber) })), 4); // 403
await emLotes(Array.from({ length: 6 }, () => () => chamar('/produtos/99999999', { headers: auth(tCleber) })), 3); // inexistente
await emLotes(Array.from({ length: 6 }, () => () => chamar('/conferencia?documento=000000000%2F9', { headers: auth(tCleber) })), 3);

// 409 de verdade: duas gravações concorrentes na mesma versão do mesmo item.
for (let i = 0; i < 6; i++) {
  const r = await chamar(`/conferencia?documento=${encodeURIComponent('000148372/1')}`, { headers: auth(tCleber) });
  if (!r || !r.ok) break;
  const corpo = await r.json();
  const item = (corpo.itens ?? [])[0];
  if (!item) break;
  const body = JSON.stringify({ codigo: item.codigo, quantidade: 24, matricula: '04127', confirmado: true, versao: item.versao });
  await Promise.all([
    chamar(`/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}`, { method: 'POST', headers: jsonAuth(tCleber), body }),
    chamar(`/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}`, { method: 'POST', headers: jsonAuth(tMarcos), body }),
  ]);
}
// versão propositalmente inválida — o exemplo fixo do ROTEIRO-DE-TESTE.md
await chamar(`/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}`, {
  method: 'POST', headers: jsonAuth(tCleber),
  body: JSON.stringify({ codigo: '05877', quantidade: 30, matricula: '04127', confirmado: true, versao: 'AAAAAAAAAAA=' }),
});

// ------------------------------------------------------- 5. fechamentos
console.log('== 5. fechamentos (métrica de negócio) ==');
for (const doc of ['000147901/1', '000148415/2']) {
  const r = await chamar(`/conferencia/fechamento?documento=${encodeURIComponent(doc)}`, {
    method: 'POST', headers: jsonAuth(tCleber), body: JSON.stringify({ confirmado: true }),
  });
  console.log(`   fechamento ${doc}: ${r?.status}`);
}

// ------------------------------------------------------- 6. reset e resumo
console.log('== 6. reset pós-rajada ==');
{
  const r = await chamar('/demo/reset', { method: 'POST', headers: auth(tRosana) });
  console.log('  ', r?.status, await r?.text());
}

const seg = ((Date.now() - inicio) / 1000).toFixed(0);
console.log('\n================ RESUMO ================');
console.log(`duração:                  ${seg}s`);
console.log(`requisições diretas à API: ${conta.api}   (1 trace de servidor cada)`);
console.log(`sessões de navegador:      ${conta.sessoes}`);
console.log(`navegações (rotas/páginas):${conta.browserNav}   (1 trace de carregamento cada)`);
console.log(`interações (cliques/bips): ${conta.browserInterac}  (trace de clique + chamada de API dentro)`);
console.log(`falhas de rede no script:  ${conta.erros}`);
console.log('status das respostas:', [...porStatus.entries()].sort().map(([k, v]) => `${k}:${v}`).join('  '));
console.log(`TOTAL DE TRACES ~ ${conta.api + conta.browserNav + conta.browserInterac}`);
console.log('========================================');
