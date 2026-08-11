import { Injectable, signal } from '@angular/core';

/**
 * UX-DR3 / NFR-15: dois sons distintos em altura, não só em duração.
 * Toca junto com a atualização visual, nunca antes. Silenciável, preferência persistida.
 */
@Injectable({ providedIn: 'root' })
export class SinalSonoro {
  readonly mudo = signal(localStorage.getItem('mudo') === 'true');
  private ctx?: AudioContext;

  alternarMudo() {
    this.mudo.update(v => !v);
    localStorage.setItem('mudo', String(this.mudo()));
  }

  aceite() { this.tocar(880, 0.07); }
  recusa() { this.tocar(220, 0.22); }

  private tocar(hz: number, duracao: number) {
    if (this.mudo()) return;
    try {
      this.ctx ??= new AudioContext();
      const osc = this.ctx.createOscillator();
      const ganho = this.ctx.createGain();
      osc.frequency.value = hz;
      osc.type = 'square';
      ganho.gain.setValueAtTime(0.06, this.ctx.currentTime);
      ganho.gain.exponentialRampToValueAtTime(0.0001, this.ctx.currentTime + duracao);
      osc.connect(ganho).connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + duracao);
    } catch { /* áudio bloqueado até a primeira interação; silencioso por design */ }
  }
}
