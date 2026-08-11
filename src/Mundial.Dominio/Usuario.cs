namespace Mundial.Dominio;

/// <summary>
/// `usuario` do legado. AD-7: a coluna `senha char(6)` em texto puro vira `senha_hash`,
/// e é a única renomeação que a fidelidade ao UIR autoriza.
/// </summary>
public sealed class Usuario
{
    public required string Matricula { get; init; }   // char(5)
    public required string Nome { get; init; }        // char(35), obrigatório — RK-d1a55f1103db
    public required string SenhaHash { get; init; }
    public string? NivelUsuario { get; init; }        // nchar(1)
    public string? Loja { get; init; }

    /// <summary>
    /// RK-8ffd715ce9ad — condição legada `vsenha < 3`.
    /// TODO(Q-2): ninguém documentou o que 1, 2 e 3 significam em usuario.niv_usu.
    /// O limiar fica aqui, num ponto único, até alguém da Mundial confirmar.
    /// </summary>
    public const int NivelMinimo = 3;

    [RegraNegocio("RK-8ffd715ce9ad", "Você não está autorizado a usar este Sistema")]
    public ResultadoRegra AvaliarAutorizacao()
    {
        var nivel = int.TryParse(NivelUsuario, out var n) ? n : 0;
        return nivel < NivelMinimo
            ? ResultadoRegra.Recusa("RK-8ffd715ce9ad", "Você não está autorizado a usar este Sistema")
            : ResultadoRegra.Ok;
    }
}

/// <summary>
/// `acesso(matric, arquivo)`. F-4: `arquivo` é o nome da TABELA, não da tela — confirmado no
/// readme do cliente e no DDL. Uma tela que toca N tabelas exige as N permissões (AD-8).
/// </summary>
public sealed class Acesso
{
    public required string Matricula { get; init; }
    public required string Tabela { get; init; }      // char(10)
    public required string Descricao { get; init; }   // char(30), obrigatório — RK-ea5a22eaf219
    public bool Consultar { get; init; }              // RK-04c918661d8d
    public bool Incluir { get; init; }                // RK-6022cae899fa
    public bool Alterar { get; init; }                // RK-fa1ca141cf21
    public bool Excluir { get; init; }                // RK-be780ff12c0e
}

public enum Operacao { Consultar, Incluir, Alterar, Excluir }

/// <summary>
/// `acesso.arquivo` é char(10), e "conferencia" tem 11 caracteres — o legado nunca conseguiu
/// guardar o nome inteiro. A chave de permissão é o nome da tabela truncado em 10.
/// Descoberto ao semear: o INSERT falhou com "String or binary data would be truncated".
/// TODO(Q-10): confirmar com a Mundial se o legado usa "conferenci" ou outro identificador.
/// </summary>
public static class Tabelas
{
    public const int TamanhoChave = 10;
    public static string Chave(string tabela) =>
        tabela.Length > TamanhoChave ? tabela[..TamanhoChave] : tabela;
}
