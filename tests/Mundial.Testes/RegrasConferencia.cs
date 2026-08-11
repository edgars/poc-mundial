using Xunit;
using Mundial.Dominio;

namespace Mundial.Testes;

/// <summary>
/// AD-20: cada regra tem ao menos um teste que cita o mesmo ruleKey no nome.
/// O domínio é testado sem banco (AD-1).
/// </summary>
public class RegrasConferencia
{
    private static Documento Doc(bool fechado = false, params (string codigo, decimal nf, decimal rec)[] itens)
    {
        var chave = new ChaveDocumento("00001", "00110", "NFE", "1", "000148372");
        var d = new Documento { Chave = chave, NumeroExibido = "000148372/1", CodigoFornecedor = "00110" };
        foreach (var (codigo, nf, rec) in itens)
            d.Itens.Add(ItemConferencia.Reidratar(chave, codigo, null, 1, nf, rec, nf, rec, false,
                rec > 0 ? Situacao.EmConferencia : Situacao.Aguardando));
        if (fechado) d.Fechar("04127", new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));
        return d;
    }

    [Fact(DisplayName = "RK-69b41cd017dd · documento fechado recusa abertura para edição")]
    public void RK_69b41cd017dd_documento_fechado_recusa_abertura()
    {
        var r = Doc(true, ("04127", 40, 40)).AvaliarAbertura();
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Equal("Este Documento já foi conferido!", r.Mensagem);
    }

    [Fact(DisplayName = "RK-ff51aa26bf33 · documento aberto passa na avaliação de abertura")]
    public void RK_ff51aa26bf33_documento_aberto_passa()
        => Assert.True(Doc(false, ("04127", 40, 0)).AvaliarAbertura().Passou);

    [Fact(DisplayName = "RK-45e526801fea · documento já lançado pede confirmação, não bloqueia")]
    public void RK_45e526801fea_ja_lancado_pede_confirmacao()
    {
        var r = Doc(false, ("04127", 40, 40)).AvaliarRelancamento();
        Assert.Equal(TipoResultado.ExigeConfirmacao, r.Tipo);
        Assert.Contains("Este Documento já foi lançado!", r.Mensagem);
    }

    [Fact(DisplayName = "RK-cc8cfa3658d1 · documento sem lançamento não avisa relançamento")]
    public void RK_cc8cfa3658d1_sem_lancamento_nao_avisa()
        => Assert.True(Doc(false, ("04127", 40, 0)).AvaliarRelancamento().Passou);

    [Fact(DisplayName = "RK-a7f3c0eb65c1 · fornecedor diferente pede confirmação")]
    public void RK_a7f3c0eb65c1_fornecedor_diferente_pede_confirmacao()
    {
        var r = Doc(false, ("04127", 40, 0)).AvaliarFornecedor("00999");
        Assert.Equal(TipoResultado.ExigeConfirmacao, r.Tipo);
        Assert.Contains("Fornecedor diferente!", r.Mensagem);
    }

    [Fact(DisplayName = "RK-fa93a48fbecc · finalizar sempre pede uma confirmação")]
    public void RK_fa93a48fbecc_finalizar_pede_confirmacao()
    {
        var r = Doc(false, ("04127", 40, 40)).AvaliarFechamento();
        Assert.Equal(TipoResultado.ExigeConfirmacao, r.Tipo);
        Assert.Contains("Finalizar conferência?", r.Mensagem);
    }

    [Fact(DisplayName = "A-10 · fechar com item pendente avisa quantos ficam pendentes, e não bloqueia")]
    public void A10_fechamento_com_pendencia_avisa_e_permite()
    {
        var doc = Doc(false, ("04127", 40, 40), ("05877", 24, 0));
        doc.Itens[1].MarcarPendente();
        var r = doc.AvaliarFechamento();
        Assert.Equal(TipoResultado.ExigeConfirmacao, r.Tipo);
        Assert.Contains("1 item(ns) ficará(ão) pendente(s)", r.Mensagem);
    }

    [Fact(DisplayName = "RK-8233e231d6fb · item com quantidade lançada exige confirmação, com o valor na mensagem")]
    public void RK_8233e231d6fb_item_com_quantidade_exige_confirmacao()
    {
        var doc = Doc(false, ("04127", 40, 40));
        var r = doc.Itens[0].AvaliarLancamento();
        Assert.Equal(TipoResultado.ExigeConfirmacao, r.Tipo);
        Assert.Contains("(40)", r.Mensagem);
    }

    [Fact(DisplayName = "RK-5960908935ee · item zerado aceita lançamento sem confirmação")]
    public void RK_5960908935ee_item_zerado_aceita_direto()
        => Assert.True(Doc(false, ("04127", 40, 0)).Itens[0].AvaliarLancamento().Passou);

    [Fact(DisplayName = "AD-17 · lançar substitui a quantidade, nunca acumula")]
    public void AD17_lancamento_substitui()
    {
        var doc = Doc(false, ("04127", 40, 40));
        doc.Itens[0].Lancar(38, 38);
        Assert.Equal(38, doc.Itens[0].QtdRec);   // não 78
        Assert.Equal(-2, doc.Itens[0].Divergencia);
    }

    [Fact(DisplayName = "FR-20 · divergência é a diferença entre nota e recebido, e é dado, não erro")]
    public void FR20_divergencia_e_dado()
    {
        var doc = Doc(false, ("04982", 120, 114));
        Assert.True(doc.Itens[0].TemDivergencia);
        Assert.Equal(-6, doc.Itens[0].Divergencia);
        Assert.True(doc.TemDivergencia);
    }

    [Fact(DisplayName = "AD-10 · fechar age sobre todas as linhas do documento de uma vez")]
    public void AD10_fechamento_atinge_todas_as_linhas()
    {
        var doc = Doc(false, ("04127", 40, 40), ("04982", 120, 114), ("05310", 60, 60));
        doc.Fechar("04310", new DateTime(2026, 8, 10, 23, 30, 0, DateTimeKind.Utc));
        Assert.True(doc.Fechado);
        Assert.All(doc.Itens, i => Assert.Equal(Situacao.Fechada, i.SituacaoAtual));
        Assert.Equal("04310", doc.MatrFec);
    }

    [Fact(DisplayName = "NFR-10 · documento fechado não fecha de novo")]
    public void NFR10_documento_fechado_e_imutavel()
    {
        var doc = Doc(true, ("04127", 40, 40));
        Assert.Throws<InvalidOperationException>(() =>
            doc.Fechar("04310", new DateTime(2026, 8, 10, 23, 30, 0, DateTimeKind.Utc)));
    }

    [Fact(DisplayName = "A-9 · situacao usa A aguardando, C em conferência, F fechada")]
    public void A9_convencao_de_situacao()
    {
        var doc = Doc(false, ("04127", 40, 0));
        Assert.Equal(Situacao.Aguardando, doc.SituacaoAtual);
        doc.Itens[0].Lancar(40, 40);
        Assert.Equal(Situacao.EmConferencia, doc.SituacaoAtual);
        doc.Fechar("04127", DateTime.UtcNow);
        Assert.Equal(Situacao.Fechada, doc.SituacaoAtual);
    }
}
