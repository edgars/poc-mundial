namespace Mundial.Dominio;

/// <summary>
/// Uma linha de `estoq` — a tabela mestre de produtos. O DBF real tem 116 colunas;
/// a tela de cadastro do legado edita seis delas (AD-3).
/// Três slots de EAN-13 (unidade de venda) e três de DUN-14 (embalagem).
/// </summary>
public sealed class Produto
{
    public required string Codigo { get; init; }        // char(5)
    public required string Descricao { get; set; }      // char(60)
    public string? Embalagem { get; set; }              // char(10)
    public decimal? EmbalagemQtd { get; set; }          // numeric(9,4)
    public string?[] Ean { get; init; } = new string?[3];       // CODBARR, CODBARR2, CODBARR3
    public string?[] Dun { get; init; } = new string?[3];       // BARR_EMB, BARR_EMB2, BARR_EMB3

    public IEnumerable<string> DunPreenchidos => Dun.Where(d => !string.IsNullOrWhiteSpace(d))!;
    public IEnumerable<string> EanPreenchidos => Ean.Where(e => !string.IsNullOrWhiteSpace(e))!;

    /// <summary>
    /// RK-a0bb1eeee55d / RK-99e9bfdcea75 / RK-f9e0b12a76af / RK-4ca8df36a760 /
    /// RK-41493150036e / RK-ab62193a2b2d — o mesmo código não pode repetir entre os três slots.
    /// A condição legada compara o valor contra os outros dois campos do próprio registro.
    /// </summary>
    [RegraNegocio("RK-a0bb1eeee55d", "Este Código já esta cadastrado!")]
    [RegraNegocio("RK-99e9bfdcea75", "Este Código já esta cadastrado!")]
    [RegraNegocio("RK-f9e0b12a76af", "Este Código já esta cadastrado")]
    [RegraNegocio("RK-4ca8df36a760", "Este Código já esta cadastrado")]
    [RegraNegocio("RK-41493150036e", "Este Código já esta cadastrado")]
    [RegraNegocio("RK-ab62193a2b2d", "Este Código já esta cadastrado")]
    public ResultadoRegra AvaliarDuplicidadeInterna(int slot, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return ResultadoRegra.Ok;
        for (var i = 0; i < Dun.Length; i++)
        {
            if (i == slot) continue;
            if (string.Equals(Dun[i], valor, StringComparison.OrdinalIgnoreCase))
                return ResultadoRegra.Recusa(ChaveDuplicidade(slot),
                    slot == 0 ? "Este Código já esta cadastrado!" : "Este Código já esta cadastrado");
        }
        return ResultadoRegra.Ok;
    }

    private static string ChaveDuplicidade(int slot) => slot switch
    {
        0 => "RK-99e9bfdcea75",
        1 => "RK-4ca8df36a760",
        _ => "RK-ab62193a2b2d"
    };

    /// <summary>
    /// RK-5b2436bca3f0 / RK-2c78478f0b97 / RK-9f92b8e2a3c0 e as confirmações
    /// RK-ade9dd1661d1 / RK-305af19071c6 / RK-21ac9f1bddea — apagar código pede confirmação.
    /// Condição legada: `muda and empty(this.value) and val(estoq.barr_emb)>0`.
    /// </summary>
    [RegraNegocio("RK-5b2436bca3f0", "Tem certeza que deseja excluir este código?")]
    [RegraNegocio("RK-2c78478f0b97", "Tem certeza que deseja excluir este código?")]
    [RegraNegocio("RK-9f92b8e2a3c0", "Tem certeza que deseja excluir este código?")]
    [RegraNegocio("RK-ade9dd1661d1", "Tem certeza que deseja excluir este código?")]
    [RegraNegocio("RK-305af19071c6", "Tem certeza que deseja excluir este código?")]
    [RegraNegocio("RK-21ac9f1bddea", "Tem certeza que deseja excluir este código?")]
    public ResultadoRegra AvaliarExclusao(int slot, string? novoValor)
        => string.IsNullOrWhiteSpace(novoValor) && !string.IsNullOrWhiteSpace(Dun[slot])
            ? ResultadoRegra.Confirma(ChaveExclusao(slot), "Tem certeza que deseja excluir este código?")
            : ResultadoRegra.Ok;

    private static string ChaveExclusao(int slot) => slot switch
    {
        0 => "RK-5b2436bca3f0",
        1 => "RK-2c78478f0b97",
        _ => "RK-9f92b8e2a3c0"
    };

    /// <summary>
    /// RK-9f4468b42859 / RK-75e2169fe930 / RK-dfe2ca45ec1a — no legado, esvaziar qualquer um dos
    /// três slots dispara a transição `barr_emb3 = ''`. As três regras compartilham a condição
    /// porque o formulário reavalia o terceiro campo a cada mudança nos outros dois.
    /// </summary>
    [RegraNegocio("RK-9f4468b42859", "Transicao de estado: barr_emb3 = ''")]
    [RegraNegocio("RK-75e2169fe930", "Transicao de estado: barr_emb3 = ''")]
    [RegraNegocio("RK-dfe2ca45ec1a", "Transicao de estado: barr_emb3 = ''")]
    public bool TerceiroSlotVazio() => string.IsNullOrWhiteSpace(Dun[2]);

    /// <summary>
    /// RK-3b8ef53b6cf2 — o EAN bipado precisa pertencer a este produto.
    /// Condição legada compara contra produto.codbarr, codbarr2 e codbarr3.
    /// </summary>
    [RegraNegocio("RK-3b8ef53b6cf2", "Código EAN não é desse DUN-14!")]
    public ResultadoRegra AvaliarEanDoProduto(string ean)
        => EanPreenchidos.Any(e => string.Equals(e, ean, StringComparison.OrdinalIgnoreCase))
            ? ResultadoRegra.Ok
            : ResultadoRegra.Recusa("RK-3b8ef53b6cf2", "Código EAN não é desse DUN-14!");
}
