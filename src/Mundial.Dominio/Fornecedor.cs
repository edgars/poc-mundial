namespace Mundial.Dominio;

/// <summary>
/// `forne` do legado — 46 colunas, treze delas obrigatórias.
/// FR-35: o POC não tem tela de cadastro, mas as regras ficam implementadas e testadas.
/// </summary>
public sealed class Fornecedor
{
    public required string Codigo { get; init; }      // codfor char(5)
    public string? Descricao { get; init; }
    public string? Cgc { get; init; }
    public string? CodCom { get; init; }
    public string? Categoria { get; init; }
    public string? TipoLogradouro { get; init; }
    public string? Logradouro { get; init; }
    public string? Bairro { get; init; }
    public string? Cep { get; init; }
    public string? Cidade { get; init; }
    public string? Uf { get; init; }
    public string? Inscricao { get; init; }
    public string? Situacao { get; init; }
    public DateTime? DataGravacao { get; init; }
    public bool? SubstituicaoTributaria { get; init; }
    public bool? MovimentaEstoque { get; init; }

    /// <summary>
    /// Os treze campos que o legado marca NOT NULL, cada um com sua chave.
    /// A ordem é a do DDL, para casar com a leitura de quem confere contra a fonte.
    /// </summary>
    [RegraNegocio("RK-b3e7fcc26f3e", "cgc is required")]
    [RegraNegocio("RK-ef82abb7456c", "cod_com is required")]
    [RegraNegocio("RK-b5da8c743238", "categ is required")]
    [RegraNegocio("RK-e74f29d4f922", "tiplog is required")]
    [RegraNegocio("RK-2ce1876d83ad", "lograd is required")]
    [RegraNegocio("RK-1d4194439839", "bairro is required")]
    [RegraNegocio("RK-4697ebd74678", "cep is required")]
    [RegraNegocio("RK-854f2452216e", "cidade is required")]
    [RegraNegocio("RK-98835efbf746", "uf is required")]
    [RegraNegocio("RK-6aff3b12acb2", "inscr is required")]
    [RegraNegocio("RK-353ee013c009", "data_grav is required")]
    [RegraNegocio("RK-37afeda868c2", "sub_trib is required")]
    [RegraNegocio("RK-f2ca891c315f", "Mov_Est is required")]
    public IReadOnlyList<ResultadoRegra> AvaliarObrigatorios()
    {
        var faltas = new List<ResultadoRegra>();

        void Texto(string? valor, string chave, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor)) faltas.Add(ResultadoRegra.Recusa(chave, $"{campo} is required"));
        }

        Texto(Cgc, "RK-b3e7fcc26f3e", "cgc");
        Texto(CodCom, "RK-ef82abb7456c", "cod_com");
        Texto(Categoria, "RK-b5da8c743238", "categ");
        Texto(TipoLogradouro, "RK-e74f29d4f922", "tiplog");
        Texto(Logradouro, "RK-2ce1876d83ad", "lograd");
        Texto(Bairro, "RK-1d4194439839", "bairro");
        Texto(Cep, "RK-4697ebd74678", "cep");
        Texto(Cidade, "RK-854f2452216e", "cidade");
        Texto(Uf, "RK-98835efbf746", "uf");
        Texto(Inscricao, "RK-6aff3b12acb2", "inscr");
        if (DataGravacao is null) faltas.Add(ResultadoRegra.Recusa("RK-353ee013c009", "data_grav is required"));
        if (SubstituicaoTributaria is null) faltas.Add(ResultadoRegra.Recusa("RK-37afeda868c2", "sub_trib is required"));
        if (MovimentaEstoque is null) faltas.Add(ResultadoRegra.Recusa("RK-f2ca891c315f", "Mov_Est is required"));

        return faltas;
    }
}
