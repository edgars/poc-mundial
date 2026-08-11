using System.Data;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Mundial.Aplicacao;
using Mundial.Dominio;

namespace Mundial.Infraestrutura;

public sealed class Relogio : IRelogio
{
    public DateTime AgoraUtc => DateTime.UtcNow;   // AD-19: nenhum DateTime.Now no código
}

public sealed class FabricaConexao(string connectionString)
{
    public IDbConnection Abrir() => new SqlConnection(connectionString);
}

/// <summary>AD-7: PasswordHasher do ASP.NET Core Identity. Senha nunca em claro.</summary>
public sealed class HashSenha : IHashSenha
{
    private readonly PasswordHasher<string> _hasher = new();
    public string Gerar(string senha) => _hasher.HashPassword("mundial", senha);
    public bool Verificar(string senha, string hash)
        => _hasher.VerifyHashedPassword("mundial", hash, senha) != PasswordVerificationResult.Failed;
}

public sealed class UsuarioRepositorio(FabricaConexao fabrica) : IUsuarioRepositorio
{
    public async Task<Usuario?> PorMatricula(string matricula, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var linha = await c.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT matric, nome, senha_hash, niv_usu, loja FROM dbo.usuario WHERE matric = @m",
            new { m = matricula });
        if (linha is null) return null;
        return new Usuario
        {
            Matricula = (string)linha.matric,
            Nome = (string)linha.nome,
            SenhaHash = (string?)linha.senha_hash ?? "",
            NivelUsuario = (string?)linha.niv_usu,
            Loja = (string?)linha.loja
        };
    }
}

public sealed class AcessoRepositorio(FabricaConexao fabrica) : IAcessoRepositorio
{
    public async Task<IReadOnlyList<Acesso>> PorMatricula(string matricula, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var linhas = await c.QueryAsync<dynamic>(
            "SELECT matric, arquivo, descri, alterar, incluir, excluir, consultar FROM dbo.acesso WHERE matric = @m",
            new { m = matricula });
        return linhas.Select(l => new Acesso
        {
            Matricula = ((string)l.matric).Trim(),
            Tabela = ((string)l.arquivo).Trim(),
            Descricao = ((string)l.descri).Trim(),
            Alterar = (bool)l.alterar,
            Incluir = (bool)l.incluir,
            Excluir = (bool)l.excluir,
            Consultar = (bool)l.consultar
        }).ToList();
    }
}

public sealed class ProdutoConsulta(FabricaConexao fabrica) : IProdutoConsulta
{
    private const string Colunas =
        "codigo, descri, embalag, embalqt, codbarr, codbarr2, codbarr3, barr_emb, barr_emb2, barr_emb3";

    public async Task<IReadOnlyList<Produto>> PorCodigoDeBarras(string codigo, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        // O operador pode bipar EAN-13 (unidade) ou DUN-14 (embalagem); os dois resolvem.
        var linhas = await c.QueryAsync<dynamic>($@"
            SELECT {Colunas} FROM dbo.estoq
            WHERE RTRIM(codbarr) = @c OR RTRIM(codbarr2) = @c OR RTRIM(codbarr3) = @c
               OR RTRIM(barr_emb) = @c OR RTRIM(barr_emb2) = @c OR RTRIM(barr_emb3) = @c",
            new { c = codigo });
        return linhas.Select(Mapear).ToList();
    }

    public async Task<Produto?> PorCodigo(string codigo, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var l = await c.QuerySingleOrDefaultAsync<dynamic>(
            $"SELECT {Colunas} FROM dbo.estoq WHERE RTRIM(codigo) = @c", new { c = codigo });
        return l is null ? null : Mapear(l);
    }

    private static Produto Mapear(dynamic l) => new()
    {
        Codigo = ((string)l.codigo).Trim(),
        Descricao = ((string)l.descri).Trim(),
        Embalagem = ((string?)l.embalag)?.Trim(),
        EmbalagemQtd = (decimal?)l.embalqt,
        Ean = [((string?)l.codbarr)?.Trim(), ((string?)l.codbarr2)?.Trim(), ((string?)l.codbarr3)?.Trim()],
        Dun = [((string?)l.barr_emb)?.Trim(), ((string?)l.barr_emb2)?.Trim(), ((string?)l.barr_emb3)?.Trim()]
    };
}

/// <summary>Schema recuperado da função reg_log do legado (F-8).</summary>
public sealed class Auditoria(FabricaConexao fabrica, IRelogio relogio) : IAuditoria
{
    public async Task Registrar(string usuario, string tabela, string chave,
        string? valorAnterior, string? valorAtual, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        await c.ExecuteAsync(@"
            INSERT INTO dbo.log_even (data_eve, usuario, arquivo, chave, val_ant, val_atu)
            VALUES (@data, @usuario, @arquivo, @chave, @ant, @atu)",
            new { data = relogio.AgoraUtc, usuario, arquivo = tabela, chave, ant = valorAnterior, atu = valorAtual });
    }
}
