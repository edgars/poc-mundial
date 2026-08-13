import {
  EnvironmentProviders,
  ErrorHandler,
  Injectable,
  Provider,
  inject,
  provideAppInitializer,
} from '@angular/core';
import {
  ActivatedRouteSnapshot,
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
} from '@angular/router';
import { Span, SpanStatusCode } from '@opentelemetry/api';
import { definirRotaAtual, registrarErro, tracerDaAplicacao } from './telemetria';

/**
 * A ponte entre o Angular e o OpenTelemetry: o que o SDK sozinho não enxerga.
 *
 * São duas coisas, e as duas eram pontos cegos completos — o erro de JavaScript não deixava
 * rastro em lugar nenhum, e a troca de tela não existia como evento.
 */
export function provedoresTelemetria(): (Provider | EnvironmentProviders)[] {
  return [
    { provide: ErrorHandler, useClass: ManipuladorDeErros },
    // Antes da navegação inicial, senão a primeira troca de tela já teria passado.
    provideAppInitializer(() => {
      inject(RastreioRotas).iniciar();
    }),
  ];
}

/**
 * O ErrorHandler do Angular recebe tudo que estoura dentro do framework. Sem um próprio, o
 * padrão só escreve no console do navegador — que ninguém está lendo do outro lado do armazém.
 */
@Injectable()
export class ManipuladorDeErros implements ErrorHandler {
  handleError(erro: unknown): void {
    registrarErro(erro, 'angular');
    // O comportamento padrão continua: quem estiver com o console aberto não perde nada.
    console.error(erro);
  }
}

/**
 * Um span por navegação de rota.
 *
 * O DocumentLoadInstrumentation cobre só o primeiro carregamento. Daí em diante esta é uma SPA:
 * doca → conferência → fecho não gera navegação de documento nenhuma, e o tempo de trocar de
 * tela — que inclui baixar o chunk da rota, porque as rotas são todas loadComponent — não
 * aparecia em sinal algum.
 */
@Injectable({ providedIn: 'root' })
export class RastreioRotas {
  private readonly router = inject(Router);
  /** Por id de navegação: o Angular permite que uma nova comece antes de a anterior terminar. */
  private readonly emCurso = new Map<number, Span>();

  iniciar(): void {
    const tracer = tracerDaAplicacao();
    if (!tracer) return;

    this.router.events.subscribe(evento => {
      if (evento instanceof NavigationStart) {
        this.emCurso.set(
          evento.id,
          tracer.startSpan('navegacao', {
            attributes: {
              'rota.de': this.router.url,
              // 'imperative' (clique no app), 'popstate' (botão voltar) ou 'hashchange'.
              'navegacao.gatilho': evento.navigationTrigger ?? 'imperative',
            },
          }),
        );
      } else if (evento instanceof NavigationEnd) {
        this.encerrar(evento.id, 'concluida');
      } else if (evento instanceof NavigationCancel) {
        // Guarda que redirecionou, quase sempre a sessão expirada mandando para /entrar.
        this.encerrar(evento.id, 'cancelada');
      } else if (evento instanceof NavigationError) {
        this.encerrar(evento.id, 'erro', evento.error);
      }
    });
  }

  private encerrar(id: number, desfecho: string, erro?: unknown): void {
    const span = this.emCurso.get(id);
    if (!span) return;
    this.emCurso.delete(id);

    const padrao = padraoDaRota(this.router.routerState.snapshot.root);
    // O nome sai com o padrão da rota, não com a URL: /conferencia/:documento é uma linha na
    // tela do SigNoz; /conferencia/000123456 seriam milhares.
    span.updateName(`navegacao ${padrao}`);
    span.setAttribute('rota', padrao);
    span.setAttribute('rota.para', this.router.url);
    span.setAttribute('navegacao.desfecho', desfecho);

    if (erro !== undefined) {
      span.setStatus({ code: SpanStatusCode.ERROR });
      registrarErro(erro, 'navegacao');
    }

    span.end();
    if (desfecho === 'concluida') definirRotaAtual(padrao);
  }
}

/** Reconstrói o padrão declarado em app.routes.ts a partir da árvore de rotas ativa. */
function padraoDaRota(raiz: ActivatedRouteSnapshot): string {
  const partes: string[] = [];
  let no: ActivatedRouteSnapshot | null = raiz;
  while (no) {
    const caminho = no.routeConfig?.path;
    if (caminho) partes.push(caminho);
    no = no.firstChild;
  }
  return '/' + partes.join('/');
}
