import { Attributes, Counter, Histogram, Meter, Tracer, metrics, trace } from '@opentelemetry/api';
import { Logger, SeverityNumber, logs } from '@opentelemetry/api-logs';
import {
  BatchSpanProcessor,
  ReadableSpan,
  Span,
  SpanProcessor,
  WebTracerProvider,
} from '@opentelemetry/sdk-trace-web';
import { MeterProvider, PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { BatchLogRecordProcessor, LoggerProvider } from '@opentelemetry/sdk-logs';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import {
  AggregationTemporalityPreference,
  OTLPMetricExporter,
} from '@opentelemetry/exporter-metrics-otlp-http';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { onCLS, onFCP, onINP, onLCP, onTTFB, type Metric } from 'web-vitals';

declare global {
  interface Window {
    OTEL_WEB?: string;
    VERSAO?: string;
    // Também declarada em api.ts — as interfaces se fundem. Repetida aqui de propósito: este
    // módulo lê a variável e não deve depender de outro arquivo estar na mesma compilação.
    API_URL?: string;
  }
}

const NOME_SERVICO = 'mundial-web';

let ligada = false;
let tracer: Tracer | undefined;
let registrador: Logger | undefined;
let contadorErros: Counter | undefined;
let rotaAtual = '';

/** Chave da sessão do navegador. Não é a sessão de negócio; é para agrupar sinais. */
const CHAVE_SESSAO = 'otel.sessao';

/**
 * Telemetria do navegador → coletor OTLP (SigNoz): traces, métricas e logs.
 *
 * Os três sinais vão para /otlp/... na PRÓPRIA origem, nunca direto para o coletor.
 * Dois motivos, os dois testados contra o ingest real:
 *   1. o ingest responde 401 ao preflight CORS — o navegador nem chegaria a postar;
 *   2. o bearer do coletor não pode viver no JavaScript, onde qualquer visitante o lê.
 * Quem põe o Authorization é o nginx do container web (web/entrypoint.sh), no compose local
 * e na máquina publicada — lá o Caddy só encaminha /otlp adiante, como faz com o resto que
 * não é /api. O bloco do nginx casa /otlp/(.*), então serve aos três sinais sem mudança.
 *
 * Desligada com OTEL_WEB=false; a flag é injetada no index.html na subida do container,
 * igual à API_URL.
 */
export function iniciarTelemetria(): void {
  // Chamada duas vezes — recarga a quente no desenvolvimento, ou alguém movendo a chamada para
  // dentro do bootstrap sem tirar a de main.ts — criaria dois provedores e dois ouvintes de
  // erro, e cada exceção seria contada em dobro. Barato de evitar, caro de perceber.
  if (ligada) return;
  if (window.OTEL_WEB !== 'true') return;
  ligada = true;

  const recurso = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: NOME_SERVICO,
    // Gravada na imagem pelo build (ARG VERSAO). Antes era um literal '0.1.0' que nunca mudava,
    // e sem versão real não há como perguntar em qual deploy a tela ficou lenta.
    [ATTR_SERVICE_VERSION]: window.VERSAO || 'dev',
  });

  const sessao = obterSessao();

  const provedorRastros = new WebTracerProvider({
    resource: recurso,
    spanProcessors: [
      new ProcessadorSessao({ 'session.id': sessao }),
      new BatchSpanProcessor(new OTLPTraceExporter({ url: '/otlp/v1/traces' })),
    ],
  });
  provedorRastros.register();

  const provedorMetricas = new MeterProvider({
    resource: recurso,
    readers: [
      new PeriodicExportingMetricReader({
        exporter: new OTLPMetricExporter({
          url: '/otlp/v1/metrics',
          // O SigNoz guarda contador em delta; com cumulativo cada recarga da página reinicia a
          // série do zero e o gráfico vira serra.
          temporalityPreference: AggregationTemporalityPreference.DELTA,
        }),
        exportIntervalMillis: 60_000,
      }),
    ],
  });
  metrics.setGlobalMeterProvider(provedorMetricas);

  const provedorLogs = new LoggerProvider({
    resource: recurso,
    processors: [
      new BatchLogRecordProcessor({ exporter: new OTLPLogExporter({ url: '/otlp/v1/logs' }) }),
    ],
  });
  logs.setGlobalLoggerProvider(provedorLogs);

  tracer = trace.getTracer(NOME_SERVICO);
  registrador = logs.getLogger(NOME_SERVICO);
  const medidor = metrics.getMeter(NOME_SERVICO);
  contadorErros = medidor.createCounter('web.erros', {
    unit: '{erro}',
    description: 'Erros no navegador, por origem.',
  });

  // A API é outra origem no compose local (:5001 contra :3000), então o traceparent
  // precisa de autorização explícita para atravessar. Sem isto o trace quebra em dois:
  // um span solto no navegador e outro solto na API, sem parentesco.
  const origemApi = window.API_URL || 'http://localhost:5000';

  // Antes de registerInstrumentations, e não depois: o web-vitals liga o listener nativo do
  // INP chamando addEventListener sem `this` de objeto (comum em lib minificada). Com o
  // addEventListener já interceptado pelo UserInteractionInstrumentation abaixo, esse `this`
  // undefined vira chave de WeakMap — TypeError síncrono que aborta o main.ts inteiro antes do
  // bootstrap, e a página fica em branco sem erro nenhum visível fora do console.
  medirVitais(medidor);

  registerInstrumentations({
    tracerProvider: provedorRastros,
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
      // Liga o clique à chamada que ele dispara. Uma ressalva honesta: o app é zoneless
      // (provideZonelessChangeDetection), então não há ZoneContextManager — que é o que a
      // documentação desta instrumentação recomenda para atravessar fronteira assíncrona.
      // Com o StackContextManager padrão, o fetch emitido no subscribe síncrono do HttpClient
      // cai dentro do span do clique; o que passa por setTimeout ou debounce sai como span
      // solto. Trazer o zone.js de volta só por isto seria pior do que a cobertura parcial.
      new UserInteractionInstrumentation(),
    ],
  });

  ouvirErrosGlobais();
  // Depois de medirVitais, e não antes: o web-vitals fecha INP e CLS num ouvinte de
  // visibilitychange, e ouvinte de DOM dispara na ordem em que foi registrado. Invertidas as
  // duas linhas, a descarga aconteceria antes de os dois valores existirem — e eles são
  // justamente os que só chegam no fim da visita.
  descarregarAoSair([provedorRastros, provedorMetricas, provedorLogs]);
}

/**
 * Web Vitals como métrica. É o número que diz se a tela trava no coletor de mão do armazém —
 * o INP especialmente, que mede a demora entre o toque e a resposta visível.
 *
 * Sem rótulo de rota de propósito: LCP e FCP descrevem o carregamento inicial, que nesta SPA é
 * sempre a tela de login; CLS e INP chegam acumulados no fim da visita, quando a pessoa já
 * passou por várias telas. Carimbar rota em qualquer um dos dois seria inventar precisão.
 */
function medirVitais(medidor: Meter): void {
  const histograma = (nome: string, unidade: string, descricao: string) =>
    medidor.createHistogram(nome, { unit: unidade, description: descricao });

  const emMilissegundos: Record<string, Histogram> = {
    LCP: histograma('web.vital.lcp', 'ms', 'Largest Contentful Paint.'),
    FCP: histograma('web.vital.fcp', 'ms', 'First Contentful Paint.'),
    TTFB: histograma('web.vital.ttfb', 'ms', 'Time To First Byte.'),
    INP: histograma('web.vital.inp', 'ms', 'Interaction to Next Paint.'),
  };
  const cls = histograma('web.vital.cls', '{score}', 'Cumulative Layout Shift.');

  const registrar = (metrica: Metric) => {
    const atributos: Attributes = { 'web.vital.avaliacao': metrica.rating };
    if (metrica.name === 'CLS') cls.record(metrica.value, atributos);
    else emMilissegundos[metrica.name]?.record(metrica.value, atributos);
  };

  onLCP(registrar);
  onFCP(registrar);
  onTTFB(registrar);
  onINP(registrar);
  onCLS(registrar);
}

/**
 * Erro de JavaScript no armazém não deixava rastro nenhum: o projeto não tinha ErrorHandler, e o
 * que estoura fora de um handler do Angular — promessa rejeitada, callback de evento — não passa
 * nem por ele. Estes dois ouvintes fecham o caminho de fuga.
 */
function ouvirErrosGlobais(): void {
  window.addEventListener('error', evento =>
    registrarErro(evento.error ?? evento.message, 'window.onerror'));

  window.addEventListener('unhandledrejection', evento =>
    registrarErro(evento.reason, 'promessa-rejeitada'));
}

/**
 * Registra um erro nos três sinais que fazem sentido: conta na métrica (quantos, de onde),
 * anexa ao span em curso quando há um (em qual requisição) e emite um log (o que dizia).
 *
 * Silenciosa com a telemetria desligada — é chamada do ErrorHandler do Angular, que existe
 * de qualquer jeito.
 */
export function registrarErro(erro: unknown, origem: string, extras: Attributes = {}): void {
  if (!ligada) return;

  const mensagem = erro instanceof Error ? erro.message : String(erro);
  const pilha = erro instanceof Error ? erro.stack : undefined;
  const atributos: Attributes = { 'erro.origem': origem, rota: rotaAtual, ...extras };

  contadorErros?.add(1, { 'erro.origem': origem });

  const emCurso = trace.getActiveSpan();
  if (emCurso) {
    if (erro instanceof Error) emCurso.recordException(erro);
    else emCurso.addEvent('exception', { 'exception.message': mensagem });
  }

  registrador?.emit({
    severityNumber: SeverityNumber.ERROR,
    severityText: 'ERROR',
    body: mensagem,
    // A pilha vai cortada: o log é para achar o ponto, não para guardar o bundle inteiro.
    attributes: { ...atributos, 'exception.stacktrace': pilha?.slice(0, 4000) },
  });
}

/** Informa a rota em vigor, para erro e log dizerem em qual tela aconteceram. */
export function definirRotaAtual(padrao: string): void {
  rotaAtual = padrao;
}

/** O tracer da aplicação, ou undefined com a telemetria desligada. */
export function tracerDaAplicacao(): Tracer | undefined {
  return tracer;
}

/**
 * Identifica a visita, para seguir uma sessão de conferência inteira em vez de spans avulsos.
 *
 * Vai em span e em log, nunca em métrica: como rótulo de métrica, cada visita viraria uma série
 * própria e a cardinalidade explodiria em uma semana.
 *
 * Não carrega quem é a pessoa. A matrícula do operador identificaria o conferente para fora da
 * máquina, e a instrumentação da API foi escrita para manter justamente esse campo dentro dela.
 */
function obterSessao(): string {
  try {
    const guardada = sessionStorage.getItem(CHAVE_SESSAO);
    if (guardada) return guardada;
    const nova = identificadorAleatorio();
    sessionStorage.setItem(CHAVE_SESSAO, nova);
    return nova;
  } catch {
    // sessionStorage barrado (modo restrito do navegador) não pode derrubar a aplicação.
    return identificadorAleatorio();
  }
}

/**
 * crypto.randomUUID só existe em contexto seguro. A captura de telas roda dentro da rede do
 * compose e abre http://web:3000, que não é seguro — ali a chamada seria undefined e o
 * bootstrap morreria antes de desenhar a primeira tela.
 */
function identificadorAleatorio(): string {
  if (typeof crypto?.randomUUID === 'function') return crypto.randomUUID();
  return Math.random().toString(36).slice(2) + Date.now().toString(36);
}

/**
 * Carimba a sessão em todo span. Processador em vez de atributo no recurso: no recurso, a sessão
 * entraria também nas métricas, que é onde ela faz estrago.
 */
class ProcessadorSessao implements SpanProcessor {
  constructor(private readonly atributos: Attributes) {}
  onStart(span: Span): void {
    span.setAttributes(this.atributos);
  }
  onEnd(_span: ReadableSpan): void {}
  forceFlush(): Promise<void> {
    return Promise.resolve();
  }
  shutdown(): Promise<void> {
    return Promise.resolve();
  }
}

/**
 * O lote pendente morre com a aba. Vale para os três sinais, e é pior nas métricas: o intervalo
 * é de 60 s, então uma visita curta não exportaria nada. visibilitychange é o gancho confiável
 * no celular, onde pagehide nem sempre dispara.
 */
function descarregarAoSair(provedores: { forceFlush(): Promise<void> }[]): void {
  const descarregar = () => {
    for (const provedor of provedores) provedor.forceFlush().catch(() => {});
  };
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') descarregar();
  });
  window.addEventListener('pagehide', descarregar);
}

function escaparRegex(texto: string): string {
  return texto.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
