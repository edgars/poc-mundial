import { chromium } from 'playwright';
const WEB = 'https://poc-mundial.exai.extreme.digital';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1360, height: 860 } });
page.on('console', (m) => console.log('CONSOLE:', m.type(), m.text()));
page.on('pageerror', (e) => console.log('PAGEERROR:', e.message));
page.on('response', (r) => { if (!r.ok()) console.log('RES!ok:', r.status(), r.url()); });

await page.goto(`${WEB}/entrar`, { waitUntil: 'networkidle' });
await page.fill('#mat', '04310');
await page.fill('#sen', 'mundial');
await page.click('button[type=submit]');
await page.waitForURL('**/docas', { timeout: 15000 });
console.log('logged in as 04310, url=', page.url());

await page.goto(`${WEB}/conferencia/000148415%2F2`, { waitUntil: 'networkidle' });
console.log('after goto, url=', page.url());
await page.waitForTimeout(1500);
console.log(await page.content());
await browser.close();
