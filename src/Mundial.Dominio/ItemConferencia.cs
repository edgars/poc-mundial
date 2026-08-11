namespace Mundial.Dominio;

/// <summary>
/// Uma linha de `conferencia` — o documento mais o código do produto.
/// AD-10: a PK composta inclui `codigo`, então cada linha é um item da nota, não a nota inteira.
/// Estas linhas nascem da integração da nota fiscal; a aplicação nunca as cria (AD-14).
/// </summary>
public sealed class ItemConferencia
{
    public required ChaveDocumento Documento { get; init; }
    public required string Codigo { get; init; }          // char(5), casa com estoq.CODIGO
    public string? Dun14 { get; set; }                    // char(14)
    public string? Descricao { get; set; }                // vem de estoq.descri, para exibição
    /// <summary>AD-17: a versão lida, para o UPDATE detectar escrita concorrente.</summary>
    public byte[]? Versao { get; set; }
    public decimal? ItNf { get; init; }                   // número do item na nota
    public decimal QtdNf { get; init; }
    public decimal QtdRec { get; private set; }
    public decimal QtdUnidNf { get; init; }
    public decimal QtdUnidRec { get; private set; }
    public bool Pendencia { get; private set; }
    public char SituacaoAtual { get; private set; } = Situacao.Aguardando;

    /// <summary>Diferença entre o que a nota diz e o que chegou. Dado, não erro (FR-20).</summary>
    public decimal Divergencia => QtdRec - QtdNf;
    public bool TemDivergencia => QtdRec > 0 && Divergencia != 0;

    /// <summary>
    /// RK-8233e231d6fb — o legado avisa quando o item já tem quantidade lançada.
    /// AD-17: o lançamento substitui; por isso a confirmação existe. Somar não seria destrutivo.
    /// </summary>
    [RegraNegocio("RK-8233e231d6fb", "Este Código já tem Qtde lançada (")]
    [RegraNegocio("RK-5960908935ee", "Este Código já tem Qtde lançada (")]
    public ResultadoRegra AvaliarLancamento()
        => QtdRec > 0
            ? ResultadoRegra.Confirma("RK-8233e231d6fb",
                $"Este Código já tem Qtde lançada ({QtdRec:0.###})!\nDeseja lança-lo assim mesmo?")
            : ResultadoRegra.Ok;

    /// <summary>AD-17: substitui, nunca acumula. TODO(Q-1): sem confirmação humana.</summary>
    public void Lancar(decimal quantidade, decimal quantidadeUnidade)
    {
        if (quantidade < 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        QtdRec = quantidade;
        QtdUnidRec = quantidadeUnidade;
        Pendencia = false;
        SituacaoAtual = Situacao.EmConferencia;
    }

    public void LimparLancamento()
    {
        QtdRec = 0;
        QtdUnidRec = 0;
    }

    /// <summary>Código bipado não existe: o item fica pendente e o operador segue (UJ-2).</summary>
    public void MarcarPendente() => Pendencia = true;

    internal void Fechar() => SituacaoAtual = Situacao.Fechada;

    public static ItemConferencia Reidratar(
        ChaveDocumento doc, string codigo, string? dun14, decimal? itNf,
        decimal qtdNf, decimal qtdRec, decimal qtdUnidNf, decimal qtdUnidRec,
        bool pendencia, char situacao, string? descricao = null, byte[]? versao = null) => new()
        {
            Documento = doc, Codigo = codigo, Dun14 = dun14, ItNf = itNf, Descricao = descricao, Versao = versao,
            QtdNf = qtdNf, QtdUnidNf = qtdUnidNf,
            QtdRec = qtdRec, QtdUnidRec = qtdUnidRec,
            Pendencia = pendencia, SituacaoAtual = situacao
        };
}
