namespace Mundial.Dominio;

/// <summary>
/// AD-5: toda regra recuperada do legado cita sua chave estável do RNC.
/// Verificável com getRule(workspaceId, chave) antes de fechar a story.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class RegraNegocioAttribute(string chave, string mensagem) : Attribute
{
    public string Chave { get; } = chave;
    public string Mensagem { get; } = mensagem;
}
