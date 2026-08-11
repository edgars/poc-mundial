import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from './api';

/**
 * Story 1.7 / FR-54 — expiração por inatividade.
 * O legado faz isto com um Timer global (ShutTimer em conferencia.PRG) que encerra a aplicação
 * depois de horas parado; é o que o campo `timer1` da tela representa.
 *
 * Aqui: qualquer interação renova o relógio. Vencido, o usuário volta ao login com a razão dita,
 * e a conferência em que ele estava fica guardada para o retorno.
 */
@Injectable({ providedIn: 'root' })
export class Sessao {
  private api = inject(Api);
  private router = inject(Router);

  readonly expirou = signal(false);
  private ultimoUso = Date.now();
  private timer?: ReturnType<typeof setInterval>;

  /** Padrão de 8 horas, na ordem de grandeza do legado — horas, não minutos. */
  private get limiteMs() {
    const s = this.api.sessao();
    if (!s?.expiraEm) return 8 * 60 * 60 * 1000;
    return Math.max(60_000, new Date(s.expiraEm).getTime() - Date.now());
  }

  iniciar() {
    if (this.timer) return;
    ['keydown', 'pointerdown', 'wheel'].forEach(evento =>
      document.addEventListener(evento, () => { this.ultimoUso = Date.now(); }, { passive: true }));
    this.timer = setInterval(() => this.verificar(), 30_000);
  }

  private verificar() {
    if (!this.api.sessao()) return;
    if (Date.now() - this.ultimoUso < this.limiteMs) return;
    this.encerrar();
  }

  encerrar() {
    // Guarda onde a pessoa estava: ao reentrar, volta para a mesma conferência.
    const rota = this.router.url;
    if (rota.startsWith('/conferencia/')) sessionStorage.setItem('retomar', rota);
    this.api.sair();
    this.expirou.set(true);
    this.router.navigate(['/entrar']);
  }

  /** Para onde ir depois de entrar: a conferência interrompida, ou o painel. */
  destinoAposEntrar(): string {
    const rota = sessionStorage.getItem('retomar');
    sessionStorage.removeItem('retomar');
    return rota ?? '/docas';
  }
}
