import { Component, inject, signal, ElementRef, viewChild, afterNextRender, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api, ResumoConferencia, ResumoFornecedor, RegistroAuditoria, exibirInstante } from '../api/api';

type Aba = 'conferencias' | 'fornecedores' | 'auditoria';

/**
 * Épico 4 · Stories 2.8, 4.1 e 4.2 — a visão do supervisor.
 * TabelaDensa: densidade alta, separação por borda, filtro por teclado com "/",
 * e a linha em foco recebe filete ciano à esquerda — nunca fundo, que brigaria com o âmbar
 * da linha divergente.
 */
@Component({
  selector: 'app-consultas',
  imports: [FormsModule],
  styles: [`
    .abas{display:flex;gap:8px;padding:var(--s4) var(--s4) 0}
    .aba{background:transparent;border:1px solid var(--border);border-radius:var(--r);
      padding:8px 14px;color:var(--text-muted);font-size:13px;cursor:pointer}
    .aba[aria-selected="true"]{border-color:var(--focus);color:var(--text);
      box-shadow:0 0 0 3px var(--focus-glow)}
    .filtros{display:flex;gap:10px;padding:var(--s3) var(--s4);align-items:center}
    .filtros input{background:var(--surface-raised);border:1px solid var(--border);
      border-radius:var(--r);padding:8px 12px;font-size:13px;color:var(--text);min-width:240px}
    .filtros input:focus{outline:0;border-color:var(--focus);box-shadow:0 0 0 3px var(--focus-glow)}
    .dica{font-family:var(--mono);font-size:11px;color:var(--text-disabled)}
    .lista{margin:0 var(--s4) var(--s4);background:var(--surface-raised);
      border:1px solid var(--border);border-radius:var(--r-md);overflow:hidden}
    .rolagem{max-height:60vh;overflow:auto}
    table{width:100%;border-collapse:collapse;font-size:14px}
    th{position:sticky;top:0;text-align:left;font-weight:500;font-size:10px;letter-spacing:.14em;
      text-transform:uppercase;color:var(--text-muted);padding:10px var(--s4);
      background:var(--surface-overlay);z-index:1}
    td{padding:10px var(--s4);border-top:1px solid var(--border);font-variant-numeric:tabular-nums}
    td.mono{font-family:var(--mono);font-size:12px;color:var(--text-muted)}
    td.num{text-align:right;font-family:var(--mono)}
    tr.div{background:rgba(251,191,36,.05)}
    tr.foco td{box-shadow:inset 2px 0 0 var(--focus)}
    tr{cursor:pointer}
    .vazio{padding:50px;text-align:center;color:var(--text-muted)}
    .rodape-lista{padding:10px var(--s4);border-top:1px solid var(--border);
      font-family:var(--mono);font-size:11px;color:var(--text-muted);display:flex;
      justify-content:space-between;align-items:center}
    .rodape-lista button{background:transparent;border:1px solid var(--border);border-radius:4px;
      color:var(--text-muted);padding:4px 10px;font:inherit;cursor:pointer}
    .valores{font-family:var(--mono);font-size:11px;line-height:1.6;white-space:pre-line;
      color:var(--text-muted);max-width:420px}
    .faltando{color:var(--reject);font-size:11px}
  `],
  template: `
    <div class="barra">
      <b>Consultas</b><span>{{ rotulo() }}</span>
      <span class="pt">{{ api.sessao()?.nome }}</span>
      <button class="btn sec" style="padding:6px 12px;font-size:12px" (click)="voltar()">Docas</button>
    </div>

    <div class="abas" role="tablist">
      <button class="aba" role="tab" [attr.aria-selected]="aba() === 'conferencias'"
              (click)="trocar('conferencias')">Conferências</button>
      @if (api.pode('forne','consultar')) {
        <button class="aba" role="tab" [attr.aria-selected]="aba() === 'fornecedores'"
                (click)="trocar('fornecedores')">Fornecedores</button>
      }
      @if (api.pode('log_even','consultar')) {
        <button class="aba" role="tab" [attr.aria-selected]="aba() === 'auditoria'"
                (click)="trocar('auditoria')">Auditoria</button>
      }
    </div>

    <div class="filtros">
      <input #filtro placeholder="Buscar" [(ngModel)]="busca" (keyup.enter)="carregar(0)">
      <span class="dica">/ foca a busca · ↑↓ navega · Enter abre</span>
    </div>

    <div class="lista">
      <div class="rolagem">
        @switch (aba()) {
          @case ('conferencias') {
            <table>
              <thead><tr>
                <th>Documento</th><th>Fornecedor</th><th>Doca</th><th>Conferiu</th>
                <th>Fechou</th><th class="num">Itens</th><th>Situação</th>
              </tr></thead>
              <tbody>
                @for (c of conferencias(); track c.documento; let i = $index) {
                  <tr [class.div]="c.temDivergencia" [class.foco]="i === linha()"
                      (click)="abrir(c)">
                    <td class="mono">{{ c.documento }}</td>
                    <td>{{ c.fornecedor }}</td>
                    <td class="mono">{{ c.doca }}</td>
                    <td class="mono">{{ c.matrConf || '—' }}</td>
                    <td class="mono">{{ c.matrFec || '—' }}</td>
                    <td class="num">{{ c.itensLancados }}/{{ c.itensTotal }}</td>
                    <td>
                      @if (c.temDivergencia) { <span class="pill p-div">diverge</span> }
                      @else if (c.situacao === 'F') { <span class="pill p-ok">fechada</span> }
                      @else if (c.situacao === 'C') { <span class="pill p-ok">em conferência</span> }
                      @else { <span class="pill p-agu">aguardando</span> }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          }
          @case ('fornecedores') {
            <table>
              <thead><tr>
                <th>Código</th><th>Razão social</th><th>CNPJ</th><th>Cidade</th>
                <th>UF</th><th>Obrigatórios</th>
              </tr></thead>
              <tbody>
                @for (f of fornecedores(); track f.codigo; let i = $index) {
                  <tr [class.foco]="i === linha()">
                    <td class="mono">{{ f.codigo }}</td>
                    <td>{{ f.descricao }}</td>
                    <td class="mono">{{ f.cgc }}</td>
                    <td>{{ f.cidade }}</td>
                    <td class="mono">{{ f.uf }}</td>
                    <td>
                      @if (f.obrigatoriosCompletos) { <span class="pill p-ok">completo</span> }
                      @else { <span class="faltando">{{ f.faltando.join(' · ') }}</span> }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          }
          @case ('auditoria') {
            <table>
              <thead><tr>
                <th>Quando</th><th>Matrícula</th><th>Tabela</th><th>Chave</th><th>Antes → depois</th>
              </tr></thead>
              <tbody>
                @for (a of auditoria(); track a.id; let i = $index) {
                  <tr [class.foco]="i === linha()">
                    <td class="mono">{{ instante(a.quando) }}</td>
                    <td class="mono">{{ a.usuario }}</td>
                    <td class="mono">{{ a.tabela }}</td>
                    <td class="mono">{{ a.chave }}</td>
                    <td class="valores">{{ a.valorAnterior }} → {{ a.valorAtual }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        }
      </div>

      @if (total() === 0) {
        <div class="vazio">{{ vazio() }}</div>
      } @else {
        <div class="rodape-lista">
          <span>{{ total() }} registro(s) · página {{ pagina() + 1 }}</span>
          <span>
            <button (click)="carregar(pagina() - 1)" [disabled]="pagina() === 0">anterior</button>
            <button (click)="carregar(pagina() + 1)" [disabled]="!temMais()">próxima</button>
          </span>
        </div>
      }
    </div>
  `,
})
export class Consultas {
  api = inject(Api);
  private router = inject(Router);
  private filtro = viewChild<ElementRef<HTMLInputElement>>('filtro');

  aba = signal<Aba>('conferencias');
  busca = '';
  pagina = signal(0);
  total = signal(0);
  linha = signal(0);
  conferencias = signal<ResumoConferencia[]>([]);
  fornecedores = signal<ResumoFornecedor[]>([]);
  auditoria = signal<RegistroAuditoria[]>([]);

  private readonly tamanho = 50;

  constructor() {
    if (!this.api.sessao() && !this.api.restaurar()) { this.router.navigate(['/entrar']); }
    afterNextRender(() => this.carregar(0));
  }

  /** Navegação por teclado: a supervisora varre a lista, não navega por ela. */
  @HostListener('document:keydown', ['$event'])
  teclado(e: KeyboardEvent) {
    const noFiltro = (e.target as HTMLElement)?.tagName === 'INPUT';
    if (e.key === '/' && !noFiltro) { e.preventDefault(); this.filtro()?.nativeElement.focus(); return; }
    if (noFiltro) return;
    const n = this.quantidade();
    if (e.key === 'ArrowDown') { e.preventDefault(); this.linha.update(l => Math.min(n - 1, l + 1)); }
    if (e.key === 'ArrowUp') { e.preventDefault(); this.linha.update(l => Math.max(0, l - 1)); }
    if (e.key === 'Enter' && this.aba() === 'conferencias') {
      const c = this.conferencias()[this.linha()];
      if (c) this.abrir(c);
    }
  }

  private quantidade() {
    return { conferencias: this.conferencias().length, fornecedores: this.fornecedores().length,
             auditoria: this.auditoria().length }[this.aba()];
  }

  trocar(a: Aba) { this.aba.set(a); this.busca = ''; this.linha.set(0); this.carregar(0); }

  async carregar(pagina: number) {
    if (pagina < 0) return;
    this.pagina.set(pagina);
    this.linha.set(0);
    const b = this.busca.trim() || undefined;
    try {
      if (this.aba() === 'conferencias') {
        const r = await this.api.conferencias(pagina, this.tamanho, b);
        this.conferencias.set(r.itens); this.total.set(r.total);
      } else if (this.aba() === 'fornecedores') {
        const r = await this.api.fornecedores(pagina, this.tamanho, b);
        this.fornecedores.set(r.itens); this.total.set(r.total);
      } else {
        const r = await this.api.auditoria(pagina, this.tamanho, b);
        this.auditoria.set(r.itens); this.total.set(r.total);
      }
    } catch { this.total.set(0); }
  }

  temMais() { return (this.pagina() + 1) * this.tamanho < this.total(); }
  instante(iso: string) { return exibirInstante(iso); }
  abrir(c: ResumoConferencia) { this.router.navigate(['/conferencia', c.documento]); }
  voltar() { this.router.navigate(['/docas']); }

  rotulo() {
    return { conferencias: 'Conferências', fornecedores: 'Fornecedores', auditoria: 'Trilha de auditoria' }[this.aba()];
  }
  vazio() {
    return { conferencias: 'Nenhuma conferência no período.',
             fornecedores: 'Nenhum fornecedor encontrado.',
             auditoria: 'Nenhum registro de auditoria.' }[this.aba()];
  }
}
