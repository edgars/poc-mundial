import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

// O frontend lê a URL da API de uma variável injetada no index.html em tempo de execução.
declare global { interface Window { API_URL?: string } }
const base = () => window.API_URL || 'http://localhost:5000';

export interface Permissao {
  tabela: string; descricao: string;
  consultar: boolean; incluir: boolean; alterar: boolean; excluir: boolean;
}
export interface Sessao { matricula: string; nome: string; permissoes: Permissao[] }

export interface ItemConf {
  codigo: string; descricao?: string; dun14?: string; itNf?: number;
  qtdNf: number; qtdRec: number; divergencia?: number;
  temDivergencia: boolean; pendencia: boolean; situacao: string;
}
export interface DocumentoConf {
  documento: string; chave: string; doca?: number; fornecedor?: string;
  fechado: boolean; situacao: string; situacaoDescricao: string;
  matrConf?: string; matrFec?: string; dtHora?: string;
  itensLancados: number; itensPendentes: number; temDivergencia: boolean;
  aviso?: { chave: string; mensagem: string }; itens: ItemConf[];
}
export interface ResumoDoca {
  doca: number; estado: string; documento?: string; fornecedor?: string; operador?: string;
  itensLancados: number; itensTotal: number; temDivergencia: boolean; temPendencia: boolean;
  abertaDesde?: string;
}
export interface Leitura {
  estado: 'aceito' | 'recusado' | 'ambiguo' | 'confirmar';
  chave?: string; mensagem?: string; candidatos?: string[];
  item?: { codigo: string; descricao: string; embalagem?: string; embalagemQtd?: number;
           dun14?: string; qtdNf: number; qtdRec: number; pendencia: boolean };
}

@Injectable({ providedIn: 'root' })
export class Api {
  private http = inject(HttpClient);
  readonly sessao = signal<Sessao | null>(null);

  async entrar(matricula: string, senha: string): Promise<Sessao> {
    const s = await firstValueFrom(
      this.http.post<Sessao>(`${base()}/api/entrar`, { matricula, senha }));
    this.sessao.set(s);
    sessionStorage.setItem('sessao', JSON.stringify(s));
    return s;
  }

  restaurar(): Sessao | null {
    const bruto = sessionStorage.getItem('sessao');
    if (!bruto) return null;
    const s = JSON.parse(bruto) as Sessao;
    this.sessao.set(s);
    return s;
  }

  sair() { this.sessao.set(null); sessionStorage.removeItem('sessao'); }

  docas() { return firstValueFrom(this.http.get<ResumoDoca[]>(`${base()}/api/docas`)); }

  documento(doc: string) {
    return firstValueFrom(this.http.get<DocumentoConf>(`${base()}/api/conferencia?documento=${encodeURIComponent(doc)}`));
  }

  ler(doc: string, codigo: string) {
    return firstValueFrom(this.http.post<Leitura>(
      `${base()}/api/conferencia/leituras?documento=${encodeURIComponent(doc)}`, { codigo }));
  }

  lancar(doc: string, codigo: string, quantidade: number, matricula: string, confirmado: boolean) {
    return firstValueFrom(this.http.post<DocumentoConf>(
      `${base()}/api/conferencia/lancamentos?documento=${encodeURIComponent(doc)}`,
      { codigo, quantidade, matricula, confirmado }));
  }

  fechar(doc: string, matricula: string, confirmado: boolean) {
    return firstValueFrom(this.http.post<DocumentoConf>(
      `${base()}/api/conferencia/fechamento?documento=${encodeURIComponent(doc)}`, { matricula, confirmado }));
  }

  codigosDemo() {
    return firstValueFrom(this.http.get<{codigo:string;efeito:string;tipo:string}[]>(`${base()}/api/demo/codigos`));
  }
}
