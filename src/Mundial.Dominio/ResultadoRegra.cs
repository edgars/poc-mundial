namespace Mundial.Dominio;

/// <summary>Como uma regra do legado se manifesta na aplicação nova.</summary>
public enum TipoResultado
{
    /// <summary>Passou. Segue o fluxo.</summary>
    Aceito,
    /// <summary>Bloqueia. O legado não deixava continuar.</summary>
    Recusado,
    /// <summary>O legado perguntava ao operador. AD-6: nunca bloqueia no servidor.</summary>
    ExigeConfirmacao
}

public sealed record ResultadoRegra(TipoResultado Tipo, string? Chave = null, string? Mensagem = null)
{
    public static readonly ResultadoRegra Ok = new(TipoResultado.Aceito);
    public static ResultadoRegra Recusa(string chave, string mensagem) => new(TipoResultado.Recusado, chave, mensagem);
    public static ResultadoRegra Confirma(string chave, string mensagem) => new(TipoResultado.ExigeConfirmacao, chave, mensagem);

    public bool Passou => Tipo == TipoResultado.Aceito;
}
