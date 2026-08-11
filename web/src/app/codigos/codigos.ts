import { Component, inject, signal, ElementRef, viewChild, afterNextRender } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Api, Produto, Etiqueta } from '../api/api';

/**
 * Épico 3 · Stories 3.1 a 3.6 — cadastro dos três códigos de embalagem e prévia da etiqueta.
 * UX: o erro aparece NO CAMPO que o causou, nunca em resumo no topo, porque as regras são de
 * duplicidade cruzada entre os três slots: saber qual deles conflita é a informação.
 */
@Component({
  selector: 'app-codigos',
  imports: [FormsModule],
  styles: [`
    .cod{display:grid;grid-template-columns:1fr 330px;gap:var(--s4);padding:var(--s4)}
    @media (max-width:900px){.cod{grid-template-columns:1fr}}
    .bloco{background:var(--surface-raised);border:1px solid var(--border);
      border-radius:var(--r-md);padding:var(--s5)}
    .bloco + .bloco{margin-top:12px}
    .bloco h3{margin:0 0 14px;font-size:13px;font-weight:600;letter-spacing:.02em}
    .grade2{display:grid;grid-template-columns:1fr 1fr;gap:12px}
    .campo{margin-bottom:12px}
    input{width:100%;margin-top:5px;background:var(--surface-base);border:1px solid var(--border);
      border-radius:var(--r);padding:9px 11px;font-family:var(--mono);font-size:15px;color:var(--text)}
    input:focus{outline:0;border-color:var(--focus);box-shadow:0 0 0 3px var(--focus-glow)}
    input:disabled{color:var(--text-muted)}
    input.err{border-color:var(--reject);box-shadow:0 0 0 3px var(--reject-wash)}
    .msg-err{color:var(--reject);font-size:12px;margin-top:5px;line-height:1.4}
    .etiq{background:#fff;color:#000;border-radius:6px;padding:14px;margin-top:10px;
      font-family:var(--mono);text-align:center;animation:revela var(--m-route) var(--ease-out)}
    @keyframes revela{from{opacity:0;clip-path:inset(0 0 100% 0)}to{opacity:1;clip-path:inset(0)}}
    .barras{height:52px;margin:8px 0;background:repeating-linear-gradient(90deg,
      #000 0 2px,#fff 2px 4px,#000 4px 7px,#fff 7px 9px,#000 9px 10px,#fff 10px 14px)}
    .zpl{margin-top:10px;background:var(--surface-base);border:1px solid var(--border);
      border-radius:var(--r);padding:10px;font-family:var(--mono);font-size:11px;
      color:var(--text-muted);line-height:1.6;white-space:pre-wrap;overflow-x:auto}
    .acoes{display:flex;gap:10px;margin-top:14px}
    .aviso{background:var(--accept-wash);border:1px solid rgba(52,211,153,.4);color:var(--accept);
      border-radius:var(--r);padding:10px 12px;font-size:13px;margin-top:12px}
    .busca{display:flex;gap:10px;padding:var(--s4) var(--s4) 0}
    .busca input{margin-top:0;flex:1;max-width:260px}
    .dialogo{position:fixed;inset:0;background:rgba(10,14,16,.72);display:grid;place-items:center;z-index:50}
    .dcartao{background:var(--surface-raised);border:1px solid var(--border-strong);
      border-radius:var(--r-lg);padding:24px;width:420px}
    .dcartao p{white-space:pre-line;margin:0 0 20px;font-size:16px}
    .dacoes{display:flex;gap:10px;justify-content:flex-end}
  `],
  template: `
    <div class="barra">
      <b>Códigos de embalagem</b>
      <span>{{ produto()?.descricao || 'Informe o código do produto' }}</span>
      <span class="pt">{{ api.sessao()?.nome }}</span>
      <button class="btn sec" style="padding:6px 12px;font-size:12px" (click)="voltar()">Docas</button>
    </div>

    <div class="busca">
      <input #busca placeholder="Código do produto" [(ngModel)]="codigoBusca"
             (keyup.enter)="carregar()" inputmode="numeric">
      <button class="btn" (click)="carregar()">Abrir</button>
    </div>

    @if (erroGeral()) { <div class="busca"><div class="msg-err">{{ erroGeral() }}</div></div> }

    @if (produto(); as p) {
      <div class="cod">
        <div>
          <div class="bloco">
            <h3>Produto</h3>
            <div class="grade2">
              <div class="campo"><label class="rot">Código</label><input [value]="p.codigo" disabled></div>
              <div class="campo"><label class="rot">Embalagem</label>
                <input [value]="(p.embalagem || '') + ' c/ ' + (p.embalagemQtd || 0)" disabled></div>
            </div>
            <div class="campo"><label class="rot">Descrição</label><input [value]="p.descricao" disabled></div>
            <div class="campo"><label class="rot">EAN-13 da unidade</label>
              <input [value]="p.ean[0] || '—'" disabled></div>
          </div>

          <div class="bloco">
            <h3>Códigos DUN-14 · até três por produto</h3>
            @for (slot of [0,1,2]; track slot) {
              <div class="campo">
                <label class="rot" [for]="'dun'+slot">Barr Emb {{ slot + 1 }}</label>
                <input [id]="'dun'+slot" [(ngModel)]="dun[slot]" [class.err]="erroSlot() === slot"
                       maxlength="14" autocomplete="off" [disabled]="!podeAlterar">
                @if (erroSlot() === slot) { <div class="msg-err">{{ erroMensagem() }}</div> }
              </div>
            }
            <div class="acoes">
              <button class="btn" (click)="gravar()" [disabled]="!podeAlterar">Gravar</button>
              <button class="btn sec" (click)="carregar()">Descartar</button>
            </div>
            @if (!podeAlterar) {
              <div class="msg-err" style="margin-top:10px">
                Sua matrícula não tem permissão de alteração em estoq — somente leitura.
              </div>
            }
            @if (gravado()) { <div class="aviso">Códigos gravados.</div> }
          </div>
        </div>

        <div class="bloco">
          <h3>Prévia da etiqueta</h3>
          @if (etiqueta(); as e) {
            <div class="etiq">
              <div style="font-size:13px;font-weight:700">{{ e.descricao }}</div>
              <div class="barras"></div>
              <div style="font-size:11px">{{ e.codigoBarras }}</div>
              <div style="font-size:12px;margin-top:6px">
                {{ e.embalagem }} c/ {{ e.embalagemQtd }}
              </div>
            </div>
            <div class="zpl">{{ e.zpl }}</div>
          } @else {
            <div class="msg-err">Produto sem código de barras para imprimir.</div>
          }
        </div>
      </div>
    }

    @if (confirmacao(); as c) {
      <div class="dialogo" role="dialog" aria-modal="true">
        <div class="dcartao">
          <p>{{ c }}</p>
          <div class="dacoes">
            <button class="btn sec" (click)="confirmacao.set('')">Não</button>
            <button class="btn" (click)="gravar(true)">Sim</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class Codigos {
  api = inject(Api);
  private rota = inject(ActivatedRoute);
  private router = inject(Router);
  private busca = viewChild<ElementRef<HTMLInputElement>>('busca');

  codigoBusca = this.rota.snapshot.queryParamMap.get('codigo') ?? '';
  produto = signal<Produto | null>(null);
  etiqueta = signal<Etiqueta | null>(null);
  dun: (string | null)[] = ['', '', ''];
  erroSlot = signal<number | null>(null);
  erroMensagem = signal('');
  erroGeral = signal('');
  gravado = signal(false);
  confirmacao = signal('');

  get podeAlterar() { return this.api.pode('estoq', 'alterar'); }

  constructor() {
    if (!this.api.sessao() && !this.api.restaurar()) { this.router.navigate(['/entrar']); }
    afterNextRender(() => {
      if (this.codigoBusca) this.carregar(); else this.busca()?.nativeElement.focus();
    });
  }

  async carregar() {
    this.limparErros();
    this.gravado.set(false);
    try {
      const p = await this.api.produto(this.codigoBusca.trim());
      this.produto.set(p);
      this.dun = [p.dun[0] ?? '', p.dun[1] ?? '', p.dun[2] ?? ''];
      await this.carregarEtiqueta();
    } catch {
      this.produto.set(null);
      this.etiqueta.set(null);
      this.erroGeral.set('Código não cadastrado!');   // RK-e84d750f340a
    }
  }

  private async carregarEtiqueta() {
    try { this.etiqueta.set(await this.api.etiqueta(this.produto()!.codigo)); }
    catch { this.etiqueta.set(null); }
  }

  async gravar(confirmado = false) {
    this.limparErros();
    this.confirmacao.set('');
    try {
      await this.api.gravarCodigos(this.produto()!.codigo, this.dun, confirmado);
      this.gravado.set(true);
      await this.carregar();
    } catch (e: any) {
      const detalhe = e?.error?.detail ?? 'Não foi possível gravar.';
      if (e?.error?.tipo === 'ExigeConfirmacao') { this.confirmacao.set(detalhe); return; }
      // O erro pertence ao campo que o causou — as regras são de duplicidade cruzada.
      this.erroSlot.set(this.slotDoErro(e?.error?.ruleKey));
      this.erroMensagem.set(detalhe);
      if (this.erroSlot() === null) this.erroGeral.set(detalhe);
    }
  }

  /** Cada slot tem sua própria chave, tanto para duplicidade interna quanto para outro produto. */
  private slotDoErro(chave?: string): number | null {
    const mapa: Record<string, number> = {
      'RK-99e9bfdcea75': 0, 'RK-a0bb1eeee55d': 0, 'RK-2976e3756f6d': 0,
      'RK-4ca8df36a760': 1, 'RK-f9e0b12a76af': 1, 'RK-ab467d52fa1f': 1,
      'RK-ab62193a2b2d': 2, 'RK-41493150036e': 2, 'RK-f3bda1fa3b77': 2,
    };
    return chave && chave in mapa ? mapa[chave] : null;
  }

  private limparErros() {
    this.erroSlot.set(null); this.erroMensagem.set(''); this.erroGeral.set('');
  }

  voltar() { this.router.navigate(['/docas']); }
}
