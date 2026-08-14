// Rajada de tráfego real contra a POC publicada, para preencher o SigNoz com dado dos três
// sinais em todas as camadas: navegador (traces/vitals/erros), API (traces/métricas/logs) e
// banco (spans de SqlClient disparados por cada chamada). Script descartável — não faz parte
// do repositório, roda uma vez e é apagado.
import { chromium } from 'playwright';

const WEB = 'https://poc-mundial.exai.extreme.digital';
const API = `${WEB}/api`;
const espera = (ms) => new Promise((r) => setTimeout(r, ms));

async function entrar(page, matricula, senha = 'mundial') {
  await page.goto(`${WEB}/entrar`, { waitUntil: 'networkidle' });
  await page.fill('#mat', matricula);
  await page.fill('#sen', senha);
  await page.click('button[type=submit]');
  await espera(800);
}

async function bipar(page, codigo) {
  await page.fill('#leitura', codigo);
  await page.press('#leitura', 'Enter');
  await espera(700);
}

// A massa da demo ao vivo já tinha se afastado do estado semeado (doca 3 apareceu fechada
// de um teste anterior) — reseta antes de rodar os roteiros, que assumem o estado do
// ROTEIRO-DE-TESTE.md exatamente.
console.log('== resetando a massa de demonstração antes da rajada ==');
{
  const r = await fetch(`${API}/entrar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ matricula: '04310', senha: 'mundial' }),
  });
  const tPre = (await r.json()).token;
  const reset = await fetch(`${API}/demo/reset`, { method: 'POST', headers: { Authorization: `Bearer ${tPre}` } });
  console.log('  demo/reset (pré-rajada):', reset.status, await reset.text());
}

console.log('== camada navegador: sessões reais via Chromium headless ==');
const navegador = await chromium.launch();

// Sessão 1 — logins recusados (três motivos diferentes de recusa em /api/entrar)
{
  const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
  const page = await ctx.newPage();
  await entrar(page, '99999');
  await entrar(page, '04127', 'errada');
  await entrar(page, '05001');
  await ctx.close();
  console.log('  sessão 1 (logins recusados) ok');
}

// Sessão 2 — Cleber (04127): doca 1 e doca 2, leituras de todo tipo, um lançamento por doca
{
  const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
  const page = await ctx.newPage();
  await entrar(page, '04127');
  await page.waitForURL('**/docas', { timeout: 15000 });
  await espera(1000);

  await page.goto(`${WEB}/conferencia/000148372%2F1`, { waitUntil: 'networkidle' });
  await espera(1000);
  await bipar(page, '7891234522015'); // aceito, pendente
  await page.fill('.qtd input', '24');
  await page.press('.qtd input', 'Enter');
  await espera(800);
  await bipar(page, '7899999000123'); // recusado — não cadastrado
  await bipar(page, '7894455000012'); // recusado — outro fornecedor
  await bipar(page, '7890000111222'); // ambíguo

  await page.goto(`${WEB}/docas`, { waitUntil: 'networkidle' });
  await espera(800);
  await page.goto(`${WEB}/conferencia/000147901%2F1`, { waitUntil: 'networkidle' });
  await espera(1000);
  await bipar(page, '7891234511019'); // água, zerada, aceita direto
  await page.fill('.qtd input', '80');
  await page.press('.qtd input', 'Enter');
  await espera(800);

  await page.goto(`${WEB}/conferencia/000147744%2F1`, { waitUntil: 'networkidle' }); // fechada
  await espera(800);
  await ctx.close();
  console.log('  sessão 2 (Cleber, docas 1/2/4) ok');
}

// Sessão 3 — Rosana (04310): permissão de inclusão, cadastro de códigos, consultas
{
  const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
  const page = await ctx.newPage();
  await entrar(page, '04310');
  await page.waitForURL('**/docas', { timeout: 15000 });
  await espera(800);

  await page.goto(`${WEB}/conferencia/000148415%2F2`, { waitUntil: 'networkidle' });
  await espera(1000);
  await bipar(page, '7899999000123'); // recusado, com oferta de cadastro

  await page.goto(`${WEB}/codigos?codigo=04127`, { waitUntil: 'networkidle' });
  await espera(1000);

  await page.goto(`${WEB}/consultas`, { waitUntil: 'networkidle' });
  await espera(1000);
  await page.click('button:has-text("Fornecedores")').catch(() => {});
  await espera(800);
  await page.click('button:has-text("Auditoria")').catch(() => {});
  await espera(800);
  await ctx.close();
  console.log('  sessão 3 (Rosana, permissão/consultas) ok');
}

await navegador.close();

console.log('== camada API/banco: chamadas diretas para volume e casos de borda ==');

async function token(matricula, senha = 'mundial') {
  const r = await fetch(`${API}/entrar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ matricula, senha }),
  });
  if (!r.ok) return null;
  return (await r.json()).token;
}

const tCleber = await token('04127');
const tRosana = await token('04310');
const auth = (t) => ({ Authorization: `Bearer ${t}` });

const leituras = [
  ['/docas', tCleber],
  ['/conferencias?pagina=0&tamanho=10', tRosana],
  ['/conferencias?busca=PRIMAVERA', tRosana],
  ['/fornecedores', tRosana],
  ['/produtos/04127', tCleber],
  ['/produtos/04127/etiqueta', tCleber],
  ['/produtos/04127/etiqueta?codigoBarras=7891234567897', tCleber],
  ['/demo/codigos', null],
];
for (let i = 0; i < 3; i++) {
  for (const [caminho, tok] of leituras) {
    await fetch(`${API}${caminho}`, { headers: tok ? auth(tok) : {} }).catch(() => {});
  }
}
console.log('  leituras em rajada (x3) ok');

// 401 — sem token
await fetch(`${API}/docas`);
// 403 — Cleber não tem log_even:consultar
await fetch(`${API}/auditoria`, { headers: auth(tCleber) });
// 409 — concorrência otimista com versao inválida (mesmo exemplo do ROTEIRO-DE-TESTE.md)
await fetch(`${API}/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}`, {
  method: 'POST',
  headers: { ...auth(tCleber), 'Content-Type': 'application/json' },
  body: JSON.stringify({ codigo: '05877', quantidade: 30, matricula: '04127', confirmado: true, versao: 'AAAAAAAAAAA=' }),
});
console.log('  401/403/409 ok');

// estorno do que a sessão 2 lançou na doca 1 (produto 05877 — Suco Uva)
await fetch(
  `${API}/conferencia/lancamentos?documento=${encodeURIComponent('000148372/1')}&codigo=05877&confirmado=true`,
  { method: 'DELETE', headers: auth(tCleber) },
);
console.log('  estorno ok');

// fechamento da doca 2, com divergência (confirmado=true) — gera Metricas.Finalizacao
await fetch(`${API}/conferencia/fechamento?documento=${encodeURIComponent('000147901/1')}`, {
  method: 'POST',
  headers: { ...auth(tCleber), 'Content-Type': 'application/json' },
  body: JSON.stringify({ confirmado: true }),
});
console.log('  fechamento da doca 2 ok');

// devolve a massa ao estado semeado — a rajada não deve deixar rastro na demonstração real
const reset = await fetch(`${API}/demo/reset`, { method: 'POST', headers: auth(tRosana) });
console.log('  demo/reset:', reset.status, await reset.text());

console.log('== fim ==');
