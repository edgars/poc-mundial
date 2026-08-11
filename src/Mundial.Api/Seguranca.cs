using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Mundial.Aplicacao;
using Mundial.Dominio;

namespace Mundial.Api;

/// <summary>
/// AD-7 e AD-8: o servidor é a autoridade. A matrícula vem do token, nunca do corpo da
/// requisição, e cada operação de escrita exige a permissão da tabela que ela toca.
/// </summary>
public static class Seguranca
{
    public const string Emissor = "mundial-conferencia";

    /// <summary>Uma claim por permissão: `perm:<tabela>:<operacao>`.</summary>
    public static string ClaimPermissao(string tabela, Operacao op)
        => $"perm:{Tabelas.Chave(tabela)}:{op.ToString().ToLowerInvariant()}";

    public static string GerarToken(UsuarioAutenticado usuario, string segredo, TimeSpan validade)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(segredo));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Matricula),
            new("nome", usuario.Nome)
        };

        foreach (var a in usuario.Permissoes)
        {
            if (a.Consultar) claims.Add(new Claim("perm", ClaimPermissao(a.Tabela, Operacao.Consultar)));
            if (a.Incluir) claims.Add(new Claim("perm", ClaimPermissao(a.Tabela, Operacao.Incluir)));
            if (a.Alterar) claims.Add(new Claim("perm", ClaimPermissao(a.Tabela, Operacao.Alterar)));
            if (a.Excluir) claims.Add(new Claim("perm", ClaimPermissao(a.Tabela, Operacao.Excluir)));
        }

        var token = new JwtSecurityToken(
            issuer: Emissor, audience: Emissor, claims: claims,
            expires: DateTime.UtcNow.Add(validade),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static TokenValidationParameters Validacao(string segredo) => new()
    {
        ValidIssuer = Emissor,
        ValidAudience = Emissor,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(segredo)),
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// As policies que os endpoints exigem. O nome é `<tabela>:<operacao>` — e a tabela é a chave
    /// truncada em 10, porque é assim que o legado guarda (F-9).
    /// </summary>
    public static void RegistrarPolicies(AuthorizationOptions o)
    {
        foreach (var tabela in new[] { "conferencia", "estoq", "forne", "usuario", "acesso", "log_even" })
            foreach (var op in Enum.GetValues<Operacao>())
            {
                var exigida = ClaimPermissao(tabela, op);
                o.AddPolicy($"{Tabelas.Chave(tabela)}:{op.ToString().ToLowerInvariant()}",
                    p => p.RequireAssertion(ctx => ctx.User.HasClaim("perm", exigida)));
            }
    }

    /// <summary>A matrícula de quem está operando — do token, nunca do que o cliente enviou.</summary>
    public static string Matricula(this ClaimsPrincipal usuario)
        => usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
           ?? throw new InvalidOperationException("Requisição sem identidade — endpoint desprotegido?");
}
