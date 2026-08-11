namespace Mundial.Dominio;

/// <summary>
/// Código de barras da unidade de venda. char(13) em estoq.CODBARR/2/3.
/// Largura vem da estrutura do DBF legado e é contrato — um caractere a mais deixa de casar.
/// </summary>
public readonly record struct CodigoEan13(string Valor)
{
    public const int Tamanho = 13;
    public bool Vazio => string.IsNullOrWhiteSpace(Valor);
    public override string ToString() => Valor;
}

/// <summary>Código de barras da embalagem. char(14) em estoq.BARR_EMB/2/3 e conferencia.dun14.</summary>
public readonly record struct CodigoDun14(string Valor)
{
    public const int Tamanho = 14;
    public bool Vazio => string.IsNullOrWhiteSpace(Valor);
    public override string ToString() => Valor;
}

/// <summary>
/// Chave natural do documento fiscal — as cinco primeiras colunas da PK composta de conferencia.
/// AD-10: o documento é o agregado; cada linha de conferencia é um item dele.
/// </summary>
public readonly record struct ChaveDocumento(string Filial, string OrigDes, string TipoDoc, string Serie, string Numero)
{
    public override string ToString() => $"{Filial}/{OrigDes}/{TipoDoc}/{Serie}/{Numero}";
}
