import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

// O frontend lê a URL da API de uma variável injetada no index.html em tempo de execução.
declare global { interface Window { API_URL?: string; TZ_APLICACAO?: string } }
const base = () => window.API_URL || 'http://localhost:5000';

/**
 * AD-19 / NFR-8: o instante viaja em UTC e é exibido no fuso do armazém.
 * Sem isto uma conferência fechada 23h30 aparece no dia seguinte para quem lê.
 */
export const fusoArmazem = () => window.TZ_APLICACAO || 'America/Sao_Paulo';

export function exibirInstante(iso?: string | null): string {
  if (!iso) return '—';
  const d = new Date(iso.endsWith('Z') || iso.includes('+') ? iso : iso + 'Z');
  return d.toLocaleString('pt-BR', {
    timeZone: fusoArmazem(), day: '2-digit', month: '2-digit',
    year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

export interface Permissao {
  tabela: string; descricao: string;
  consultar: boolean; incluir: boolean; alterar: boolean; excluir: boolean;
}
export interface Sessao { token: string; expiraEm: string; matricula: string; nome: string; permissoes: Permissao[] }

export interface ItemConf {
  codigo: string; descricao?: string; dun14?: string; itNf?: number;
  qtdNf: number; qtdRec: number; divergencia?: number;
  temDivergencia: boolean; pendencia: boolean; situacao: string;
  /** AD-17: a versão lida volta no lançamento, para o servidor detectar escrita concorrente. */
  versao?: string;
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
export interface Produto {
  codigo: string; descricao: string; embalagem?: string; embalagemQtd?: number;
  ean: (string | null)[]; dun: (string | null)[];
}
export interface Etiqueta {
  codigo: string; descricao: string; embalagem?: string; embalagemQtd?: number;
  codigoBarras: string; zpl: string;
}
export interface ResumoFornecedor {
  codigo: string; descricao: string; cgc: string; cidade: string; uf: string;
  situacao: string; obrigatoriosCompletos: boolean; faltando: string[];
}
export interface RegistroAuditoria {
  id: number; quando: string; usuario: string; tabela: string;
  chave: string; valorAnterior?: string; valorAtual?: string;
}
export interface ResumoConferencia {
  documento: string; fornecedor?: string; doca?: number; matrConf?: string; matrFec?: string;
  itensLancados: number; itensTotal: number; temDivergencia: boolean; situacao: string; dtHora?: string;
}
export interface Pagina<T> { itens: T[]; total: number; pagina: number; tamanho: number }

export interface Leitura {
  estado: 'aceito' | 'recusado' | 'ambiguo' | 'confirmar';
  chave?: string; mensagem?: string; candidatos?: string[];
  /** RK-dab7d2033e2e: só vem preenchido para quem tem permissão de inclusão em estoq. */
  ofertaCadastro?: string;
  item?: { codigo: string; descricao: string; embalagem?: string; embalagemQtd?: number;
           dun14?: string; qtdNf: number; qtdRec: number; pendencia: boolean };
}

@Injectable({ providedIn: 'root' })
export class Api {
  private http = inject(HttpClient);
  readonly sessao = signal<Sessao | null>(null);

  /** AD-7: o token acompanha toda chamada autenticada. */
  private get auth() {
    const t = this.sessao()?.token;
    return t ? { headers: { Authorization: `Bearer ${t}` } } : {};
  }

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

  docas() { return firstValueFrom(this.http.get<ResumoDoca[]>(`${base()}/api/docas`, this.auth)); }

  documento(doc: string) {
    return firstValueFrom(this.http.get<DocumentoConf>(
      `${base()}/api/conferencia?documento=${encodeURIComponent(doc)}`, this.auth));
  }

  ler(doc: string, codigo: string) {
    // AD-8: quem decide se pode incluir é o servidor, lendo a permissão do token.
    return firstValueFrom(this.http.post<Leitura>(
      `${base()}/api/conferencia/leituras?documento=${encodeURIComponent(doc)}`, { codigo }, this.auth));
  }

  lancar(doc: string, codigo: string, quantidade: number, confirmado: boolean, versao?: string) {
    // A matrícula não vai no corpo: o servidor a lê do token (AD-7).
    return firstValueFrom(this.http.post<DocumentoConf>(
      `${base()}/api/conferencia/lancamentos?documento=${encodeURIComponent(doc)}`,
      { codigo, quantidade, confirmado, versao }, this.auth));
  }

  fechar(doc: string, confirmado: boolean) {
    return firstValueFrom(this.http.post<DocumentoConf>(
      `${base()}/api/conferencia/fechamento?documento=${encodeURIComponent(doc)}`,
      { confirmado }, this.auth));
  }

  produto(codigo: string) {
    return firstValueFrom(this.http.get<Produto>(`${base()}/api/produtos/${codigo}`, this.auth));
  }

  criarProduto(p: {codigo: string; descricao: string; embalagem?: string;
                   embalagemQtd?: number; ean?: string; dun: (string | null)[]}) {
    return firstValueFrom(this.http.post<{criado: boolean}>(`${base()}/api/produtos`, p, this.auth));
  }

  salvarProduto(p: {codigo: string; descricao: string; embalagem?: string;
                    embalagemQtd?: number; ean?: string; dun: (string | null)[]}) {
    return firstValueFrom(this.http.put<{gravado: boolean}>(
      `${base()}/api/produtos/${p.codigo}`, p, this.auth));
  }

  gravarCodigos(codigo: string, dun: (string | null)[], confirmado: boolean) {
    return firstValueFrom(this.http.put<{gravado: boolean}>(
      `${base()}/api/produtos/${codigo}/codigos`, { dun, confirmado }, this.auth));
  }

  etiqueta(codigo: string, codigoBarras?: string) {
    const q = codigoBarras ? `?codigoBarras=${encodeURIComponent(codigoBarras)}` : '';
    return firstValueFrom(this.http.get<Etiqueta>(`${base()}/api/produtos/${codigo}/etiqueta${q}`, this.auth));
  }

  conferencias(pagina = 0, tamanho = 50, busca?: string) {
    const q = new URLSearchParams({ pagina: String(pagina), tamanho: String(tamanho) });
    if (busca) q.set('busca', busca);
    return firstValueFrom(this.http.get<Pagina<ResumoConferencia>>(
      `${base()}/api/conferencias?${q}`, this.auth));
  }

  fornecedores(pagina = 0, tamanho = 50, busca?: string) {
    const q = new URLSearchParams({ pagina: String(pagina), tamanho: String(tamanho) });
    if (busca) q.set('busca', busca);
    return firstValueFrom(this.http.get<Pagina<ResumoFornecedor>>(`${base()}/api/fornecedores?${q}`, this.auth));
  }

  auditoria(pagina = 0, tamanho = 50, busca?: string) {
    const q = new URLSearchParams({ pagina: String(pagina), tamanho: String(tamanho) });
    if (busca) q.set('busca', busca);
    return firstValueFrom(this.http.get<Pagina<RegistroAuditoria>>(`${base()}/api/auditoria?${q}`, this.auth));
  }

  resetarDemo() {
    return firstValueFrom(this.http.post<{resetado: boolean}>(`${base()}/api/demo/reset`, {}, this.auth));
  }

  pode(tabela: string, op: 'consultar' | 'incluir' | 'alterar' | 'excluir') {
    const chave = tabela.length > 10 ? tabela.slice(0, 10) : tabela;
    return this.sessao()?.permissoes.some(p => p.tabela === chave && p[op]) ?? false;
  }

  codigosDemo() {
    return firstValueFrom(this.http.get<{codigo:string;efeito:string;tipo:string}[]>(`${base()}/api/demo/codigos`));
  }
}
