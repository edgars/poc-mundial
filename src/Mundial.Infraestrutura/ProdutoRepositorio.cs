using Dapper;
using Mundial.Aplicacao;
using Mundial.Dominio;

namespace Mundial.Infraestrutura;

/// <summary>AD-16: o único caminho de escrita em `estoq`.</summary>
public sealed class ProdutoRepositorio(FabricaConexao fabrica) : IProdutoRepositorio
{
    private readonly ProdutoConsulta _consulta = new(fabrica);

    public Task<IReadOnlyList<Produto>> PorCodigoDeBarras(string c, CancellationToken ct = default)
        => _consulta.PorCodigoDeBarras(c, ct);

    public Task<Produto?> PorCodigo(string c, CancellationToken ct = default)
        => _consulta.PorCodigo(c, ct);

    public async Task<Produto?> DonoDoCodigoDeBarras(string codigoBarras, string exceto,
        CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var l = await c.QuerySingleOrDefaultAsync<dynamic>(@"
            SELECT TOP 1 codigo, descri FROM dbo.estoq
             WHERE RTRIM(codigo) <> @exceto
               AND (RTRIM(barr_emb) = @cb OR RTRIM(barr_emb2) = @cb OR RTRIM(barr_emb3) = @cb)",
            new { cb = codigoBarras, exceto });
        return l is null ? null : new Produto
        {
            Codigo = ((string)l.codigo).Trim(),
            Descricao = ((string)l.descri).Trim()
        };
    }

    public async Task Salvar(Produto p, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        await c.ExecuteAsync(@"
            UPDATE dbo.estoq
               SET barr_emb = @d1, barr_emb2 = @d2, barr_emb3 = @d3
             WHERE RTRIM(codigo) = @codigo",
            new { codigo = p.Codigo.Trim(), d1 = p.Dun[0], d2 = p.Dun[1], d3 = p.Dun[2] });
    }
}
