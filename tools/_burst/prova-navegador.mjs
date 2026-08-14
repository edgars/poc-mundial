// Prova da camada do navegador: abre uma sessão real e escuta as respostas de /otlp.
// Se o Angular está exportando, aparecem POSTs para /otlp/v1/{traces,metrics,logs} com 200.
import { chromium } from 'playwright';

const WEB = 'https://poc-mundial.exai.extreme.digital';
const espera = (ms) => new Promise((r) => setTimeout(r, ms));

const navegador = await chromium.launch();
const ctx = await navegador.newContext({ viewport: { width: 1360, height: 860 } });
const page = await ctx.newPage();

const otlp = [];
page.on('response', (r) => {
  const u = r.url();
  if (u.includes('/otlp/')) otlp.push(`${r.status()} ${u.replace(WEB, '')}`);
});

await page.goto(`${WEB}/entrar`, { waitUntil: 'networkidle' });
await page.fill('#mat', '04127');
await page.fill('#sen', 'mundial');
await page.click('button[type=submit]');
await page.waitForURL('**/docas', { timeout: 15000 }).catch(() => {});
await espera(1500);
await page.goto(`${WEB}/conferencia/${encodeURIComponent('000148372/1')}`, { waitUntil: 'networkidle' });
await page.fill('#leitura', '7891234522015').catch(() => {});
await page.press('#leitura', 'Enter').catch(() => {});
await espera(2000);
await page.goto(`${WEB}/consultas`, { waitUntil: 'networkidle' });
// O exportador do navegador manda em lote; dá tempo de um flush acontecer.
await espera(8000);

console.log(`POSTs para /otlp observados: ${otlp.length}`);
for (const l of otlp) console.log('  ', l);

await ctx.close();
await navegador.close();
