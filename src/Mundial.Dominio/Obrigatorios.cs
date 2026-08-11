namespace Mundial.Dominio;

/// <summary>
/// Campos NOT NULL que o legado exige fora de `forne`, cada um com sua chave.
/// FR-7 (usuario/acesso) e FR-21 (conferencia).
/// </summary>
public static class Obrigatorios
{
    [RegraNegocio("RK-d1a55f1103db", "nome is required")]
    [RegraNegocio("RK-ea5a22eaf219", "descri is required")]
    public static IReadOnlyList<ResultadoRegra> Usuario(string? nome, string? descricaoAcesso)
    {
        var faltas = new List<ResultadoRegra>();
        if (string.IsNullOrWhiteSpace(nome))
            faltas.Add(ResultadoRegra.Recusa("RK-d1a55f1103db", "nome is required"));
        if (string.IsNullOrWhiteSpace(descricaoAcesso))
            faltas.Add(ResultadoRegra.Recusa("RK-ea5a22eaf219", "descri is required"));
        return faltas;
    }

    /// <summary>As quatro flags de `acesso` são NOT NULL no DDL — ausência é negação.</summary>
    [RegraNegocio("RK-fa1ca141cf21", "alterar is required")]
    [RegraNegocio("RK-6022cae899fa", "incluir is required")]
    [RegraNegocio("RK-be780ff12c0e", "excluir is required")]
    [RegraNegocio("RK-04c918661d8d", "consultar is required")]
    public static IReadOnlyList<ResultadoRegra> Acesso(bool? alterar, bool? incluir, bool? excluir, bool? consultar)
    {
        var faltas = new List<ResultadoRegra>();
        if (alterar is null) faltas.Add(ResultadoRegra.Recusa("RK-fa1ca141cf21", "alterar is required"));
        if (incluir is null) faltas.Add(ResultadoRegra.Recusa("RK-6022cae899fa", "incluir is required"));
        if (excluir is null) faltas.Add(ResultadoRegra.Recusa("RK-be780ff12c0e", "excluir is required"));
        if (consultar is null) faltas.Add(ResultadoRegra.Recusa("RK-04c918661d8d", "consultar is required"));
        return faltas;
    }

    [RegraNegocio("RK-82c929f4e851", "peso_bruto_col is required")]
    [RegraNegocio("RK-c5a64175c9a1", "balanca is required")]
    [RegraNegocio("RK-16bc1acd7b74", "situacao is required")]
    public static IReadOnlyList<ResultadoRegra> Conferencia(decimal? pesoBrutoCol, bool? balanca, char? situacao)
    {
        var faltas = new List<ResultadoRegra>();
        if (pesoBrutoCol is null) faltas.Add(ResultadoRegra.Recusa("RK-82c929f4e851", "peso_bruto_col is required"));
        if (balanca is null) faltas.Add(ResultadoRegra.Recusa("RK-c5a64175c9a1", "balanca is required"));
        if (situacao is null or ' ') faltas.Add(ResultadoRegra.Recusa("RK-16bc1acd7b74", "situacao is required"));
        return faltas;
    }
}
