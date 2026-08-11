// Grava um percurso narrado da aplicação, no ritmo de quem assiste — não no do robô.
// A narração é injetada como uma faixa sobre a própria página, então aparece no vídeo.
// Roda em container Playwright, contra a app real subida pelo compose.
import { chromium } from 'playwright';

const WEB = process.env.WEB || 'http://web:3000';
const SAIDA = '/saida';

const espera = (ms) => new Promise(r => setTimeout(r, ms));

const navegador = await chromium.launch();
const contexto = await navegador.newContext({
  viewport: { width: 1360, height: 800 },
  recordVideo: { dir: SAIDA, size: { width: 1360, height: 800 } },
});
const page = await contexto.newPage();

// O navegador roda dentro da rede do compose: localhost seria o próprio container.
await page.route('**://localhost:5001/**', route =>
  route.continue({ url: route.request().url().replace('http://localhost:5001', 'http://api:5000') }));

/** Faixa de narração sobre a página. Reinjetada a cada navegação, porque o DOM é trocado. */
async function narrar(texto, ms = 3200) {
  await page.evaluate((t) => {
    let faixa = document.getElementById('__narracao');
    if (!faixa) {
      faixa = document.createElement('div');
      faixa.id = '__narracao';
      // No topo, com fundo sólido: no rodapé a faixa competia com o conteúdo da página
      // e saía cortada no vídeo.
      faixa.style.cssText = [
        'position:fixed', 'left:0', 'right:0', 'top:0', 'z-index:2147483647',
        'background:#0A0E10', 'borderBottom:2px solid #3DD6D0',
        'color:#E8EEF2', 'padding:18px 40px',
        'font:600 20px/1.4 system-ui,-apple-system,"Segoe UI",Roboto,sans-serif',
        'letter-spacing:-.01em', 'text-align:center', 'pointer-events:none',
        'boxShadow:0 8px 24px rgba(0,0,0,.5)',
      ].join(';');
      faixa.style.borderBottom = '2px solid #3DD6D0';
      faixa.style.boxShadow = '0 8px 24px rgba(0,0,0,.5)';
      document.body.appendChild(faixa);
    }
    faixa.textContent = t;
    faixa.style.display = 'block';
    document.body.style.paddingTop = faixa.offsetHeight + 'px';
  }, texto);
  await espera(ms);
}

async function limparNarracao() {
  await page.evaluate(() => {
    const f = document.getElementById('__narracao');
    if (f) { f.style.display = 'none'; document.body.style.paddingTop = '0'; }
  }).catch(() => {});
  await espera(300);
}

/** Digita devagar, como alguém digitando de verdade. */
async function digitar(seletor, texto, atraso = 45) {
  await page.click(seletor);
  await page.fill(seletor, '');
  await page.type(seletor, texto, { delay: atraso });
}

async function bipar(codigo) {
  await digitar('#leitura', codigo, 28);
  await espera(500);
  await page.press('#leitura', 'Enter');
  await espera(1100);
}

// ─────────────────────────────────────────────────────────────────────
console.log('· abertura');
await page.goto(`${WEB}/entrar`, { waitUntil: 'networkidle' });
await espera(700);
await narrar('Mundial · Conferência de Recebimento — do Visual FoxPro de 2011 para .NET 10 e Angular 22', 4200);

console.log('· login recusado');
await limparNarracao();
await narrar('As mensagens são as do sistema antigo, literais. Matrícula que não existe:', 2600);
await digitar('#mat', '99999');
await digitar('#sen', 'mundial', 60);
await page.click('button[type=submit]');
await espera(1400);
await narrar('"Matrícula não cadastrada! Favor contactar supervisor" — regra RK-046f5592ef5b', 3400);

console.log('· login aceito');
await limparNarracao();
await digitar('#mat', '04127');
await digitar('#sen', 'mundial', 60);
await narrar('Cleber é operador de doca. Não tem permissão para incluir código novo.', 2800);
await page.click('button[type=submit]');
await page.waitForURL('**/docas', { timeout: 15000 });
await espera(1200);

console.log('· painel de docas');
await narrar('O painel ordena por tempo de doca aberta, não por número. A doca 2 está há mais de três horas — anel âmbar.', 4600);
await limparNarracao();
await espera(600);

console.log('· conferência');
await page.click('.cartao >> nth=1');
await page.waitForURL('**/conferencia/**', { timeout: 15000 });
await espera(1300);
await narrar('Doca 1 · Bebidas Primavera. A cerveja veio com 6 a menos — divergência é dado, não erro.', 4200);

await limparNarracao();
await narrar('O operador bipa. O resultado aparece sempre no mesmo lugar, à direita.', 2800);
await bipar('7891234522015');
await narrar('Aceito. Suco de uva, caixa com 6.', 2400);
await limparNarracao();
await digitar('.qtd input', '24', 90);
await page.press('.qtd input', 'Enter');
await espera(1300);
await narrar('Lançado. O foco volta sozinho ao campo — a tela inteira funciona sem mouse.', 3200);

console.log('· confirmação de requantidade');
await limparNarracao();
await narrar('Agora um item que já tem 40 lançadas:', 2200);
await bipar('7891234567897');
await digitar('.qtd input', '38', 90);
await page.press('.qtd input', 'Enter');
await espera(1200);
await narrar('O legado pergunta antes de sobrescrever — RK-8233e231d6fb. O lançamento substitui, e substituir é destrutivo.', 4600);
const sim = page.locator('.dacoes .btn').last();
if (await sim.isVisible()) { await sim.click(); await espera(1400); }
await narrar('Virou 38. Substituiu os 40 — não somou para 78.', 3200);

console.log('· recusas');
await limparNarracao();
await narrar('Código que não existe:', 1800);
await bipar('7899999000123');
await narrar('Recusado, com som diferente. Cleber não pode incluir, então nada de oferta de cadastro.', 3600);

await limparNarracao();
await narrar('Código que existe, mas não pertence a esta nota:', 2200);
await bipar('7894455000012');
await narrar('"Código Não cadastrado para BEBIDAS PRIMAVERA LTDA!"', 3000);

await limparNarracao();
await narrar('E um código que responde a dois produtos:', 2200);
await bipar('7890000111222');
await narrar('Leitura ambígua. O sistema mostra os candidatos e não escolhe por você.', 3800);

console.log('· fechamento');
await limparNarracao();
await narrar('F2 finaliza a conferência.', 2000);
await page.keyboard.press('F2');
await espera(1000);
await narrar('Pergunta uma vez, e avisa quantos itens ficam pendentes.', 3200);
const simFinal = page.locator('.dacoes .btn').last();
if (await simFinal.isVisible()) { await simFinal.click(); await espera(1800); }
await narrar('Fechado. A tela inteira muda de modo — somente leitura, com quem fechou registrado.', 4000);

console.log('· permissão');
await limparNarracao();
await page.goto(`${WEB}/docas`, { waitUntil: 'networkidle' });
await page.click('button:has-text("Sair")').catch(() => {});
await espera(800);
await narrar('A mesma tela, agora com a supervisora. A permissão é por tabela, e vem do token.', 3600);
await digitar('#mat', '04310');
await digitar('#sen', 'mundial', 60);
await page.click('button[type=submit]');
await page.waitForURL('**/docas', { timeout: 15000 });
await espera(1000);

await page.goto(`${WEB}/conferencia/000148415%2F2`, { waitUntil: 'networkidle' });
await espera(1200);
await bipar('7899999000123');
await narrar('Mesmo código recusado — mas Rosana pode incluir, então aparece "Deseja Cadastrar agora?"', 4400);

console.log('· cadastro');
await limparNarracao();
await page.goto(`${WEB}/codigos?codigo=04127`, { waitUntil: 'networkidle' });
await espera(1400);
await narrar('Cadastro de códigos de embalagem, com a etiqueta ZPL gerada ao lado — byte a byte igual à do legado.', 4600);

await limparNarracao();
await narrar('Um código que já pertence a outro produto:', 2200);
await digitar('#dun1', '17891234500010', 35);
await page.click('button:has-text("Gravar")');
await espera(1400);
await narrar('O erro aparece no campo que o causou, nomeando o produto dono. Um resumo no topo perderia essa informação.', 4800);

console.log('· consultas');
await limparNarracao();
await page.goto(`${WEB}/consultas`, { waitUntil: 'networkidle' });
await espera(1400);
await narrar('A visão do supervisor: conferências, fornecedores e a trilha de auditoria.', 3600);
await page.click('button:has-text("Auditoria")');
await espera(1500);
await narrar('A trilha no formato recuperado do FoxPro: quem, quando, e o valor antes e depois.', 4400);

console.log('· encerramento');
await limparNarracao();
await narrar('68 das 70 regras do legado implementadas, cada uma com teste citando sua chave. As duas restantes são erro de ODBC, fora do escopo.', 5200);

await espera(800);
await contexto.close();   // fecha o contexto para o vídeo ser gravado
await navegador.close();
console.log('vídeo gravado em', SAIDA);
