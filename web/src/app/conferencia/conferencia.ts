import { Component, inject, signal, computed, ElementRef, viewChild, afterNextRender, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Api, DocumentoConf, Leitura } from '../api/api';
import { SinalSonoro } from '../api/som';

type Confirmacao = { chave?: string; mensagem: string; acao: () => void } | null;

@Component({
  selector: 'app-conferencia',
  imports: [FormsModule],
  styles: [`
    .conf{display:grid;grid-template-columns:1fr 330px;gap:var(--s4);padding:var(--s4)}
    @media (max-width:900px){.conf{grid-template-columns:1fr}}
    .ctx{display:flex;gap:10px;margin-bottom:12px;flex-wrap:wrap}
    .chip{background:var(--surface-raised);border:1px solid var(--border);border-radius:var(--r);
      padding:9px 13px;flex:1;min-width:150px}
    .chip b{display:block;font-size:17px;font-weight:600;margin-top:3px;letter-spacing:-.01em}
    .leitura{background:var(--surface-raised);border:1px solid var(--focus);border-radius:var(--r-md);
      padding:14px 16px;box-shadow:0 0 0 3px var(--focus-glow)}
    .leitura input{width:100%;margin-top:7px;background:transparent;border:0;color:var(--text);
      font-family:var(--mono);font-size:29px;letter-spacing:.05em}
    .leitura input:focus{outline:0}
    .lista{margin-top:12px;background:var(--surface-raised);border:1px solid var(--border);
      border-radius:var(--r-md);overflow:hidden}
    table{width:100%;border-collapse:collapse;font-size:14px}
    th{text-align:left;font-weight:500;font-size:10px;letter-spacing:.14em;text-transform:uppercase;
      color:var(--text-muted);padding:10px var(--s4);background:var(--surface-overlay)}
    td{padding:11px var(--s4);border-top:1px solid var(--border);font-variant-numeric:tabular-nums}
    td.num{text-align:right;font-family:var(--mono)}
    td.cod{font-family:var(--mono);font-size:12px;color:var(--text-muted)}
    tr.div{background:rgba(251,191,36,.05)}
    tr.novo{animation:entra var(--m-shift) var(--ease-out)}
    @keyframes entra{from{opacity:0;transform:translateY(-6px)}to{opacity:1;transform:none}}
    .focal{background:var(--surface-raised);border:1px solid var(--border);border-radius:var(--r-md);
      padding:var(--s5);display:flex;flex-direction:column;min-height:430px}
    .selo{border-radius:var(--r);padding:9px;text-align:center;font-size:12px;letter-spacing:.14em;
      text-transform:uppercase;font-weight:600;border:1px solid}
    .selo.ok{background:var(--accept-wash);color:var(--accept);border-color:rgba(52,211,153,.4)}
    .selo.err{background:var(--reject-wash);color:var(--reject);border-color:rgba(248,113,113,.4)}
    .selo.esp{background:rgba(143,163,176,.10);color:var(--text-muted);border-color:var(--border)}
    .selo.flash{animation:pulso var(--m-flash) var(--ease-out) 2}
    @keyframes pulso{50%{transform:scale(1.03)}}
    .desc{font-size:23px;line-height:1.22;margin:18px 0 5px;font-weight:600;letter-spacing:-.02em}
    .emb{color:var(--text-muted);font-size:13px;font-family:var(--mono)}
    .qtd{margin-top:var(--s6);padding-top:var(--s5);border-top:1px solid var(--border)}
    .qtd b{display:block;font-family:var(--mono);font-size:56px;line-height:1;font-weight:600;
      margin-top:5px;letter-spacing:-.03em}
    .qtd input{width:100%;margin-top:8px;background:var(--surface-base);border:1px solid var(--border);
      border-radius:var(--r);padding:10px 12px;color:var(--text);font-family:var(--mono);font-size:24px}
    .qtd input:focus{outline:0;border-color:var(--focus);box-shadow:0 0 0 3px var(--focus-glow)}
    .hist{margin-top:auto;padding-top:18px;font-size:12px;color:var(--text-muted);
      line-height:1.9;font-family:var(--mono)}
    .acoes{display:flex;gap:8px;padding:0 var(--s4) var(--s4)}
    .dialogo{position:fixed;inset:0;background:rgba(10,14,16,.72);display:grid;place-items:center;z-index:50}
    .dcartao{background:var(--surface-raised);border:1px solid var(--border-strong);
      border-radius:var(--r-lg);padding:24px;width:420px}
    .dcartao p{white-space:pre-line;margin:0 0 20px;font-size:16px}
    .dacoes{display:flex;gap:10px;justify-content:flex-end}
    .fechado{background:var(--attention-wash);border-bottom:1px solid var(--attention);
      color:var(--attention);padding:10px var(--s4);font-size:13px;font-weight:600}
    .celebra{animation:celebra var(--m-celebrate) var(--ease-out)}
    @keyframes celebra{0%{box-shadow:0 0 0 0 var(--accept-wash)}
      60%{box-shadow:0 0 0 22px rgba(52,211,153,0)}100%{box-shadow:0 0 0 0 rgba(52,211,153,0)}}
    .codigos{margin-top:12px;background:var(--surface-raised);border:1px solid var(--border);
      border-radius:var(--r-md);padding:12px}
    .codigos h4{margin:0 0 8px;font-size:10px;letter-spacing:.14em;text-transform:uppercase;
      color:var(--text-muted);font-weight:500}
    .codigos button{display:block;width:100%;text-align:left;background:var(--surface-base);
      border:1px solid var(--border);border-radius:var(--r-sm);padding:6px 9px;margin-bottom:5px;
      color:var(--text);font-family:var(--mono);font-size:11px;cursor:pointer}
    .codigos small{color:var(--text-muted);font-family:var(--sans)}
  `],
  template: `
    <div class="barra">
      <b>Conferência</b>
      <span>Doca {{ doc()?.doca }}</span>
      <span class="pt">
        {{ api.sessao()?.nome }} ·
        <button class="btn sec" style="padding:3px 9px;font-size:11px" (click)="som.alternarMudo()">
          {{ som.mudo() ? 'som off' : 'som on' }}
        </button>
      </span>
      <button class="btn sec" style="padding:6px 12px;font-size:12px" (click)="voltar()">Docas</button>
    </div>

    @if (doc()?.fechado) {
      <div class="fechado">
        Este Documento já foi conferido! Fechado por {{ doc()?.matrFec }} — somente leitura.
      </div>
    }

    <div class="conf" [class.celebra]="celebrando()">
      <div>
        <div class="ctx">
          <div class="chip"><span class="rot">Documento</span><b>{{ doc()?.documento }}</b></div>
          <div class="chip"><span class="rot">Fornecedor</span><b>{{ doc()?.fornecedor }}</b></div>
          <div class="chip"><span class="rot">Itens</span>
            <b>{{ doc()?.itensLancados }} / {{ doc()?.itens?.length }}</b></div>
        </div>

        @if (!doc()?.fechado) {
          <div class="leitura">
            <label class="rot" for="leitura">Bipe o código</label>
            <input id="leitura" #campo name="codigo" [(ngModel)]="codigo" autocomplete="off"
                   (keyup.enter)="bipar()" placeholder="—">
          </div>
        }

        <div class="lista">
          <table>
            <thead><tr>
              <th>Cód</th><th>Produto</th><th class="num">Nota</th><th class="num">Recebido</th><th></th>
            </tr></thead>
            <tbody>
              @for (i of doc()?.itens ?? []; track i.codigo) {
                <tr [class.div]="i.temDivergencia" [class.novo]="i.codigo === ultimoLancado()">
                  <td class="cod">{{ i.codigo }}</td>
                  <td [style.color]="i.qtdRec ? null : 'var(--text-muted)'">{{ i.descricao }}</td>
                  <td class="num">{{ i.qtdNf }}</td>
                  <td class="num">{{ i.qtdRec || '—' }}</td>
                  <td>
                    @if (i.pendencia) { <span class="pill p-err">pendente</span> }
                    @else if (i.temDivergencia) { <span class="pill p-div">{{ i.divergencia }}</span> }
                    @else if (i.qtdRec > 0) { <span class="pill p-ok">ok</span> }
                    @else { <span class="pill p-agu">aguarda</span> }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (!doc()?.fechado) {
          <div class="acoes" style="padding-left:0;padding-right:0">
            <button class="btn" (click)="finalizar()">Finalizar conferência (F2)</button>
          </div>
        }
      </div>

      <!-- UX-DR1 / NFR-14: ponto único de verdade. Posição e ordem nunca mudam. -->
      <div class="focal">
        <div class="selo" [class.ok]="leitura()?.estado === 'aceito' || leitura()?.estado === 'confirmar'"
             [class.err]="leitura()?.estado === 'recusado' || leitura()?.estado === 'ambiguo'"
             [class.esp]="!leitura()" [class.flash]="flash()">
          {{ rotuloSelo() }}
        </div>

        <div class="desc">{{ leitura()?.item?.descricao ?? (leitura() ? '' : 'Bipe o primeiro item') }}</div>
        <div class="emb">
          @if (leitura()?.item) {
            {{ leitura()?.item?.embalagem }} · DUN-14 {{ leitura()?.item?.dun14 }}
          } @else if (leitura()?.mensagem) {
            {{ leitura()?.mensagem }}
          }
        </div>

        @if (leitura()?.candidatos?.length) {
          <div class="emb" style="margin-top:10px;line-height:1.9">
            @for (c of leitura()?.candidatos ?? []; track c) { {{ c }}<br> }
          </div>
        }

        @if (leitura()?.item && !doc()?.fechado) {
          <div class="qtd">
            <span class="rot">Quantidade recebida — nota diz {{ leitura()?.item?.qtdNf }}</span>
            <input #quantidade type="number" [(ngModel)]="qtd" (keyup.enter)="lancar()" inputmode="numeric">
          </div>
        } @else if (leitura()?.item) {
          <div class="qtd"><span class="rot">Quantidade recebida</span>
            <b>{{ leitura()?.item?.qtdRec }}</b></div>
        }

        <div class="hist">
          @if (historico().length) { Anteriores<br> }
          @for (h of historico(); track h) { {{ h }}<br> }
        </div>
      </div>
    </div>

    @if (codigosDemo().length && !doc()?.fechado) {
      <div class="conf" style="padding-top:0">
        <div class="codigos">
          <h4>Códigos à mão · demonstração</h4>
          @for (c of codigosDemo(); track c.codigo) {
            <button (click)="usarCodigo(c.codigo)">{{ c.codigo }} <small>— {{ c.efeito }}</small></button>
          }
        </div>
      </div>
    }

    @if (confirmacao(); as c) {
      <div class="dialogo" role="dialog" aria-modal="true">
        <div class="dcartao">
          <p>{{ c.mensagem }}</p>
          <div class="dacoes">
            <button #botaoSeguro class="btn sec" (click)="confirmacao.set(null); focar()">Não</button>
            <button class="btn" (click)="c.acao()">Sim</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class Conferencia {
  api = inject(Api);
  som = inject(SinalSonoro);
  private rota = inject(ActivatedRoute);
  private router = inject(Router);
  private campo = viewChild<ElementRef<HTMLInputElement>>('campo');
  private quantidade = viewChild<ElementRef<HTMLInputElement>>('quantidade');

  documento = this.rota.snapshot.paramMap.get('documento')!;
  doc = signal<DocumentoConf | null>(null);
  leitura = signal<Leitura | null>(null);
  confirmacao = signal<Confirmacao>(null);
  codigosDemo = signal<{codigo:string;efeito:string;tipo:string}[]>([]);
  historico = signal<string[]>([]);
  ultimoLancado = signal('');
  flash = signal(false);
  celebrando = signal(false);
  codigo = '';
  qtd: number | null = null;

  constructor() {
    if (!this.api.sessao() && !this.api.restaurar()) { this.router.navigate(['/entrar']); }
    this.carregar();
    this.api.codigosDemo().then(c => this.codigosDemo.set(c)).catch(() => {});
    afterNextRender(() => this.focar());
  }

  /** NFR-2 / UX-DR2: o campo de leitura recupera o foco a qualquer tecla imprimível. */
  @HostListener('document:keydown', ['$event'])
  teclado(e: KeyboardEvent) {
    if (e.key === 'F2') { e.preventDefault(); this.finalizar(); return; }
    if (this.confirmacao()) {
      if (e.key === 'Escape') { this.confirmacao.set(null); this.focar(); }
      return;
    }
    const alvo = e.target as HTMLElement;
    const digitando = alvo?.tagName === 'INPUT';
    if (!digitando && e.key.length === 1) this.focar();
  }

  focar() { queueMicrotask(() => this.campo()?.nativeElement.focus()); }

  private async carregar() {
    try { this.doc.set(await this.api.documento(this.documento)); }
    catch { this.router.navigate(['/docas']); }
  }

  async bipar() {
    const valor = this.codigo.trim();
    if (!valor) return;
    this.codigo = '';
    const r = await this.api.ler(this.documento, valor);
    this.leitura.set(r);
    this.piscar();
    // NFR-15: som junto com o visual, nunca antes
    if (r.estado === 'aceito' || r.estado === 'confirmar') this.som.aceite(); else this.som.recusa();

    if (r.estado === 'aceito' || r.estado === 'confirmar') {
      this.qtd = r.item?.qtdNf ?? null;
      queueMicrotask(() => this.quantidade()?.nativeElement.select());
    } else {
      this.focar();
    }
  }

  private piscar() {
    this.flash.set(true);
    setTimeout(() => this.flash.set(false), 260);
  }

  async lancar(confirmado = false) {
    const item = this.leitura()?.item;
    if (!item || this.qtd === null) return;
    try {
      const atualizado = await this.api.lancar(
        this.documento, item.codigo, Number(this.qtd),
        this.api.sessao()!.matricula, confirmado);
      this.doc.set(atualizado);
      this.ultimoLancado.set(item.codigo);
      this.historico.update(h => [`${item.codigo} · ${this.qtd} un`, ...h].slice(0, 2));
      this.leitura.set(null);
      this.qtd = null;
      this.focar();
    } catch (e: any) {
      // AD-6: confirmação é decisão do operador, chega como problem+json
      const detalhe = e?.error?.detail;
      if (e?.error?.tipo === 'ExigeConfirmacao' && detalhe) {
        this.confirmacao.set({
          chave: e.error.ruleKey, mensagem: detalhe,
          acao: () => { this.confirmacao.set(null); this.lancar(true); },
        });
      }
    }
  }

  async finalizar(confirmado = false) {
    if (this.doc()?.fechado) return;
    try {
      const atualizado = await this.api.fechar(this.documento, this.api.sessao()!.matricula, confirmado);
      this.doc.set(atualizado);
      // UX-DR15e: a sequência de conclusão, o único momento que comporta algo mais elaborado
      this.celebrando.set(true);
      this.som.aceite();
      setTimeout(() => this.celebrando.set(false), 700);
    } catch (e: any) {
      const detalhe = e?.error?.detail;
      if (e?.error?.tipo === 'ExigeConfirmacao' && detalhe) {
        this.confirmacao.set({
          chave: e.error.ruleKey, mensagem: detalhe,
          acao: () => { this.confirmacao.set(null); this.finalizar(true); },
        });
      }
    }
  }

  usarCodigo(c: string) { this.codigo = c; this.bipar(); }

  rotuloSelo() {
    const l = this.leitura();
    if (!l) return 'Aguardando leitura';
    return { aceito: 'Código aceito', confirmar: 'Código aceito',
             recusado: 'Leitura recusada', ambiguo: 'Leitura ambígua' }[l.estado];
  }

  voltar() { this.router.navigate(['/docas']); }
}
