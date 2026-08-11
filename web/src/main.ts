import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { iniciarTelemetria } from './app/api/telemetria';

// Antes do bootstrap: instrumentar depois já teria perdido o carregamento da página
// e as primeiras chamadas à API.
iniciarTelemetria();

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
