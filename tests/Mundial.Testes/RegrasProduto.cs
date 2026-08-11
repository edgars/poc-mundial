using Xunit;
using Mundial.Dominio;

namespace Mundial.Testes;

public class RegrasProduto
{
    private static Produto Prod(string? d1 = null, string? d2 = null, string? d3 = null,
                                string? e1 = "7891234567897")
        => new()
        {
            Codigo = "04127", Descricao = "REFRIGERANTE COLA 2L",
            Embalagem = "CX c/ 6", EmbalagemQtd = 6,
            Dun = [d1, d2, d3], Ean = [e1, null, null]
        };

    [Fact(DisplayName = "RK-99e9bfdcea75 · o mesmo código não pode repetir entre os três slots de embalagem")]
    public void RK_99e9bfdcea75_duplicidade_interna_recusa()
    {
        var p = Prod(d1: "17891234567894", d2: "17891234500010");
        var r = p.AvaliarDuplicidadeInterna(2, "17891234567894");
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Contains("Este Código já esta cadastrado", r.Mensagem);
    }

    [Fact(DisplayName = "RK-4ca8df36a760 · código diferente dos outros dois slots é aceito")]
    public void RK_4ca8df36a760_codigo_novo_aceito()
        => Assert.True(Prod(d1: "17891234567894").AvaliarDuplicidadeInterna(1, "17891234500010").Passou);

    [Fact(DisplayName = "RK-ab62193a2b2d · slot vazio não dispara duplicidade")]
    public void RK_ab62193a2b2d_vazio_nao_conflita()
        => Assert.True(Prod(d1: "17891234567894").AvaliarDuplicidadeInterna(2, "").Passou);

    [Fact(DisplayName = "RK-a0bb1eeee55d · comparação de duplicidade ignora caixa")]
    public void RK_a0bb1eeee55d_duplicidade_ignora_caixa()
        => Assert.Equal(TipoResultado.Recusado,
            Prod(d1: "abc12345678901").AvaliarDuplicidadeInterna(1, "ABC12345678901").Tipo);

    [Fact(DisplayName = "RK-f9e0b12a76af · o próprio slot não conflita consigo mesmo")]
    public void RK_f9e0b12a76af_proprio_slot_nao_conflita()
        => Assert.True(Prod(d1: "17891234567894").AvaliarDuplicidadeInterna(0, "17891234567894").Passou);

    [Fact(DisplayName = "RK-41493150036e · duplicidade é avaliada nos três slots")]
    public void RK_41493150036e_avalia_os_tres_slots()
    {
        var p = Prod(d1: "11111111111111", d2: "22222222222222", d3: "33333333333333");
        Assert.Equal(TipoResultado.Recusado, p.AvaliarDuplicidadeInterna(0, "33333333333333").Tipo);
        Assert.Equal(TipoResultado.Recusado, p.AvaliarDuplicidadeInterna(1, "11111111111111").Tipo);
        Assert.Equal(TipoResultado.Recusado, p.AvaliarDuplicidadeInterna(2, "22222222222222").Tipo);
    }

    [Fact(DisplayName = "RK-5b2436bca3f0 · apagar um código preenchido pede confirmação")]
    public void RK_5b2436bca3f0_exclusao_pede_confirmacao()
    {
        var r = Prod(d1: "17891234567894").AvaliarExclusao(0, "");
        Assert.Equal(TipoResultado.ExigeConfirmacao, r.Tipo);
        Assert.Equal("Tem certeza que deseja excluir este código?", r.Mensagem);
    }

    [Fact(DisplayName = "RK-2c78478f0b97 · apagar slot que já estava vazio não pergunta nada")]
    public void RK_2c78478f0b97_slot_vazio_nao_pergunta()
        => Assert.True(Prod().AvaliarExclusao(1, "").Passou);

    [Fact(DisplayName = "RK-9f92b8e2a3c0 · trocar o valor, sem esvaziar, não é exclusão")]
    public void RK_9f92b8e2a3c0_troca_nao_e_exclusao()
        => Assert.True(Prod(d3: "17891234567894").AvaliarExclusao(2, "17891234500010").Passou);

    [Fact(DisplayName = "RK-ade9dd1661d1 · a confirmação de exclusão traz o texto literal do legado")]
    public void RK_ade9dd1661d1_texto_literal()
        => Assert.Equal("Tem certeza que deseja excluir este código?",
            Prod(d1: "17891234567894").AvaliarExclusao(0, null).Mensagem);

    [Fact(DisplayName = "RK-305af19071c6 · exclusão no segundo slot também confirma")]
    public void RK_305af19071c6_segundo_slot_confirma()
        => Assert.Equal(TipoResultado.ExigeConfirmacao, Prod(d2: "17891234500010").AvaliarExclusao(1, "").Tipo);

    [Fact(DisplayName = "RK-21ac9f1bddea · exclusão no terceiro slot também confirma")]
    public void RK_21ac9f1bddea_terceiro_slot_confirma()
        => Assert.Equal(TipoResultado.ExigeConfirmacao, Prod(d3: "17891234511016").AvaliarExclusao(2, "").Tipo);

    [Fact(DisplayName = "RK-3b8ef53b6cf2 · EAN que não pertence ao produto é recusado")]
    public void RK_3b8ef53b6cf2_ean_de_outro_produto_recusa()
    {
        var r = Prod(e1: "7891234567897").AvaliarEanDoProduto("7899999000123");
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Equal("Código EAN não é desse DUN-14!", r.Mensagem);
    }

    [Fact(DisplayName = "RK-3b8ef53b6cf2 · EAN do próprio produto é aceito")]
    public void RK_3b8ef53b6cf2_ean_proprio_aceito()
        => Assert.True(Prod(e1: "7891234567897").AvaliarEanDoProduto("7891234567897").Passou);
}

public class RegrasAcesso
{
    private static Usuario Usr(string? nivel) => new()
    {
        Matricula = "04127", Nome = "CLEBER SANTOS", SenhaHash = "x", NivelUsuario = nivel
    };

    [Fact(DisplayName = "RK-8ffd715ce9ad · nível abaixo de 3 não entra no sistema")]
    public void RK_8ffd715ce9ad_nivel_insuficiente_recusa()
    {
        var r = Usr("1").AvaliarAutorizacao();
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Equal("Você não está autorizado a usar este Sistema", r.Mensagem);
    }

    [Fact(DisplayName = "RK-8ffd715ce9ad · nível 3 entra")]
    public void RK_8ffd715ce9ad_nivel_suficiente_passa()
        => Assert.True(Usr("3").AvaliarAutorizacao().Passou);

    [Fact(DisplayName = "Q-2 · nível ausente ou ilegível é tratado como insuficiente")]
    public void Q2_nivel_ilegivel_e_insuficiente()
    {
        Assert.Equal(TipoResultado.Recusado, Usr(null).AvaliarAutorizacao().Tipo);
        Assert.Equal(TipoResultado.Recusado, Usr("x").AvaliarAutorizacao().Tipo);
    }

    [Fact(DisplayName = "F-9 · a chave de permissão trunca o nome da tabela em 10 caracteres")]
    public void F9_chave_de_permissao_trunca()
    {
        // acesso.arquivo é char(10) e "conferencia" tem 11 — o legado nunca guardou o nome inteiro
        Assert.Equal("conferenci", Tabelas.Chave("conferencia"));
        Assert.Equal("estoq", Tabelas.Chave("estoq"));
        Assert.Equal("log_even", Tabelas.Chave("log_even"));
    }
}

public class RegrasTransicaoBarrEmb3
{
    private static Produto Prod(string? d3) => new()
    {
        Codigo = "04127", Descricao = "REFRIGERANTE COLA 2L",
        Dun = ["17891234567894", null, d3], Ean = ["7891234567897", null, null]
    };

    [Fact(DisplayName = "RK-9f4468b42859 · terceiro slot vazio é a transição barr_emb3 = ''")]
    public void RK_9f4468b42859_terceiro_slot_vazio() => Assert.True(Prod(null).TerceiroSlotVazio());

    [Fact(DisplayName = "RK-75e2169fe930 · terceiro slot só com espaços também conta como vazio")]
    public void RK_75e2169fe930_espacos_contam_como_vazio() => Assert.True(Prod("   ").TerceiroSlotVazio());

    [Fact(DisplayName = "RK-dfe2ca45ec1a · terceiro slot preenchido não está na transição")]
    public void RK_dfe2ca45ec1a_slot_preenchido() => Assert.False(Prod("17891234511016").TerceiroSlotVazio());
}
