namespace Mundial.Dominio;

/// <summary>
/// AD-10: o agregado. Identificado por filial + orig_des + tipo_doc + SERIE + numero.
/// Fechar age sobre todas as linhas de uma vez, em transação única.
/// </summary>
public sealed class Documento
{
    public required ChaveDocumento Chave { get; init; }
    public required string NumeroExibido { get; init; }   // conferencia.acesso, char(25) — o que o operador bipa
    public int? Doca { get; set; }
    public string? MatrConf { get; private set; }
    public string? MatrFec { get; private set; }
    public DateTime? DtHora { get; private set; }
    public bool Fechado { get; private set; }
    public string? CodigoFornecedor { get; init; }
    public string? NomeFornecedor { get; init; }
    public DateTime? DataMov { get; init; }
    public List<ItemConferencia> Itens { get; init; } = [];

    public int ItensLancados => Itens.Count(i => i.QtdRec > 0);
    public int ItensPendentes => Itens.Count(i => i.Pendencia);
    public bool TemDivergencia => Itens.Any(i => i.TemDivergencia);
    public char SituacaoAtual => Fechado ? Situacao.Fechada
        : ItensLancados > 0 ? Situacao.EmConferencia : Situacao.Aguardando;

    /// <summary>RK-ff51aa26bf33 / RK-69b41cd017dd — documento já conferido não abre para edição.</summary>
    [RegraNegocio("RK-ff51aa26bf33", "Este Documento já foi conferido!")]
    [RegraNegocio("RK-69b41cd017dd", "Este Documento já foi conferido!")]
    public ResultadoRegra AvaliarAbertura()
        => Fechado
            ? ResultadoRegra.Recusa("RK-69b41cd017dd", "Este Documento já foi conferido!")
            : ResultadoRegra.Ok;

    /// <summary>
    /// RK-cc8cfa3658d1 / RK-45e526801fea — documento já lançado avisa e pede confirmação.
    /// AD-6: é decisão do operador, nunca bloqueio de servidor.
    /// </summary>
    [RegraNegocio("RK-cc8cfa3658d1", "Este Documento já foi lançado!")]
    [RegraNegocio("RK-45e526801fea", "Este Documento já foi lançado!")]
    public ResultadoRegra AvaliarRelancamento()
        => ItensLancados > 0 && !Fechado
            ? ResultadoRegra.Confirma("RK-45e526801fea",
                "Este Documento já foi lançado!\nConfirma assim mesmo?")
            : ResultadoRegra.Ok;

    /// <summary>RK-a7f3c0eb65c1 — fornecedor diferente do esperado pede confirmação.</summary>
    [RegraNegocio("RK-a7f3c0eb65c1", "Fornecedor diferente!")]
    public ResultadoRegra AvaliarFornecedor(string? fornecedorEsperado)
        => !string.IsNullOrWhiteSpace(fornecedorEsperado)
           && !string.Equals(fornecedorEsperado, CodigoFornecedor, StringComparison.OrdinalIgnoreCase)
            ? ResultadoRegra.Confirma("RK-a7f3c0eb65c1",
                "Fornecedor diferente!\nConfirma este fornecedor?")
            : ResultadoRegra.Ok;

    /// <summary>
    /// RK-fa93a48fbecc — finalizar sempre pede uma confirmação.
    /// A-10: itens pendentes não bloqueiam; a confirmação informa quantos ficam pendentes.
    /// </summary>
    [RegraNegocio("RK-fa93a48fbecc", "Finalizar conferência?")]
    public ResultadoRegra AvaliarFechamento()
    {
        if (Fechado) return ResultadoRegra.Recusa("RK-69b41cd017dd", "Este Documento já foi conferido!");
        var aviso = ItensPendentes > 0
            ? $"Finalizar conferência?\n{ItensPendentes} item(ns) ficará(ão) pendente(s)."
            : "Finalizar conferência?";
        return ResultadoRegra.Confirma("RK-fa93a48fbecc", aviso);
    }

    /// <summary>AD-10: transição atômica sobre todas as linhas.</summary>
    public void Fechar(string matricula, DateTime agoraUtc)
    {
        if (Fechado) throw new InvalidOperationException("Documento já fechado.");
        Fechado = true;
        MatrFec = matricula;
        DtHora = agoraUtc;
        foreach (var item in Itens) item.Fechar();
    }

    public void RegistrarConferente(string matricula) => MatrConf ??= matricula;

    public void Reidratar(string? matrConf, string? matrFec, DateTime? dtHora, bool fechado)
    {
        MatrConf = matrConf; MatrFec = matrFec; DtHora = dtHora; Fechado = fechado;
    }
}
