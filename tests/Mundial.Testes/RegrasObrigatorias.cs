using Xunit;
using Mundial.Dominio;

namespace Mundial.Testes;

public class RegrasFornecedor
{
    private static Fornecedor Completo() => new()
    {
        Codigo = "00110", Descricao = "BEBIDAS PRIMAVERA LTDA",
        Cgc = "12.345.678/0001-90", CodCom = "00021", Categoria = "01", TipoLogradouro = "RUA",
        Logradouro = "DAS INDUSTRIAS", Bairro = "DISTRITO INDUSTRIAL", Cep = "21540-000",
        Cidade = "RIO DE JANEIRO", Uf = "RJ", Inscricao = "86.412.330", Situacao = "A",
        DataGravacao = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
        SubstituicaoTributaria = true, MovimentaEstoque = true
    };

    [Fact(DisplayName = "RK-b3e7fcc26f3e · fornecedor completo não acusa falta nenhuma")]
    public void RK_b3e7fcc26f3e_completo_passa() => Assert.Empty(Completo().AvaliarObrigatorios());

    [Theory(DisplayName = "obrigatórios de forne · cada campo ausente acusa a sua própria chave")]
    [InlineData("RK-b3e7fcc26f3e", "cgc")]
    [InlineData("RK-ef82abb7456c", "cod_com")]
    [InlineData("RK-b5da8c743238", "categ")]
    [InlineData("RK-e74f29d4f922", "tiplog")]
    [InlineData("RK-2ce1876d83ad", "lograd")]
    [InlineData("RK-1d4194439839", "bairro")]
    [InlineData("RK-4697ebd74678", "cep")]
    [InlineData("RK-854f2452216e", "cidade")]
    [InlineData("RK-98835efbf746", "uf")]
    [InlineData("RK-6aff3b12acb2", "inscr")]
    [InlineData("RK-353ee013c009", "data_grav")]
    [InlineData("RK-37afeda868c2", "sub_trib")]
    [InlineData("RK-f2ca891c315f", "Mov_Est")]
    public void Obrigatorios_de_forne(string chave, string campo)
    {
        var b = Completo();
        var f = new Fornecedor
        {
            Codigo = b.Codigo, Descricao = b.Descricao,
            Cgc = campo == "cgc" ? null : b.Cgc,
            CodCom = campo == "cod_com" ? null : b.CodCom,
            Categoria = campo == "categ" ? null : b.Categoria,
            TipoLogradouro = campo == "tiplog" ? null : b.TipoLogradouro,
            Logradouro = campo == "lograd" ? null : b.Logradouro,
            Bairro = campo == "bairro" ? null : b.Bairro,
            Cep = campo == "cep" ? null : b.Cep,
            Cidade = campo == "cidade" ? null : b.Cidade,
            Uf = campo == "uf" ? null : b.Uf,
            Inscricao = campo == "inscr" ? null : b.Inscricao,
            Situacao = b.Situacao,
            DataGravacao = campo == "data_grav" ? null : b.DataGravacao,
            SubstituicaoTributaria = campo == "sub_trib" ? null : b.SubstituicaoTributaria,
            MovimentaEstoque = campo == "Mov_Est" ? null : b.MovimentaEstoque
        };
        var faltas = f.AvaliarObrigatorios();
        Assert.Single(faltas);
        Assert.Equal(chave, faltas[0].Chave);
        Assert.Equal($"{campo} is required", faltas[0].Mensagem);
    }
}

public class RegrasCamposObrigatorios
{
    [Fact(DisplayName = "RK-d1a55f1103db · usuario.nome é obrigatório")]
    public void RK_d1a55f1103db_nome_obrigatorio()
        => Assert.Contains(Obrigatorios.Usuario(null, "Conferencia"), f => f.Chave == "RK-d1a55f1103db");

    [Fact(DisplayName = "RK-ea5a22eaf219 · acesso.descri é obrigatório")]
    public void RK_ea5a22eaf219_descri_obrigatorio()
        => Assert.Contains(Obrigatorios.Usuario("CLEBER", null), f => f.Chave == "RK-ea5a22eaf219");

    [Fact(DisplayName = "RK-fa1ca141cf21 · a flag alterar é obrigatória")]
    public void RK_fa1ca141cf21_alterar_obrigatoria()
        => Assert.Contains(Obrigatorios.Acesso(null, true, true, true), f => f.Chave == "RK-fa1ca141cf21");

    [Fact(DisplayName = "RK-6022cae899fa · a flag incluir é obrigatória")]
    public void RK_6022cae899fa_incluir_obrigatoria()
        => Assert.Contains(Obrigatorios.Acesso(true, null, true, true), f => f.Chave == "RK-6022cae899fa");

    [Fact(DisplayName = "RK-be780ff12c0e · a flag excluir é obrigatória")]
    public void RK_be780ff12c0e_excluir_obrigatoria()
        => Assert.Contains(Obrigatorios.Acesso(true, true, null, true), f => f.Chave == "RK-be780ff12c0e");

    [Fact(DisplayName = "RK-04c918661d8d · a flag consultar é obrigatória")]
    public void RK_04c918661d8d_consultar_obrigatoria()
        => Assert.Contains(Obrigatorios.Acesso(true, true, true, null), f => f.Chave == "RK-04c918661d8d");

    [Fact(DisplayName = "RK-82c929f4e851 · peso_bruto_col é obrigatório na conferência")]
    public void RK_82c929f4e851_peso_obrigatorio()
        => Assert.Contains(Obrigatorios.Conferencia(null, false, 'A'), f => f.Chave == "RK-82c929f4e851");

    [Fact(DisplayName = "RK-c5a64175c9a1 · balanca é obrigatória na conferência")]
    public void RK_c5a64175c9a1_balanca_obrigatoria()
        => Assert.Contains(Obrigatorios.Conferencia(0, null, 'A'), f => f.Chave == "RK-c5a64175c9a1");

    [Fact(DisplayName = "RK-16bc1acd7b74 · situacao em branco não satisfaz a obrigatoriedade")]
    public void RK_16bc1acd7b74_situacao_obrigatoria()
    {
        Assert.Contains(Obrigatorios.Conferencia(0, false, ' '), f => f.Chave == "RK-16bc1acd7b74");
        Assert.Empty(Obrigatorios.Conferencia(0, false, Situacao.Aguardando));
    }
}
