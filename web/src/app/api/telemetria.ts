import { WebTracerProvider, BatchSpanProcessor } from '@opentelemetry/sdk-trace-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import {
  ATTR_SERVICE_NAME,
  ATTR_SERVICE_VERSION,
} from '@opentelemetry/semantic-conventions';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';

declare global {
  interface Window {
    OTEL_WEB?: string;
  }
}

/**
 * Telemetria do navegador → coletor OTLP (SigNoz).
 *
 * Os spans vão para /otlp/v1/traces na PRÓPRIA origem, nunca direto para o coletor.
 * Dois motivos, os dois testados contra o ingest real:
 *   1. o ingest responde 401 ao preflight CORS — o navegador nem chegaria a postar;
 *   2. o bearer do coletor não pode viver no JavaScript, onde qualquer visitante o lê.
 * Quem põe o Authorization é o nginx do container web (web/entrypoint.sh), no compose local
 * e na máquina publicada — lá o Caddy só encaminha /otlp adiante, como faz com o resto que
 * não é /api. Uma implementação só, e o token não sai do servidor.
 *
 * Desligada com OTEL_WEB=false; a flag é injetada no index.html na subida do container,
 * igual à API_URL.
 */
export function iniciarTelemetria(): void {
  if (window.OTEL_WEB !== 'true') return;

  const provider = new WebTracerProvider({
    resource: resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'mundial-web',
      [ATTR_SERVICE_VERSION]: '0.1.0',
    }),
    spanProcessors: [new BatchSpanProcessor(new OTLPTraceExporter({ url: '/otlp/v1/traces' }))],
  });

  provider.register();

  // A API é outra origem no compose local (:5001 contra :3000), então o traceparent
  // precisa de autorização explícita para atravessar. Sem isto o trace quebra em dois:
  // um span solto no navegador e outro solto na API, sem parentesco.
  const origemApi = window.API_URL || 'http://localhost:5000';

  registerInstrumentations({
    tracerProvider: provider,
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new FetchInstrumentation({
        // O HttpClient do Angular 22 usa fetch por padrão — o FetchBackend é o provider
        // padrão de provideHttpClient, e withXhr() é que seria o desvio. Instrumentar XHR
        // aqui não pegaria chamada nenhuma.
        propagateTraceHeaderCorsUrls: [new RegExp('^' + escaparRegex(origemApi))],
        clearTimingResources: true,
        // Postar span sobre o próprio POST de span é laço infinito.
        ignoreUrls: [/\/otlp\//],
      }),
    ],
  });
}

function escaparRegex(texto: string): string {
  return texto.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
