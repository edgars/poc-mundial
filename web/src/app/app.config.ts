import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { routes } from './app.routes';
import { provedoresTelemetria } from './api/telemetria-angular';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    // UX-DR15b: transição contínua doca → conferência
    provideRouter(routes, withViewTransitions()),
    provideHttpClient(),
    // Erro do Angular e troca de tela viram sinal. Sem efeito com OTEL_WEB=false.
    ...provedoresTelemetria(),
  ],
};
