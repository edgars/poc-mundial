using Mundial.Dominio;

namespace Mundial.Aplicacao;

// AD-1: toda dependência externa entra como port declarado aqui e implementado na Infraestrutura.

public interface IRelogio { DateTime AgoraUtc { get; } }

public interface IUsuarioRepositorio
{
    Task<Usuario?> PorMatricula(string matricula, CancellationToken ct = default);
}

public interface IAcessoRepositorio
{
    Task<IReadOnlyList<Acesso>> PorMatricula(string matricula, CancellationToken ct = default);
}

public interface IDocumentoRepositorio
{
    Task<Documento?> PorNumeroExibido(string numero, CancellationToken ct = default);
    Task<IReadOnlyList<ResumoDoca>> Docas(CancellationToken ct = default);
    Task<IReadOnlyList<ResumoDocumento>> Listar(FiltroListagem filtro, CancellationToken ct = default);
    Task<int> ContarListagem(FiltroListagem filtro, CancellationToken ct = default);
    Task GravarLancamento(ItemConferencia item, CancellationToken ct = default);
    Task Fechar(Documento documento, CancellationToken ct = default);
}

/// <summary>AD-16: o fluxo de conferência LÊ estoq; quem escreve é o cadastro DUN-14.</summary>
public interface IProdutoConsulta
{
    Task<IReadOnlyList<Produto>> PorCodigoDeBarras(string codigo, CancellationToken ct = default);
    Task<Produto?> PorCodigo(string codigo, CancellationToken ct = default);
}

public interface IAuditoria
{
    Task Registrar(string usuario, string tabela, string chave, string? valorAnterior, string? valorAtual,
        CancellationToken ct = default);
}

public interface IHashSenha
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);
}

/// <summary>AD-15: contrato único de listagem.</summary>
public sealed record FiltroListagem(int Pagina = 0, int Tamanho = 50, string? Busca = null, string? Ordem = null)
{
    public int TamanhoEfetivo => Math.Clamp(Tamanho, 1, 200);
    public int Offset => Math.Max(0, Pagina) * TamanhoEfetivo;
}

public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Total, int PaginaAtual, int Tamanho)
{
    // Serializado como {itens,total,pagina,tamanho} — ver a configuração de JSON na Api.
}

public sealed record ResumoDoca(
    int Doca, string Estado, string? Documento, string? Fornecedor, string? Operador,
    int ItensLancados, int ItensTotal, bool TemDivergencia, bool TemPendencia,
    DateTime? AbertaDesdeUtc);

public sealed record ResumoDocumento(
    string Documento, string? Fornecedor, int? Doca, string? MatrConf, string? MatrFec,
    int ItensLancados, int ItensTotal, bool TemDivergencia, char Situacao, DateTime? DtHora);
