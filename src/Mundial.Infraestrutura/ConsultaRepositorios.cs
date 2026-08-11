using Dapper;
using Mundial.Aplicacao;
using Mundial.Dominio;

namespace Mundial.Infraestrutura;

public sealed class FornecedorConsulta(FabricaConexao fabrica) : IFornecedorConsulta
{
    public async Task<IReadOnlyList<Fornecedor>> Listar(FiltroListagem f, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var linhas = await c.QueryAsync<dynamic>(@"
            SELECT codfor, descri, cgc, cod_com, categ, tiplog, lograd, bairro, cep,
                   cidade, uf, inscr, situacao, data_grav, sub_trib, Mov_Est
              FROM dbo.forne
             WHERE (@busca IS NULL OR RTRIM(codfor) LIKE '%' + @busca + '%'
                                   OR descri LIKE '%' + @busca + '%')
             ORDER BY descri
            OFFSET @off ROWS FETCH NEXT @tam ROWS ONLY",
            new { busca = f.Busca, off = f.Offset, tam = f.TamanhoEfetivo });

        return linhas.Select(l => new Fornecedor
        {
            Codigo = (string)l.codfor, Descricao = (string?)l.descri, Cgc = (string?)l.cgc,
            CodCom = (string?)l.cod_com, Categoria = (string?)l.categ, TipoLogradouro = (string?)l.tiplog,
            Logradouro = (string?)l.lograd, Bairro = (string?)l.bairro, Cep = (string?)l.cep,
            Cidade = (string?)l.cidade, Uf = (string?)l.uf, Inscricao = (string?)l.inscr,
            Situacao = (string?)l.situacao, DataGravacao = (DateTime?)l.data_grav,
            SubstituicaoTributaria = (bool?)l.sub_trib, MovimentaEstoque = (bool?)l.Mov_Est
        }).ToList();
    }

    public async Task<int> Contar(FiltroListagem f, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        return await c.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM dbo.forne
             WHERE (@busca IS NULL OR RTRIM(codfor) LIKE '%' + @busca + '%'
                                   OR descri LIKE '%' + @busca + '%')", new { busca = f.Busca });
    }
}

/// <summary>FR-42: a trilha é append-only e só de leitura pela aplicação.</summary>
public sealed class AuditoriaConsulta(FabricaConexao fabrica) : IAuditoriaConsulta
{
    public async Task<IReadOnlyList<RegistroAuditoria>> Listar(FiltroListagem f, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var linhas = await c.QueryAsync<dynamic>(@"
            SELECT id, data_eve, usuario, arquivo, chave, val_ant, val_atu
              FROM dbo.log_even
             WHERE (@busca IS NULL OR usuario LIKE '%' + @busca + '%'
                                   OR arquivo LIKE '%' + @busca + '%'
                                   OR chave LIKE '%' + @busca + '%')
             ORDER BY id DESC
            OFFSET @off ROWS FETCH NEXT @tam ROWS ONLY",
            new { busca = f.Busca, off = f.Offset, tam = f.TamanhoEfetivo });

        return linhas.Select(l => new RegistroAuditoria(
            (int)l.id, (DateTime)l.data_eve, ((string?)l.usuario)?.Trim() ?? "",
            ((string?)l.arquivo)?.Trim() ?? "", ((string?)l.chave)?.Trim() ?? "",
            (string?)l.val_ant, (string?)l.val_atu)).ToList();
    }

    public async Task<int> Contar(FiltroListagem f, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        return await c.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM dbo.log_even
             WHERE (@busca IS NULL OR usuario LIKE '%' + @busca + '%'
                                   OR arquivo LIKE '%' + @busca + '%'
                                   OR chave LIKE '%' + @busca + '%')", new { busca = f.Busca });
    }
}
