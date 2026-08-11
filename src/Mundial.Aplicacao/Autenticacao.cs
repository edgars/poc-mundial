using Mundial.Dominio;

namespace Mundial.Aplicacao;

public sealed record PedidoLogin(string Matricula, string Senha);
public sealed record UsuarioAutenticado(string Matricula, string Nome, IReadOnlyList<Acesso> Permissoes);

/// <summary>
/// RK-58fefec22db6 — o legado compara o campo de confirmação com o de senha e recusa quando
/// divergem: `Thisform.senha3.Value#This.Value And !Empty(Thisform.senha3.Value)`.
/// </summary>
public sealed class DefinirSenha(IHashSenha hash)
{
    [RegraNegocio("RK-58fefec22db6", "Você deve Confirmar a senha")]
    public (string? Hash, ResultadoRegra Resultado) Executar(string senha, string confirmacao)
    {
        if (string.IsNullOrEmpty(senha) || senha != confirmacao)
            return (null, ResultadoRegra.Recusa("RK-58fefec22db6", "Você deve Confirmar a senha"));
        return (hash.Gerar(senha), ResultadoRegra.Ok);
    }
}

public sealed class Autenticar(IUsuarioRepositorio usuarios, IAcessoRepositorio acessos, IHashSenha hash)
{
    /// <summary>
    /// RK-046f5592ef5b — matrícula inexistente.
    /// RK-f8293cf9dbb3 — senha inválida.
    /// RK-8ffd715ce9ad — nível insuficiente (via Usuario.AvaliarAutorizacao).
    /// </summary>
    [RegraNegocio("RK-046f5592ef5b", "Matrícula não cadastrada! Favor contactar supervisor")]
    [RegraNegocio("RK-f8293cf9dbb3", "Senha inválida")]
    public async Task<(UsuarioAutenticado? Usuario, ResultadoRegra Resultado)> Executar(
        PedidoLogin pedido, CancellationToken ct = default)
    {
        var usuario = await usuarios.PorMatricula(pedido.Matricula.Trim(), ct);
        if (usuario is null)
            return (null, ResultadoRegra.Recusa("RK-046f5592ef5b",
                "Matrícula não cadastrada! Favor contactar supervisor"));

        if (!hash.Verificar(pedido.Senha, usuario.SenhaHash))
            return (null, ResultadoRegra.Recusa("RK-f8293cf9dbb3", "Senha inválida"));

        var autorizacao = usuario.AvaliarAutorizacao();
        if (!autorizacao.Passou) return (null, autorizacao);

        var permissoes = await acessos.PorMatricula(usuario.Matricula.Trim(), ct);
        return (new UsuarioAutenticado(usuario.Matricula.Trim(), usuario.Nome.Trim(), permissoes),
                ResultadoRegra.Ok);
    }
}
