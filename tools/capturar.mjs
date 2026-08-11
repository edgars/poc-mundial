// Percorre os roteiros do ROTEIRO-DE-TESTE.md e captura a tela em cada passo.
// Roda em container Playwright, contra a app real subida pelo compose.
import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

const WEB = process.env.WEB || 'http://web:3000';
const SAIDA = '/saida';
mkdirSync(SAIDA, { recursive: true });

const passos = [];
let n = 0;

async function tirar(page, titulo, legenda) {
  n += 1;
  const arquivo = `${String(n).padStart(2, '0')}-${titulo.toLowerCase().replace(/[^a-z0-9]+/g, '-')}.png`;
  await page.screenshot({ path: `${SAIDA}/${arquivo}` });
  passos.push({ arquivo, titulo, legenda });
  console.log(`  ${arquivo}`);
}

const espera = (ms) => new Promise(r => setTimeout(r, ms));

async function entrar(page, matricula, senha = 'mundial') {
  await page.goto(`${WEB}/entrar`, { waitUntil: 'networkidle' });
  await page.fill('#mat', matricula);
  await page.fill('#sen', senha);
}

async function bipar(page, codigo) {
  await page.fill('#leitura', codigo);
  await page.press('#leitura', 'Enter');
  await espera(700);
}

const navegador = await chromium.launch();
const page = await navegador.newPage({ viewport: { width: 1360, height: 860 } });

// O navegador roda dentro da rede do compose: "localhost" seria o próprio container.
// O index.html fixa window.API_URL em tempo de subida, então redirecionamos por rota —
// toda chamada a localhost:5001 vai para o serviço api pelo nome.
await page.route('**://localhost:5001/**', route =>
  route.continue({ url: route.request().url().replace('http://localhost:5001', 'http://api:5000') }));

console.log('Roteiro 1 · Cleber confere a doca 1');
await page.goto(`${WEB}/entrar`, { waitUntil: 'networkidle' });
await tirar(page, 'Tela de entrada', 'A primeira tela. Foco já no campo de matrícula — quem opera na doca digita e tecla Enter, sem tocar no mouse.');

await entrar(page, '99999');
await page.click('button[type=submit]');
await espera(900);
await tirar(page, 'Matricula nao cadastrada', 'Matrícula que não existe. A mensagem é o texto literal do legado, e a chave da regra é RK-046f5592ef5b.');

await entrar(page, '04127', 'errada');
await page.click('button[type=submit]');
await espera(900);
await tirar(page, 'Senha invalida', 'Senha errada. "Senha inválida" — RK-f8293cf9dbb3. Não revela se a matrícula existe.');

await entrar(page, '05001');
await page.click('button[type=submit]');
await espera(900);
await tirar(page, 'Nivel insuficiente', 'Paulo tem nível 1. Barrado antes de qualquer tela — RK-8ffd715ce9ad, a condição legada vsenha < 3.');

await entrar(page, '04127');
await page.click('button[type=submit]');
await page.waitForURL('**/docas', { timeout: 15000 });
await espera(1200);
await tirar(page, 'Painel de docas', 'Quatro docas. A 2 vem primeiro com anel âmbar porque está aberta há mais de três horas — a ordem é por tempo aberto, não por número. Sem medidor de ocupação.');

await page.click('.cartao >> nth=1');
await page.waitForURL('**/conferencia/**', { timeout: 15000 });
await espera(1200);
await tirar(page, 'Conferencia aberta', 'Doca 1, Bebidas Primavera. Três itens já lançados, um pendente, e a cerveja com divergência de -6. O painel focal à direita é o ponto único de leitura.');

await bipar(page, '7891234522015');
await tirar(page, 'Leitura aceita', 'Suco de uva aceito. Selo verde, descrição, embalagem e o campo de quantidade já com o valor da nota selecionado.');

await page.fill('.qtd input', '24');
await page.press('.qtd input', 'Enter');
await espera(900);
await tirar(page, 'Item lancado', 'Lançado. A linha entra com pílula ok, a pendência some, e o foco volta sozinho ao campo de leitura.');

await bipar(page, '7891234567897');
await page.fill('.qtd input', '38');
await page.press('.qtd input', 'Enter');   // o diálogo só aparece ao tentar gravar
await espera(900);
await tirar(page, 'Confirmacao de requantidade', 'Ao tentar gravar 38 sobre os 40 que já existiam, RK-8233e231d6fb pede confirmação. O aviso existe porque o lançamento substitui — e substituir é destrutivo. Somar não precisaria perguntar.');

const sim = page.locator('.dacoes .btn').last();
if (await sim.isVisible()) { await sim.click(); await espera(1200); }
await tirar(page, 'Substituiu nao somou', 'Confirmado. O valor virou 38 — substituiu os 40, não somou para 78. A linha passa a marcar divergência -2, em âmbar.');

await bipar(page, '7899999000123');
await tirar(page, 'Codigo nao cadastrado', 'Código que não existe. Som diferente, selo vermelho. Cleber não tem permissão de inclusão, então não aparece oferta de cadastrar.');

await bipar(page, '7894455000012');
await tirar(page, 'Codigo de outro fornecedor', 'Sabão em pó existe no cadastro, mas não está nesta nota. RK-732bb9300bad nomeia o fornecedor do documento.');

await bipar(page, '7890000111222');
await tirar(page, 'Leitura ambigua', 'O mesmo código responde a dois produtos. O sistema recusa e mostra os candidatos — nunca escolhe por você.');

await page.keyboard.press('F2');
await espera(700);
await tirar(page, 'Finalizar conferencia', 'F2 finaliza. RK-fa93a48fbecc pergunta uma vez, e avisa quantos itens ficam pendentes.');

const simFinal = page.locator('.dacoes .btn').last();
if (await simFinal.isVisible()) { await simFinal.click(); await espera(1400); }
await tirar(page, 'Documento fechado', 'Fechado. A tela inteira muda de modo: faixa âmbar, campo de leitura some, nada de botão cinza espalhado.');

console.log('Roteiro 3 · Rosana e a permissão de inclusão');
await page.goto(`${WEB}/docas`, { waitUntil: 'networkidle' });
await page.click('button:has-text("Sair")').catch(() => {});
await espera(700);
await entrar(page, '04310');
await page.click('button[type=submit]');
await page.waitForURL('**/docas', { timeout: 15000 });
await espera(1000);

await page.goto(`${WEB}/conferencia/000148415%2F2`, { waitUntil: 'networkidle' });
await espera(1200);
await tirar(page, 'Doca aguardando', 'Doca 3 vista pela Rosana. Nada lançado ainda — todos os itens em aguarda.');

await bipar(page, '7899999000123');
await tirar(page, 'Oferta de cadastrar', 'Mesmo código inexistente, agora com a Rosana. Ela tem permissão de inclusão, então a recusa vem acompanhada da oferta de cadastrar na hora — RK-dab7d2033e2e.');

console.log('Roteiro 4 · Documento fechado');
await page.goto(`${WEB}/conferencia/000147744%2F1`, { waitUntil: 'networkidle' });
await espera(1200);
await tirar(page, 'Somente leitura', 'Documento da doca 4, fechado desde o seed. Somente leitura, com quem fechou registrado no topo.');

await navegador.close();
console.log(JSON.stringify(passos, null, 2));
import { writeFileSync } from 'node:fs';
writeFileSync(`${SAIDA}/passos.json`, JSON.stringify(passos, null, 2));
