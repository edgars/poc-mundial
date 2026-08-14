import { Component, inject, signal, computed, ElementRef, viewChild, afterNextRender, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api, DocumentoConf, Leitura, exibirInstante } from '../api/api';
import { SinalSonoro } from '../api/som';

/**
 * Uma nota dentro da sessão de recebimento da doca.
 *
 * `doc` é o que veio do banco; `volumes`, `transportadora` e `observacao` são o que o operador
 * preenche no passo 2. Os três últimos ainda não têm onde ser gravados — ver o aviso na tela e
 * a nota de pendência no fim deste arquivo.
 */
type NotaSessao = {
  numero: string;
  doc: DocumentoConf;
  volumes: number | null;
  transportadora: string;
  observacao: string;
};

type Confirmacao = { chave?: string; mensagem: string; acao: () => void } | null;

/**
 * O fluxo de entrada em três telas, como a operação da doca realmente acontece:
 *
 *   1. doca + notas   uma tela só: informa a doca e bipa/digita quantas notas chegaram
 *   2. preenchimento  os dados que vieram do banco, mais o que só o operador sabe
 *   3. baixa          conferência item a item e fechamento, nota por nota
 *
 * A diferença para /conferencia é o escopo: lá a unidade de trabalho é UMA nota, aberta pelo
 * painel de docas. Aqui a unidade é a CARGA — o caminhão encostou com várias notas, e o
 * operador as junta antes de começar a bipar. É a diferença entre conferir e receber.
 */
@Component({
  selector: 'app-fluxo-correto',
  imports: [FormsModule],
  styles: [`
    .passos{display:flex;gap:var(--s2);padding:var(--s4) var(--s4) 0;align-items:center}
    .pas{display:flex;align-items:center;gap:8px;padding:7px 13px;border-radius:9999px;
      border:1px solid var(--border);font-size:12px;color:var(--text-muted);background:var(--surface-raised)}
    .pas.at{border-color:var(--focus);color:var(--text);box-shadow:0 0 0 3px var(--focus-glow)}
    .pas.ok{border-color:rgba(52,211,153,.4);color:var(--accept)}
    .pas i{font-style:normal;font-family:var(--mono);font-size:11px;opacity:.7}
    .traco{flex:none;width:18px;height:1px;background:var(--border)}
    .tela{padding:var(--s4);display:grid;gap:var(--s4)}
    .cx{background:var(--surface-raised);border:1px solid var(--border);border-radius:var(--r-md);padding:var(--s5)}
    .cx.foco{border-color:var(--focus);box-shadow:0 0 0 3px var(--focus-glow)}
    .duo{display:grid;grid-template-columns:200px 1fr;gap:var(--s4)}
    @media (max-width:800px){.duo{grid-template-columns:1fr}}
    .entrada{width:100%;margin-top:7px;background:transparent;border:0;color:var(--text);
      font-family:var(--mono);font-size:29px;letter-spacing:.05em}
    .entrada:focus{outline:0}
    .campo{width:100%;margin-top:6px;background:var(--surface-base);border:1px solid var(--border);
      border-radius:var(--r);padding:9px 11px;color:var(--text);font-family:var(--mono);font-size:15px}
    .campo:focus{outline:0;border-color:var(--focus);box-shadow:0 0 0 3px var(--focus-glow)}
    table{width:100%;border-collapse:collapse;font-size:14px}
    th{text-align:left;font-weight:500;font-size:10px;letter-spacing:.14em;text-transform:uppercase;
      color:var(--text-muted);padding:10px var(--s4);background:var(--surface-overlay)}
    td{padding:11px var(--s4);border-top:1px solid var(--border);font-variant-numeric:tabular-nums}
    td.num{text-align:right;font-family:var(--mono)}
    td.cod{font-family:var(--mono);font-size:12px;color:var(--text-muted)}
    tr.div{background:rgba(251,191,36,.05)}
    tr.atual{background:var(--surface-overlay)}
    .lista{background:var(--surface-raised);border:1px solid var(--border);border-radius:var(--r-md);overflow:hidden}
    .erro{background:var(--reject-wash);border:1px solid rgba(248,113,113,.4);color:var(--reject);
      border-radius:var(--r);padding:10px 13px;font-size:14px;white-space:pre-line}
    .aviso{background:var(--attention-wash);border:1px solid var(--attention);color:var(--attention);
      border-radius:var(--r);padding:9px 12px;font-size:13px}
    .rodape{display:flex;gap:10px;align-items:center;padding:0 var(--s4) var(--s4)}
    .rodape .esp{margin-left:auto;color:var(--text-muted);font-size:12px;font-family:var(--mono)}
    .conf{display:grid;grid-template-columns:1fr 330px;gap:var(--s4)}
    @media (max-width:900px){.conf{grid-template-columns:1fr}}
    .focal{background:var(--surface-raised);border:1px solid var(--border);border-radius:var(--r-md);
      padding:var(--s5);display:flex;flex-direction:column;min-height:400px}
    .selo{border-radius:var(--r);padding:9px;text-align:center;font-size:12px;letter-spacing:.14em;
      text-transform:uppercase;font-weight:600;border:1px solid}
    .selo.ok{background:var(--accept-wash);color:var(--accept);border-color:rgba(52,211,153,.4)}
    .selo.err{background:var(--reject-wash);color:var(--reject);border-color:rgba(248,113,113,.4)}
    .selo.esp{background:rgba(143,163,176,.10);color:var(--text-muted);border-color:var(--border)}
    .selo.flash{animation:pulso var(--m-flash) var(--ease-out) 2}
    @keyframes pulso{50%{transform:scale(1.03)}}
    .desc{font-size:22px;line-height:1.22;margin:16px 0 5px;font-weight:600;letter-spacing:-.02em}
    .emb{color:var(--text-muted);font-size:13px;font-family:var(--mono)}
    .qtd{margin-top:var(--s5);padding-top:var(--s5);border-top:1px solid var(--border)}
    .qtd input{width:100%;margin-top:8px;background:var(--surface-base);border:1px solid var(--border);
      border-radius:var(--r);padding:10px 12px;color:var(--text);font-family:var(--mono);font-size:24px}
    .qtd input:focus{outline:0;border-color:var(--focus);box-shadow:0 0 0 3px var(--focus-glow)}
    .hist{margin-top:auto;padding-top:16px;font-size:12px;color:var(--text-muted);
      line-height:1.9;font-family:var(--mono)}
    .trilho{height:6px;background:var(--surface-base);border-radius:9999px;overflow:hidden;margin-top:8px}
    .preen{height:100%;border-radius:9999px;background:var(--accept);transition:width var(--m-shift) var(--ease-out)}
    .preen.at{background:var(--attention)}
    .abas{display:flex;gap:6px;flex-wrap:wrap;margin-bottom:var(--s3)}
    .aba{background:var(--surface-raised);border:1px solid var(--border);border-radius:var(--r);
      padding:8px 12px;color:var(--text-muted);font-size:12px;cursor:pointer;text-align:left}
    .aba.at{border-color:var(--focus);color:var(--text)}
    .aba.ok{border-color:rgba(52,211,153,.4);color:var(--accept)}
    .aba b{display:block;font-family:var(--mono);font-size:13px}
    .dialogo{position:fixed;inset:0;background:rgba(10,14,16,.72);display:grid;place-items:center;z-index:50}
    .dcartao{background:var(--surface-raised);border:1px solid var(--border-strong);
      border-radius:var(--r-lg);padding:24px;width:420px}
    .dcartao p{white-space:pre-line;margin:0 0 20px;font-size:16px}
    .dacoes{display:flex;gap:10px;justify-content:flex-end}
    .fim{text-align:center;padding:50px var(--s4)}
    .fim h2{font-size:26px;margin:0 0 6px;letter-spacing:-.02em}
  `],
  template: `
    <div class="barra">
      <b>Recebimento</b>
      <span>Fluxo por carga</span>
      <span class="pt">
        {{ api.sessao()?.nome }} ·
        <button class="btn sec" style="padding:3px 9px;font-size:11px" (click)="som.alternarMudo()">
          {{ som.mudo() ? 'som off' : 'som on' }}
        </button>
      </span>
      <button class="btn sec" style="padding:6px 12px;font-size:12px" (click)="sairDoFluxo()">Docas</button>
    </div>

    <div class="passos">
      <span class="pas" [class.at]="passo() === 1" [class.ok]="passo() > 1"><i>1</i> Doca e notas</span>
      <span class="traco"></span>
      <span class="pas" [class.at]="passo() === 2" [class.ok]="passo() > 2"><i>2</i> Dados das notas</span>
      <span class="traco"></span>
      <span class="pas" [class.at]="passo() === 3" [class.ok]="passo() > 3"><i>3</i> Baixa</span>
    </div>

    <!-- ───────────────────────── passo 1 · doca e notas ───────────────────────── -->
    @if (passo() === 1) {
      <div class="tela">
        <div class="duo">
          <div class="cx">
            <label class="rot" for="doca">Doca</label>
            <input id="doca" #campoDoca class="entrada" type="number" inputmode="numeric"
                   name="doca" [(ngModel)]="doca" placeholder="—" (keyup.enter)="focarNota()">
          </div>

          <div class="cx foco">
            <label class="rot" for="nota">Bipe ou digite a nota</label>
            <!-- Nunca desabilitado: com a API pendurada, campo desabilitado é doca parada sem
                 o operador entender por quê. Quem impede a leitura dupla é a guarda em
                 adicionarNota(), e ela sempre se solta pelo prazo máximo. -->
            <input id="nota" #campoNota class="entrada" name="nota" autocomplete="off"
                   [(ngModel)]="numeroNota" placeholder="—" (keyup.enter)="adicionarNota()">
            <div class="emb" style="margin-top:6px">
              {{ buscando() ? 'buscando no banco…' : 'cada leitura busca a nota no banco e junta à carga' }}
            </div>
          </div>
        </div>

        @if (erro()) { <div class="erro">{{ erro() }}</div> }

        @if (notas().length) {
          <div class="lista">
            <table>
              <thead><tr>
                <th>Nota</th><th>Fornecedor</th><th class="num">Itens</th>
                <th class="num">Qtd nota</th><th>Situação</th><th></th>
              </tr></thead>
              <tbody>
                @for (n of notas(); track n.numero) {
                  <tr>
                    <td class="cod">{{ n.doc.documento }}</td>
                    <td>{{ n.doc.fornecedor }}</td>
                    <td class="num">{{ n.doc.itens.length }}</td>
                    <td class="num">{{ totalDaNota(n) }}</td>
                    <td>
                      @if (n.doc.fechado) { <span class="pill p-err">já conferida</span> }
                      @else if (docaDivergente(n)) { <span class="pill p-div">consta doca {{ n.doc.doca }}</span> }
                      @else { <span class="pill p-ok">pronta</span> }
                    </td>
                    <td style="text-align:right">
                      <button class="btn sec" style="padding:4px 10px;font-size:11px"
                              (click)="removerNota(n)">tirar</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="emb" style="padding:0 var(--s4)">Nenhuma nota na carga ainda.</div>
        }
      </div>

      <div class="rodape">
        <button class="btn" [disabled]="!podeAvancarDoPasso1()" (click)="irPara(2)">
          Continuar (F2)
        </button>
        <span class="esp">{{ notas().length }} nota(s) · doca {{ doca || '—' }}</span>
      </div>
    }

    <!-- ──────────────────── passo 2 · preenchimento das notas ──────────────────── -->
    @if (passo() === 2) {
      <div class="tela">
        <div class="aviso">
          Volumes, transportadora e observação ainda não têm coluna no banco — hoje ficam só nesta
          sessão. Ver a pendência de backend no fim do arquivo desta página.
        </div>

        @for (n of notas(); track n.numero) {
          <div class="cx">
            <div style="display:flex;gap:var(--s4);flex-wrap:wrap;align-items:baseline">
              <div><span class="rot">Nota</span><b style="font-size:19px">{{ n.doc.documento }}</b></div>
              <div><span class="rot">Fornecedor</span><b>{{ n.doc.fornecedor }}</b></div>
              <div><span class="rot">Itens</span><b>{{ n.doc.itens.length }}</b></div>
              <div><span class="rot">Qtd na nota</span><b>{{ totalDaNota(n) }}</b></div>
              <div><span class="rot">Doca</span><b>{{ doca }}</b></div>
            </div>

            <div style="display:grid;grid-template-columns:150px 1fr 2fr;gap:var(--s4);margin-top:var(--s5)">
              <div>
                <label class="rot">Volumes recebidos</label>
                <input class="campo" type="number" inputmode="numeric" [(ngModel)]="n.volumes"
                       [name]="'vol' + n.numero" placeholder="—">
              </div>
              <div>
                <label class="rot">Transportadora</label>
                <input class="campo" [(ngModel)]="n.transportadora" [name]="'tra' + n.numero" placeholder="—">
              </div>
              <div>
                <label class="rot">Observação da chegada</label>
                <input class="campo" [(ngModel)]="n.observacao" [name]="'obs' + n.numero" placeholder="—">
              </div>
            </div>
          </div>
        }
      </div>

      <div class="rodape">
        <button class="btn sec" (click)="irPara(1)">Voltar</button>
        <button class="btn" [disabled]="!podeAvancarDoPasso2()" (click)="irPara(3)">
          Iniciar baixa (F2)
        </button>
        <span class="esp">
          {{ preenchidas() }} de {{ notas().length }} preenchidas
        </span>
      </div>
    }

    <!-- ───────────────────────────── passo 3 · baixa ───────────────────────────── -->
    @if (passo() === 3 && notaAtual(); as n) {
      <div class="tela">
        <div class="abas">
          @for (x of notas(); track x.numero; let i = $index) {
            <button class="aba" [class.at]="i === indice()" [class.ok]="x.doc.fechado"
                    (click)="irParaNota(i)">
              nota {{ i + 1 }}
              <b>{{ x.doc.documento }}</b>
              {{ x.doc.itensLancados }}/{{ x.doc.itens.length }}
            </button>
          }
        </div>

        @if (conflito()) {
          <div class="erro">
            {{ conflito() }}
            <button class="btn sec" style="padding:3px 10px;font-size:12px;margin-left:10px"
                    (click)="conflito.set('')">Entendi</button>
          </div>
        }
        @if (n.doc.fechado) {
          <div class="aviso">
            Nota já baixada por {{ n.doc.matrFec }} em {{ quando(n) }} — somente leitura.
          </div>
        }

        <div class="conf">
          <div>
            <div class="cx" [class.foco]="!n.doc.fechado" style="padding:14px 16px">
              <label class="rot" for="item">Bipe o item — nota {{ n.doc.documento }}</label>
              <input id="item" #campoItem class="entrada" name="codigo" autocomplete="off"
                     [(ngModel)]="codigo" placeholder="—" (keyup.enter)="bipar()"
                     [disabled]="n.doc.fechado">
              <div class="trilho">
                <div class="preen" [class.at]="n.doc.temDivergencia"
                     [style.width.%]="n.doc.itens.length ? (n.doc.itensLancados / n.doc.itens.length) * 100 : 0"></div>
              </div>
            </div>

            <div class="lista" style="margin-top:12px">
              <table>
                <thead><tr>
                  <th>Cód</th><th>Produto</th><th class="num">Nota</th><th class="num">Recebido</th><th></th>
                </tr></thead>
                <tbody>
                  @for (i of n.doc.itens; track i.codigo) {
                    <tr [class.div]="i.temDivergencia" [class.atual]="i.codigo === ultimoLancado()">
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
          </div>

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

            @if (leitura()?.item && !n.doc.fechado) {
              <div class="qtd">
                <span class="rot">Quantidade recebida — nota diz {{ leitura()?.item?.qtdNf }}</span>
                <input #campoQtd type="number" inputmode="numeric" [(ngModel)]="qtd" name="qtd"
                       (keyup.enter)="lancar()">
              </div>
            }

            <div class="hist">
              @if (historico().length) { Anteriores<br> }
              @for (h of historico(); track h) { {{ h }}<br> }
            </div>
          </div>
        </div>
      </div>

      <div class="rodape">
        <button class="btn sec" (click)="irPara(2)">Voltar</button>
        @if (!n.doc.fechado) {
          <button class="btn" (click)="baixar()">Dar baixa nesta nota (F2)</button>
        } @else if (temNotaAberta()) {
          <button class="btn" (click)="proximaAberta()">Próxima nota</button>
        } @else {
          <button class="btn" (click)="irPara(4)">Concluir carga</button>
        }
        <span class="esp">nota {{ indice() + 1 }} de {{ notas().length }} · {{ baixadas() }} baixada(s)</span>
      </div>
    }

    <!-- ───────────────────────────── passo 4 · resumo ───────────────────────────── -->
    @if (passo() === 4) {
      <div class="fim">
        <h2>Carga recebida</h2>
        <div class="emb">Doca {{ doca }} · {{ notas().length }} nota(s) · {{ baixadas() }} baixada(s)</div>
        <div class="tela" style="max-width:760px;margin:var(--s6) auto 0;text-align:left">
          <div class="lista">
            <table>
              <thead><tr>
                <th>Nota</th><th>Fornecedor</th><th class="num">Itens</th>
                <th class="num">Volumes</th><th>Situação</th>
              </tr></thead>
              <tbody>
                @for (n of notas(); track n.numero) {
                  <tr>
                    <td class="cod">{{ n.doc.documento }}</td>
                    <td>{{ n.doc.fornecedor }}</td>
                    <td class="num">{{ n.doc.itensLancados }}/{{ n.doc.itens.length }}</td>
                    <td class="num">{{ n.volumes ?? '—' }}</td>
                    <td>
                      @if (n.doc.fechado) { <span class="pill p-ok">baixada</span> }
                      @else { <span class="pill p-agu">em aberto</span> }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
        <div style="margin-top:var(--s6);display:flex;gap:10px;justify-content:center">
          <button class="btn" (click)="novaCarga()">Nova carga</button>
          <button class="btn sec" (click)="sairDoFluxo()">Ir para as docas</button>
        </div>
      </div>
    }

    @if (confirmacao(); as c) {
      <div class="dialogo" role="dialog" aria-modal="true">
        <div class="dcartao">
          <p>{{ c.mensagem }}</p>
          <div class="dacoes">
            <button class="btn sec" (click)="confirmacao.set(null); focarItem()">Não</button>
            <button class="btn" (click)="c.acao()">Sim</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class FluxoCorreto {
  api = inject(Api);
  som = inject(SinalSonoro);
  private router = inject(Router);
  private campoDoca = viewChild<ElementRef<HTMLInputElement>>('campoDoca');
  private campoNota = viewChild<ElementRef<HTMLInputElement>>('campoNota');
  private campoItem = viewChild<ElementRef<HTMLInputElement>>('campoItem');
  private campoQtd = viewChild<ElementRef<HTMLInputElement>>('campoQtd');

  passo = signal<1 | 2 | 3 | 4>(1);
  doca: number | null = null;
  numeroNota = '';
  notas = signal<NotaSessao[]>([]);
  buscando = signal(false);
  erro = signal('');

  indice = signal(0);
  codigo = '';
  qtd: number | null = null;
  leitura = signal<Leitura | null>(null);
  historico = signal<string[]>([]);
  ultimoLancado = signal('');
  flash = signal(false);
  conflito = signal('');
  confirmacao = signal<Confirmacao>(null);

  notaAtual = computed(() => this.notas()[this.indice()] ?? null);
  baixadas = computed(() => this.notas().filter(n => n.doc.fechado).length);
  preenchidas = computed(() => this.notas().filter(n => (n.volumes ?? 0) > 0).length);
  temNotaAberta = computed(() => this.notas().some(n => !n.doc.fechado));

  constructor() {
    if (!this.api.sessao() && !this.api.restaurar()) { this.router.navigate(['/entrar']); }
    afterNextRender(() => this.campoDoca()?.nativeElement.focus());
  }

  /** Mesmo contrato de teclado das outras telas: F2 avança, tecla imprimível devolve o foco. */
  @HostListener('document:keydown', ['$event'])
  teclado(e: KeyboardEvent) {
    if (this.confirmacao()) {
      if (e.key === 'Escape') { this.confirmacao.set(null); this.focarItem(); }
      return;
    }
    if (e.key === 'F2') {
      e.preventDefault();
      if (this.passo() === 1 && this.podeAvancarDoPasso1()) this.irPara(2);
      else if (this.passo() === 2 && this.podeAvancarDoPasso2()) this.irPara(3);
      else if (this.passo() === 3) this.baixar();
      return;
    }
    const digitando = (e.target as HTMLElement)?.tagName === 'INPUT';
    if (!digitando && e.key.length === 1 && this.passo() === 3) this.focarItem();
  }

  // ───────────────────────────── passo 1 ─────────────────────────────

  focarNota() { queueMicrotask(() => this.campoNota()?.nativeElement.focus()); }
  focarItem() { queueMicrotask(() => this.campoItem()?.nativeElement.focus()); }

  /**
   * Busca a nota no banco e junta à carga. O operador bipa uma atrás da outra sem tocar no mouse,
   * então o campo se limpa e continua com o foco, e o erro nunca interrompe a sequência.
   */
  async adicionarNota() {
    const numero = this.numeroNota.trim();
    if (!numero || this.buscando()) return;
    this.erro.set('');

    if (this.notas().some(n => n.numero === numero || n.doc.documento.trim() === numero)) {
      this.numeroNota = '';
      this.erro.set(`A nota ${numero} já está nesta carga.`);
      this.som.recusa();
      return;
    }

    this.buscando.set(true);
    try {
      const doc = await this.comPrazo(this.api.documento(numero));
      this.notas.update(l => [...l, { numero, doc, volumes: null, transportadora: '', observacao: '' }]);
      this.numeroNota = '';
      this.som.aceite();
    } catch (e: any) {
      this.erro.set(this.explicar(e, numero));
      this.som.recusa();
    } finally {
      this.buscando.set(false);
      this.focarNota();
    }
  }

  /**
   * Traduz a falha para quem está na doca.
   *
   * "Http failure response for …: 0 undefined" — que é o que o HttpClient dá de graça — não diz
   * nada a um conferente, e foi exatamente o que apareceu na tela no primeiro teste. A regra de
   * negócio (RK-c0fce5362f62 e afins) sempre vem em problem+json e tem precedência.
   */
  private explicar(e: any, numero: string): string {
    if (e?.error?.detail) return e.error.detail;
    if (e?.status === 0) return 'Sem resposta da API. Verifique a rede da doca e bipe de novo.';
    if (!e?.status && e?.message) return e.message;   // estouro de prazo
    return `Nota ${numero} não encontrada.`;
  }

  /**
   * Prazo máximo para a busca da nota.
   *
   * Sem ele, uma API que aceita a conexão e não responde deixa a tela em "buscando no banco…"
   * indefinidamente — e o operador fica olhando para uma doca parada sem mensagem nenhuma.
   * Aconteceu no primeiro teste desta tela. Oito segundos é mais que o dobro do pior tempo
   * medido para abrir documento, então não corta busca legítima.
   */
  private comPrazo<T>(promessa: Promise<T>, ms = 8000): Promise<T> {
    let alarme: ReturnType<typeof setTimeout>;
    const estouro = new Promise<never>((_, rejeitar) => {
      alarme = setTimeout(
        () => rejeitar(new Error('O banco não respondeu em 8 s. Bipe a nota de novo.')), ms);
    });
    return Promise.race([promessa, estouro]).finally(() => clearTimeout(alarme)) as Promise<T>;
  }

  removerNota(n: NotaSessao) {
    this.notas.update(l => l.filter(x => x !== n));
    if (this.indice() >= this.notas().length) this.indice.set(Math.max(0, this.notas().length - 1));
  }

  totalDaNota(n: NotaSessao) {
    return n.doc.itens.reduce((s, i) => s + i.qtdNf, 0);
  }

  /** A nota traz a doca da integração; se o operador informou outra, mostra — não bloqueia. */
  docaDivergente(n: NotaSessao) {
    return n.doc.doca != null && this.doca != null && Number(n.doc.doca) !== Number(this.doca);
  }

  podeAvancarDoPasso1() {
    return !!this.doca && this.notas().length > 0;
  }

  // ───────────────────────────── passo 2 ─────────────────────────────

  /** Volumes é o único obrigatório: é o número que o operador confere contra o caminhão. */
  podeAvancarDoPasso2() {
    return this.notas().length > 0 && this.notas().every(n => (n.volumes ?? 0) > 0);
  }

  irPara(p: 1 | 2 | 3 | 4) {
    this.passo.set(p);
    this.erro.set('');
    if (p === 3) {
      // Começa pela primeira nota que ainda não foi baixada, não pela primeira da lista:
      // reentrar no passo 3 depois de baixar duas não pode voltar para o começo.
      const i = this.notas().findIndex(n => !n.doc.fechado);
      this.indice.set(i >= 0 ? i : 0);
      this.limparLeitura();
      this.focarItem();
    }
    if (p === 1) queueMicrotask(() => this.campoDoca()?.nativeElement.focus());
  }

  // ───────────────────────────── passo 3 ─────────────────────────────

  irParaNota(i: number) {
    this.indice.set(i);
    this.limparLeitura();
    this.focarItem();
  }

  proximaAberta() {
    const i = this.notas().findIndex(n => !n.doc.fechado);
    if (i >= 0) this.irParaNota(i); else this.irPara(4);
  }

  private limparLeitura() {
    this.leitura.set(null);
    this.qtd = null;
    this.codigo = '';
    this.historico.set([]);
  }

  private piscar() {
    this.flash.set(true);
    setTimeout(() => this.flash.set(false), 260);
  }

  /** Substitui a nota atual pelo documento que o servidor devolveu depois de escrever. */
  private atualizarNota(doc: DocumentoConf) {
    this.notas.update(l => l.map((n, i) => i === this.indice() ? { ...n, doc } : n));
  }

  async bipar() {
    const n = this.notaAtual();
    const valor = this.codigo.trim();
    if (!n || !valor) return;
    this.codigo = '';

    const r = await this.api.ler(n.doc.documento.trim(), valor);
    this.leitura.set(r);
    this.piscar();
    if (r.estado === 'aceito' || r.estado === 'confirmar') this.som.aceite(); else this.som.recusa();

    if (r.estado === 'aceito' || r.estado === 'confirmar') {
      this.qtd = r.item?.qtdNf ?? null;
      queueMicrotask(() => this.campoQtd()?.nativeElement.select());
    } else {
      this.focarItem();
    }
  }

  async lancar(confirmado = false) {
    const n = this.notaAtual();
    const item = this.leitura()?.item;
    if (!n || !item || this.qtd === null) return;
    try {
      // AD-17: a versão que esta tela leu volta ao servidor, que recusa escrita concorrente.
      const versao = n.doc.itens.find(i => i.codigo === item.codigo)?.versao;
      const atualizado = await this.api.lancar(
        n.doc.documento.trim(), item.codigo, Number(this.qtd), confirmado, versao);
      this.atualizarNota(atualizado);
      this.ultimoLancado.set(item.codigo);
      this.historico.update(h => [`${item.codigo} · ${this.qtd} un`, ...h].slice(0, 2));
      this.leitura.set(null);
      this.qtd = null;
      this.focarItem();
    } catch (e: any) {
      const detalhe = e?.error?.detail;
      if (e?.error?.tipo === 'ExigeConfirmacao' && detalhe) {
        this.confirmacao.set({
          chave: e.error.ruleKey, mensagem: detalhe,
          acao: () => { this.confirmacao.set(null); this.lancar(true); },
        });
      } else if (e?.status === 409) {
        this.conflito.set(detalhe ?? 'Outro operador alterou este item.');
        await this.recarregarNota();
        this.leitura.set(null);
        this.qtd = null;
        this.focarItem();
      }
    }
  }

  private async recarregarNota() {
    const n = this.notaAtual();
    if (!n) return;
    try { this.atualizarNota(await this.api.documento(n.doc.documento.trim())); } catch { /* mantém */ }
  }

  /** A baixa é o fechamento da nota — RK-fa93a48fbecc pede confirmação antes. */
  async baixar(confirmado = false) {
    const n = this.notaAtual();
    if (!n || n.doc.fechado) return;
    try {
      const atualizado = await this.api.fechar(n.doc.documento.trim(), confirmado);
      this.atualizarNota(atualizado);
      this.som.aceite();
      // Encadeia: baixou esta, já cai na próxima que ainda está aberta; se não há, fecha a carga.
      if (this.temNotaAberta()) this.proximaAberta(); else this.irPara(4);
    } catch (e: any) {
      const detalhe = e?.error?.detail;
      if (e?.error?.tipo === 'ExigeConfirmacao' && detalhe) {
        this.confirmacao.set({
          chave: e.error.ruleKey, mensagem: detalhe,
          acao: () => { this.confirmacao.set(null); this.baixar(true); },
        });
      } else if (detalhe) {
        this.conflito.set(detalhe);
      }
    }
  }

  rotuloSelo() {
    const l = this.leitura();
    if (!l) return 'Aguardando leitura';
    return { aceito: 'Código aceito', confirmar: 'Código aceito',
             recusado: 'Leitura recusada', ambiguo: 'Leitura ambígua' }[l.estado];
  }

  /** AD-19: instante no fuso do armazém. */
  quando(n: NotaSessao) { return exibirInstante(n.doc.dtHora); }

  novaCarga() {
    this.notas.set([]);
    this.indice.set(0);
    this.numeroNota = '';
    this.limparLeitura();
    this.irPara(1);
  }

  sairDoFluxo() { this.router.navigate(['/docas']); }
}

/*
 * Pendências de backend deste fluxo — a tela funciona sem elas, mas com remendo no cliente:
 *
 *  1. Não existe o conceito de CARGA. A lista de notas vive só nesta aba; recarregar a página
 *     perde a montagem. Precisaria de POST /api/cargas {doca, notas[]} e GET /api/cargas/{id}.
 *  2. A doca informada no passo 1 não é gravada em lugar nenhum: conferencia.doca vem da
 *     integração da nota (AD-14). Atribuir doca pela tela exige endpoint próprio e decisão de
 *     negócio — hoje a tela só avisa quando a nota consta em outra doca.
 *  3. Volumes, transportadora e observação não têm coluna. Ver a proposta no meu retorno: ou
 *     colunas novas em `conferencia`, ou uma tabela `receb_carga` ao lado, sem tocar no legado.
 *  4. Cada nota buscada é um GET separado. Com dez notas por carga, dez viagens — um
 *     GET /api/conferencias/lote?documentos=a,b,c resolveria em uma.
 */
