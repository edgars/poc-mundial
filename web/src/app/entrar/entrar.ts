import { Component, inject, signal, ElementRef, viewChild, afterNextRender } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api } from '../api/api';
import { Sessao } from '../api/sessao';

@Component({
  selector: 'app-entrar',
  imports: [FormsModule],
  styles: [`
    .tela{min-height:100vh;display:grid;place-items:center;padding:40px}
    .cartao{width:390px;background:var(--surface-raised);border:1px solid var(--border);
      border-radius:var(--r-lg);padding:28px}
    .marca{display:block;width:196px;height:auto;margin:0 auto 4px}
    h1{font-size:23px;font-weight:600;letter-spacing:-.02em;margin:8px 0 22px;text-align:center}
    .campo{margin-bottom:16px}
    input{width:100%;margin-top:6px;background:var(--surface-base);border:1px solid var(--border);
      border-radius:var(--r);padding:12px 14px;font-family:var(--mono);font-size:20px;
      letter-spacing:.05em;color:var(--text)}
    input:focus{outline:0;border-color:var(--focus);box-shadow:0 0 0 3px var(--focus-glow)}
    .btn{width:100%;margin-top:8px}
    .erro{margin-top:14px;background:var(--reject-wash);border:1px solid rgba(248,113,113,.4);
      color:var(--reject);border-radius:var(--r);padding:10px 12px;font-size:13px;white-space:pre-line}
    .dica{margin-top:18px;font-size:12px;color:var(--text-disabled);line-height:1.7}
    .dica b{color:var(--text-muted);font-weight:500}
  `],
  template: `
    <div class="tela">
      <form class="cartao" (ngSubmit)="entrar()">
        <img class="marca" src="mundial.svg" alt="Supermercados Mundial" width="196" height="134">
        <h1>Conferência de Recebimento</h1>

        <div class="campo">
          <label class="rot" for="mat">Matrícula</label>
          <input id="mat" #primeiro name="matricula" [(ngModel)]="matricula" autocomplete="off" inputmode="numeric">
        </div>
        <div class="campo">
          <label class="rot" for="sen">Senha</label>
          <input id="sen" type="password" name="senha" [(ngModel)]="senha" autocomplete="off">
        </div>

        <button class="btn" type="submit" [disabled]="ocupado()">
          {{ ocupado() ? 'Entrando…' : 'Entrar' }}
        </button>

        @if (sessao.expirou()) {
          <div class="erro" style="background:var(--attention-wash);border-color:rgba(251,191,36,.4);
               color:var(--attention)">Sessão encerrada por inatividade</div>
        }
        @if (erro()) { <div class="erro">{{ erro() }}</div> }

        <div class="dica">
          <b>Demonstração</b><br>
          04127 · Cleber, operador · sem permissão de inclusão<br>
          04310 · Rosana, supervisão · todas as permissões<br>
          05001 · Paulo · nível insuficiente<br>
          Senha de todos: <b>mundial</b>
        </div>
      </form>
    </div>
  `,
})
export class Entrar {
  private api = inject(Api);
  sessao = inject(Sessao);
  private router = inject(Router);
  private primeiro = viewChild<ElementRef<HTMLInputElement>>('primeiro');

  matricula = '';
  senha = '';
  erro = signal('');
  ocupado = signal(false);

  constructor() {
    afterNextRender(() => this.primeiro()?.nativeElement.focus());
    if (this.api.restaurar()) { this.sessao.iniciar(); this.router.navigateByUrl(this.sessao.destinoAposEntrar()); }
  }

  async entrar() {
    this.erro.set('');
    this.ocupado.set(true);
    try {
      await this.api.entrar(this.matricula.trim(), this.senha);
      this.sessao.expirou.set(false);
      this.sessao.iniciar();
      // Story 1.7: quem foi desconectado no meio de uma conferência volta para ela.
      this.router.navigateByUrl(this.sessao.destinoAposEntrar());
    } catch (e: any) {
      // AD-11: a mensagem do legado chega em problem+json
      this.erro.set(e?.error?.detail ?? 'Não foi possível entrar.');
    } finally {
      this.ocupado.set(false);
    }
  }
}
