import { Component, inject, signal, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Api, ResumoDoca } from '../api/api';

@Component({
  selector: 'app-docas',
  styles: [`
    .grade{display:grid;grid-template-columns:repeat(4,1fr);gap:var(--s3);padding:var(--s4)}
    @media (max-width:1100px){.grade{grid-template-columns:repeat(2,1fr)}}
    @media (max-width:700px){.grade{grid-template-columns:1fr}}
    .cartao{background:var(--surface-raised);border:1px solid var(--border);border-radius:var(--r-md);
      padding:15px;min-height:215px;display:flex;flex-direction:column;cursor:pointer;
      transition:border-color var(--m-flash) var(--ease-out), box-shadow var(--m-flash) var(--ease-out);
      text-align:left}
    .cartao:hover{border-color:var(--border-strong)}
    .cartao.atrasada{border-color:var(--attention);box-shadow:0 0 0 3px var(--attention-wash)}
    .cartao:disabled{cursor:default;opacity:.75}
    .n{font-size:10px;letter-spacing:.16em;text-transform:uppercase;color:var(--text-muted);
      display:flex;gap:8px;align-items:center}
    .doc{margin-top:13px;font-size:19px;font-weight:600;letter-spacing:-.01em}
    .forn{color:var(--text-muted);font-size:12px;margin-top:3px;line-height:1.4}
    .prog{margin-top:auto;padding-top:14px}
    .trilho{height:6px;background:var(--surface-base);border-radius:9999px;overflow:hidden}
    .preen{height:100%;border-radius:9999px;background:var(--accept);
      transition:width var(--m-shift) var(--ease-out)}
    .preen.at{background:var(--attention)}
    .tempo{font-size:11px;color:var(--text-muted);margin-top:8px;display:flex;
      justify-content:space-between;font-family:var(--mono)}
    .tempo b{font-weight:400}
    .atrasada .tempo b{color:var(--reject);font-weight:600}
    .vazio{padding:60px;text-align:center;color:var(--text-muted)}
  `],
  template: `
    <div class="barra">
      <b>Docas</b><span>Recebimento</span>
      <span class="pt">{{ api.sessao()?.nome }} · atualiza sozinho</span>
      @if (api.pode('estoq','consultar')) {
        <button class="btn sec" style="padding:6px 12px;font-size:12px"
                (click)="ir('/codigos')">Códigos</button>
      }
      <button class="btn sec" style="padding:6px 12px;font-size:12px"
              (click)="ir('/consultas')">Consultas</button>
      <button class="btn sec" style="padding:6px 12px;font-size:12px"
              (click)="ir('/fluxo-correto')">Receber carga</button>
      @if (podeResetar()) {
        <button class="btn sec" style="padding:6px 12px;font-size:12px"
                (click)="resetar()" [disabled]="resetando()">
          {{ resetando() ? 'Resetando…' : 'Resetar demo' }}
        </button>
      }
      <button class="btn sec" style="padding:6px 12px;font-size:12px" (click)="sair()">Sair</button>
    </div>

    @if (docas().length === 0) {
      <div class="vazio">Nenhuma doca em operação.</div>
    } @else {
      <div class="grade">
        @for (d of docas(); track d.documento) {
          <button class="cartao" [class.atrasada]="atrasada(d)"
                  [disabled]="d.estado === 'fechada'" (click)="abrir(d)">
            <div class="n">
              Doca {{ d.doca }}
              <span class="pill" [class]="classe(d)">{{ rotulo(d) }}</span>
            </div>
            <div class="doc">{{ d.documento }}</div>
            <div class="forn">
              {{ d.fornecedor }}<br>{{ d.operador || '—' }}
            </div>
            <div class="prog">
              <div class="trilho">
                <div class="preen" [class.at]="d.temDivergencia"
                     [style.width.%]="d.itensTotal ? (d.itensLancados / d.itensTotal) * 100 : 0"></div>
              </div>
              <div class="tempo">
                <span>{{ d.itensLancados }} de {{ d.itensTotal }} itens</span>
                <b>{{ decorrido(d) }}</b>
              </div>
            </div>
          </button>
        }
      </div>
    }
  `,
})
export class Docas implements OnDestroy {
  api = inject(Api);
  private router = inject(Router);
  docas = signal<ResumoDoca[]>([]);
  private timer?: ReturnType<typeof setInterval>;
  resetando = signal(false);

  constructor() {
    if (!this.api.sessao() && !this.api.restaurar()) { this.router.navigate(['/entrar']); }
    this.carregar();
    // FR-47: o painel se atualiza sozinho
    this.timer = setInterval(() => this.carregar(), 5000);
  }

  ngOnDestroy() { clearInterval(this.timer); }

  async carregar() {
    try { this.docas.set(await this.api.docas()); } catch { /* mantém o último estado */ }
  }

  /** UX-DR6: a ordem já vem do servidor por tempo de doca aberta. */
  atrasada(d: ResumoDoca) {
    return d.estado === 'em conferência' && this.minutos(d) > 120;
  }

  private minutos(d: ResumoDoca) {
    if (!d.abertaDesde) return 0;
    return (Date.now() - new Date(d.abertaDesde).getTime()) / 60000;
  }

  decorrido(d: ResumoDoca) {
    if (!d.abertaDesde || d.estado === 'fechada') return '—';
    const m = Math.max(0, Math.floor(this.minutos(d)));
    return `${String(Math.floor(m / 60)).padStart(2, '0')}:${String(m % 60).padStart(2, '0')}`;
  }

  classe(d: ResumoDoca) {
    if (d.temDivergencia || this.atrasada(d)) return 'p-div';
    if (d.estado === 'em conferência') return 'p-ok';
    return 'p-agu';
  }

  rotulo(d: ResumoDoca) {
    if (this.atrasada(d)) return 'atrasada';
    if (d.temPendencia) return 'pendência';
    if (d.temDivergencia) return 'divergência';
    return d.estado;
  }

  abrir(d: ResumoDoca) {
    if (d.documento) this.router.navigate(['/conferencia', d.documento]);
  }

  ir(rota: string) { this.router.navigate([rota]); }

  /** Story 5.3 / FR-51: só existe com MODO_DEMO ligado, e só para quem pode alterar. */
  podeResetar() { return this.api.pode('conferencia', 'alterar'); }

  async resetar() {
    this.resetando.set(true);
    try { await this.api.resetarDemo(); await this.carregar(); }
    catch { /* sem MODO_DEMO o endpoint não existe */ }
    finally { this.resetando.set(false); }
  }

  sair() { this.api.sair(); this.router.navigate(['/entrar']); }
}
