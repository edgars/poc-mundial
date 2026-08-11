using Mundial.Dominio;

namespace Mundial.Aplicacao;

public sealed record ResumoFornecedor(string Codigo, string Descricao, string Cgc, string Cidade, string Uf,
    string Situacao, bool ObrigatoriosCompletos, IReadOnlyList<string> Faltando);

public sealed record RegistroAuditoria(long Id, DateTime Quando, string Usuario, string Tabela,
    string Chave, string? ValorAnterior, string? ValorAtual);

/// <summary>AD-16: `forne` é lido aqui; no POC ninguém escreve nela.</summary>
public interface IFornecedorConsulta
{
    Task<IReadOnlyList<Fornecedor>> Listar(FiltroListagem filtro, CancellationToken ct = default);
    Task<int> Contar(FiltroListagem filtro, CancellationToken ct = default);
}

public interface IAuditoriaConsulta
{
    Task<IReadOnlyList<RegistroAuditoria>> Listar(FiltroListagem filtro, CancellationToken ct = default);
    Task<int> Contar(FiltroListagem filtro, CancellationToken ct = default);
}

/// <summary>
/// FR-34 e FR-35: consulta de fornecedor. O POC não tem tela de cadastro, mas as treze regras de
/// obrigatoriedade rodam sobre o que está gravado — assim a lacuna fica visível em vez de silenciosa.
/// </summary>
public sealed class ConsultarFornecedores(IFornecedorConsulta fornecedores)
{
    public async Task<Pagina<ResumoFornecedor>> Executar(FiltroListagem filtro, CancellationToken ct = default)
    {
        var itens = await fornecedores.Listar(filtro, ct);
        var total = await fornecedores.Contar(filtro, ct);

        var resumos = itens.Select(f =>
        {
            var faltas = f.AvaliarObrigatorios();
            return new ResumoFornecedor(
                f.Codigo.Trim(), f.Descricao?.Trim() ?? "", f.Cgc?.Trim() ?? "",
                f.Cidade?.Trim() ?? "", f.Uf?.Trim() ?? "", f.Situacao?.Trim() ?? "",
                faltas.Count == 0,
                faltas.Select(x => x.Mensagem ?? "").ToList());
        }).ToList();

        return new Pagina<ResumoFornecedor>(resumos, total, filtro.Pagina, filtro.TamanhoEfetivo);
    }
}
