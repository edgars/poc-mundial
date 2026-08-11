using Mundial.Dominio;

namespace Mundial.Infraestrutura.Etiquetas;

/// <summary>
/// AD-6: as 7 regras de ZPL não são validação — são um requisito de impressão, e vivem aqui.
///
/// A montagem reproduz o legado comando a comando. FR-38 exige compatibilidade byte a byte:
/// posição alterada invalida a leitura no armazém, então as coordenadas são as do FoxPro.
///
/// O legado escolhe entre duas posições de código de barras (^FO210 e ^FO220) conforme o
/// comprimento do código — 13 dígitos ficam em 210, 14 em 220.
/// </summary>
public sealed class GeradorZpl
{
    [RegraNegocio("RK-b382d85d0edc", "campo = '^XA'")]
    [RegraNegocio("RK-0811a89bc8e6", "^FO510,40 descrição do produto")]
    [RegraNegocio("RK-2b3c11b27fef", "^FO420,360 embalagem com quantidade")]
    [RegraNegocio("RK-25721748a2b1", "^FO210,270 código de barras")]
    [RegraNegocio("RK-3ff169d79617", "^FO220,270 código de barras")]
    [RegraNegocio("RK-1b386e3870da", "^FO110,335 código legível")]
    [RegraNegocio("RK-e8876989538a", "campo = '^XZ'")]
    public string Gerar(Produto produto, string codigoBarras)
    {
        var descricao = produto.Descricao.Trim();
        var embalagem = produto.Embalagem?.Trim() ?? "";
        var quantidade = Trans(produto.EmbalagemQtd);
        var codigo = codigoBarras.Trim();

        var linhas = new List<string>
        {
            "^XA",                                                          // RK-b382d85d0edc
            $"^FO510,40^A0R,150,36^FD{descricao}^FS",                       // RK-0811a89bc8e6
            $"^FO420,360^A0R,100,50^FD{embalagem} c/ {quantidade}^FS",      // RK-2b3c11b27fef
            // RK-25721748a2b1 (13 dígitos) e RK-3ff169d79617 (14 dígitos)
            codigo.Length <= CodigoEan13.Tamanho
                ? $"^FO210,270^BCR,200,Y,N,N^FD{codigo}^FS"
                : $"^FO220,270^BCR,200,Y,N,N^FD{codigo}^FS",
            $"^FO110,335^A0R,55,35^FD{codigo}^FS",                          // RK-1b386e3870da
            "^XZ"                                                           // RK-e8876989538a
        };

        return string.Join("\n", linhas);
    }

    /// <summary>
    /// Reproduz o Trans() do FoxPro para a quantidade da embalagem: inteiro sem casas decimais,
    /// fracionado com as casas que tiver. `embalqt` é numeric(9,4) e quase sempre é inteiro.
    /// </summary>
    private static string Trans(decimal? valor)
    {
        if (valor is null) return "";
        var v = valor.Value;
        return v == decimal.Truncate(v)
            ? decimal.Truncate(v).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : v.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
    }
}
