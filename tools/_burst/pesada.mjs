// Rajada pesada: mais sessões de navegador em paralelo, mais volume de chamadas de API,
// concorrência de propósito (dois lançamentos na mesma versão) para gerar 409 de verdade.
import { chromium } from 'playwright';

const WEB = 'https://poc-mundial.exai.extreme.digital';
const API = `${WEB}/api`;
const espera = (ms) => new Promise((r) => setTimeout(r, ms));

async function token(matricula, senha = 'mundial') {
  const r = await fetch(`${API}/entrar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ matricula, senha }),
  });
  if (!r.ok) return null;
  return (await r.json()).token;
}
const auth = (t) => ({ Authorization: `Bearer ${t}` });

console.log('== reset pré-rajada ==');
{
  const t = await token('04310');
  const r = await fetch(`${API}/demo/reset`, { method: 'POST', headers: auth(t) });
  console.log('  ', r.status, await r.text());
}

async function entrar(page, matricula, senha = 'mundial') {
  await page.goto(`${WEB}/entrar`, { waitUntil: 'networkidle' });
  await page.fill('#mat', matricula);
  await page.fill('#sen', senha);
  await page.click('button[type=submit]');
  await espera(600);
}
async function bipar(page, codigo) {
  await page.fill('#leitura', codigo);
  await page.press('#leitura', 'Enter');
  await espera(500);
}

console.log('== camada navegador: sessões concorrentes ==');
const navegador = await chromium.launch();

const codigosDemo = [
  '7891234567897', '7891234500013', '7891234511019', '7891234522015',
  '7899876500019', '7894455000012', '7890000111222', '7899999000123',
];

async function sessaoLeituras(persona, doc, n) {
  const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
  const page = await ctx.newPage();
  try {
    await entrar(page, persona);
    await page.waitForURL('**/docas', { timeout: 15000 }).catch(() => {});
    await espera(500);
    await page.goto(`${WEB}/conferencia/${encodeURIComponent(doc)}`, { waitUntil: 'networkidle' });
    await espera(800);
    for (let i = 0; i < n; i++) {
      await bipar(page, codigosDemo[(i + persona.length) % codigosDemo.length]);
    }
    await page.goto(`${WEB}/docas`, { waitUntil: 'networkidle' }).catch(() => {});
    await espera(400);
    await page.goto(`${WEB}/consultas`, { waitUntil: 'networkidle' }).catch(() => {});
    await espera(400);
  } catch (e) {
    console.log(`  sessão ${persona}/${doc} — aviso: ${e.message.split('\n')[0]}`);
  } finally {
    await ctx.close();
  }
}

async function sessaoLoginsRuins() {
  const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
  const page = await ctx.newPage();
  try {
    await entrar(page, '99999');
    await entrar(page, '04127', 'errada');
    await entrar(page, '05001');
    await entrar(page, '04982', 'tambem-errada');
  } finally {
    await ctx.close();
  }
}

const docas = ['000148372/1', '000147901/1', '000148415/2', '000147744/1'];
const personas = ['04127', '04310', '04982'];

const tarefas = [];
tarefas.push(sessaoLoginsRuins());
tarefas.push(sessaoLoginsRuins());
for (let i = 0; i < 8; i++) {
  const persona = personas[i % personas.length];
  const doc = docas[i % docas.length];
  tarefas.push(sessaoLeituras(persona, doc, 4));
}
await Promise.all(tarefas);
await navegador.close();
console.log('  ', tarefas.length, 'sessões de navegador concluídas');

console.log('== camada API/banco: rajada de volume ==');
const tCleber = await token('04127');
const tRosana = await token('04310');
const tMarcos = await token('04982');

const leituras = [
  ['/docas', tCleber], ['/docas', tRosana], ['/docas', tMarcos],
  ['/conferencias?pagina=0&tamanho=10', tRosana],
  ['/conferencias?pagina=1&tamanho=5', tRosana],
  ['/conferencias?busca=PRIMAVERA', tRosana],
  ['/conferencias?busca=LATICINIOS', tRosana],
  ['/fornecedores', tRosana],
  ['/fornecedores?busca=HIGIENE', tRosana],
  ['/auditoria', tRosana],
  ['/produtos/04127', tCleber], ['/produtos/04982', tCleber], ['/produtos/05310', tCleber],
  ['/produtos/05877', tCleber], ['/produtos/06120', tCleber], ['/produtos/06430', tCleber],
  ['/produtos/07001', tCleber],
  ['/produtos/04127/etiqueta', tCleber],
  ['/produtos/04127/etiqueta?codigoBarras=7891234567897', tCleber],
  ['/demo/codigos', null],
];

for (let onda = 0; onda < 6; onda++) {
  await Promise.all(leituras.map(([caminho, tok]) =>
    fetch(`${API}${caminho}`, { headers: tok ? auth(tok) : {} }).catch(() => {})));
  console.log(`  onda ${onda + 1}/6 (${leituras.length} chamadas concorrentes) ok`);
}

console.log('== casos de borda ==');
// 401 em rajada
await Promise.all(Array.from({ length: 5 }, () => fetch(`${API}/docas`)));
// 403 — Cleber sem log_even:consultar / forne:consultar
await Promise.all([
  fetch(`${API}/auditoria`, { headers: auth(tCleber) }),
  fetch(`${API}/auditoria`, { headers: auth(tMarcos) }),
]);
// 422 — leitura semântica de produto inexistente
await fetch(`${API}/produtos/99999999`, { headers: auth(tCleber) });

// 409 de verdade: duas gravações concorrentes na mesma versão do mesmo item
{
  const doc = await (await fetch(`${API}/conferencia?documento=${encodeURIComponent('000148372/1')}`,
    { headers: auth(tCleber) })).json();
  const item = doc.itens.find((i) => i.codigo === '05877') ?? doc.itens[0];
  const corpo = JSON.stringify({ codigo: item.codigo, quantidade: 24, matricula: '04127', confirmado: true, versao: item.versao });
  const [a, b] = await Promise.all([
    fetch(`${API}/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}`,
      { method: 'POST', headers: { ...auth(tCleber), 'Content-Type': 'application/json' }, body: corpo }),
    fetch(`${API}/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}`,
      { method: 'POST', headers: { ...auth(tMarcos), 'Content-Type': 'application/json' }, body: corpo }),
  ]);
  console.log('  409 concorrente: status', a.status, b.status);
}
// exemplo fixo do ROTEIRO-DE-TESTE.md, versão propositalmente inválida
await fetch(`${API}/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}`, {
  method: 'POST', headers: { ...auth(tCleber), 'Content-Type': 'application/json' },
  body: JSON.stringify({ codigo: '05877', quantidade: 30, matricula: '04127', confirmado: true, versao: 'AAAAAAAAAAA=' }),
});
console.log('  401/403/422/409 ok');

console.log('== fechamentos e estornos ==');
// estorna o que sobrou lançado na doca 1 (produto 05877) antes de tentar fechar
await fetch(`${API}/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}&codigo=05877&confirmado=true`,
  { method: 'DELETE', headers: auth(tCleber) }).catch(() => {});

// fecha doca 2 (com divergência) e doca 3 (o que a rajada de leituras tiver deixado pendente)
for (const doc of ['000147901/1', '000148415/2']) {
  const r = await fetch(`${API}/conferencia/fechamento?documento=${encodeURIComponent(doc)}`, {
    method: 'POST', headers: { ...auth(tCleber), 'Content-Type': 'application/json' },
    body: JSON.stringify({ confirmado: true }),
  });
  console.log(`  fechamento ${doc}:`, r.status);
}

console.log('== reset pós-rajada ==');
const reset = await fetch(`${API}/demo/reset`, { method: 'POST', headers: auth(tRosana) });
console.log('  ', reset.status, await reset.text());
console.log('== fim ==');
