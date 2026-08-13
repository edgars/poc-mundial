// Proxy do `ng serve` para /otlp, só para desenvolvimento local fora de container.
//
// Em produção quem faz isto é o nginx do container (web/entrypoint.sh): recebe em /otlp na
// própria origem e repassa ao coletor pondo o Authorization, porque o ingest recusa preflight
// CORS e o token não pode viver no JavaScript do navegador. Aqui é o mesmo papel, só que quem
// termina a conexão é o dev-server do Angular (Vite) em vez do nginx.
//
// Sem segredo embutido no arquivo: lê OTEL_EXPORTER_OTLP_ENDPOINT e _HEADERS do .env na raiz do
// repo (ou do ambiente, se já exportadas) no momento em que o `ng serve` sobe — este arquivo é
// versionado, o .env não.
const fs = require('fs');
const path = require('path');

function lerVariavel(chave) {
  if (process.env[chave]) return process.env[chave];

  const envPath = path.resolve(__dirname, '..', '.env');
  if (!fs.existsSync(envPath)) return undefined;

  const linha = fs
    .readFileSync(envPath, 'utf-8')
    .split('\n')
    .find((l) => l.startsWith(`${chave}=`));
  return linha ? linha.slice(chave.length + 1).trim() : undefined;
}

const coletor = (lerVariavel('OTEL_EXPORTER_OTLP_ENDPOINT') || '').replace(/\/+$/, '');
const linhaAutorizacao = lerVariavel('OTEL_EXPORTER_OTLP_HEADERS') || '';
const separador = linhaAutorizacao.indexOf('=');
const cabecalhoNome = separador > -1 ? linhaAutorizacao.slice(0, separador) : null;
const cabecalhoValor = separador > -1 ? linhaAutorizacao.slice(separador + 1) : null;

if (!coletor || !/^https?:\/\//.test(coletor)) {
  // Sem coletor configurado, igual ao entrypoint.sh: /otlp não existe e nada quebra.
  module.exports = {};
} else {
  module.exports = {
    '^/otlp/.*': {
      target: coletor,
      changeOrigin: true,
      secure: true,
      // O exportador do navegador posta em /otlp/v1/traces; o coletor espera /v1/traces —
      // mesma regra do `proxy_pass $coletor/$1;` do nginx em produção.
      rewrite: (caminho) => caminho.replace(/^\/otlp/, ''),
      headers: cabecalhoNome ? { [cabecalhoNome]: cabecalhoValor } : {},
    },
  };
}
