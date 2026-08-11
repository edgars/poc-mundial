using Xunit;
using Mundial.Dominio;
using Mundial.Infraestrutura.Etiquetas;

namespace Mundial.Testes;

/// <summary>
/// FR-38 exige compatibilidade byte a byte com o legado: as coordenadas são contrato,
/// porque posição alterada invalida a leitura no armazém.
/// </summary>
public class RegrasEtiqueta
{
    private static Produto Cola => new()
    {
        Codigo = "04127", Descricao = "REFRIGERANTE COLA 2L", Embalagem = "CX c/ 6", EmbalagemQtd = 6,
        Ean = ["7891234567897", null, null], Dun = ["17891234567894", null, null]
    };

    [Fact(DisplayName = "RK-b382d85d0edc · a etiqueta abre com ^XA")]
    public void RK_b382d85d0edc_abre_com_XA()
        => Assert.StartsWith("^XA", new GeradorZpl().Gerar(Cola, "17891234567894"));

    [Fact(DisplayName = "RK-e8876989538a · a etiqueta fecha com ^XZ")]
    public void RK_e8876989538a_fecha_com_XZ()
        => Assert.EndsWith("^XZ", new GeradorZpl().Gerar(Cola, "17891234567894"));

    [Fact(DisplayName = "RK-0811a89bc8e6 · a descrição sai em ^FO510,40 com a fonte do legado")]
    public void RK_0811a89bc8e6_descricao_na_posicao_do_legado()
        => Assert.Contains("^FO510,40^A0R,150,36^FDREFRIGERANTE COLA 2L^FS",
            new GeradorZpl().Gerar(Cola, "17891234567894"));

    [Fact(DisplayName = "RK-2b3c11b27fef · embalagem e quantidade saem em ^FO420,360")]
    public void RK_2b3c11b27fef_embalagem_com_quantidade()
        => Assert.Contains("^FO420,360^A0R,100,50^FDCX c/ 6 c/ 6^FS",
            new GeradorZpl().Gerar(Cola, "17891234567894"));

    [Fact(DisplayName = "RK-3ff169d79617 · DUN-14 usa a posição ^FO220,270")]
    public void RK_3ff169d79617_dun14_em_220()
        => Assert.Contains("^FO220,270^BCR,200,Y,N,N^FD17891234567894^FS",
            new GeradorZpl().Gerar(Cola, "17891234567894"));

    [Fact(DisplayName = "RK-25721748a2b1 · EAN-13 usa a posição ^FO210,270")]
    public void RK_25721748a2b1_ean13_em_210()
        => Assert.Contains("^FO210,270^BCR,200,Y,N,N^FD7891234567897^FS",
            new GeradorZpl().Gerar(Cola, "7891234567897"));

    [Fact(DisplayName = "RK-1b386e3870da · o código legível sai em ^FO110,335")]
    public void RK_1b386e3870da_codigo_legivel()
        => Assert.Contains("^FO110,335^A0R,55,35^FD17891234567894^FS",
            new GeradorZpl().Gerar(Cola, "17891234567894"));

    [Fact(DisplayName = "FR-38 · a etiqueta inteira, byte a byte")]
    public void FR38_etiqueta_completa()
    {
        var esperado = string.Join("\n",
            "^XA",
            "^FO510,40^A0R,150,36^FDREFRIGERANTE COLA 2L^FS",
            "^FO420,360^A0R,100,50^FDCX c/ 6 c/ 6^FS",
            "^FO220,270^BCR,200,Y,N,N^FD17891234567894^FS",
            "^FO110,335^A0R,55,35^FD17891234567894^FS",
            "^XZ");
        Assert.Equal(esperado, new GeradorZpl().Gerar(Cola, "17891234567894"));
    }

    [Fact(DisplayName = "FR-38 · quantidade inteira não leva casas decimais, como no Trans() do FoxPro")]
    public void FR38_quantidade_inteira_sem_decimais()
        => Assert.Contains("c/ 6^FS", new GeradorZpl().Gerar(Cola, "17891234567894"));
}
