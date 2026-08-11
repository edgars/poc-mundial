using Xunit;
using Mundial.Aplicacao;
using Mundial.Dominio;

namespace Mundial.Testes;

// Fakes em memória: a camada de aplicação é testada sem banco, pelos ports do AD-1.
file sealed class UsuariosFake(params Usuario[] usuarios) : IUsuarioRepositorio
{
    public Task<Usuario?> PorMatricula(string m, CancellationToken ct = default)
        => Task.FromResult(usuarios.FirstOrDefault(u => u.Matricula == m));
}

file sealed class AcessosFake : IAcessoRepositorio
{
    public Task<IReadOnlyList<Acesso>> PorMatricula(string m, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Acesso>>([]);
}

file sealed class HashFake : IHashSenha
{
    public string Gerar(string senha) => "#" + senha;
    public bool Verificar(string senha, string hash) => hash == "#" + senha;
}

internal sealed class ProdutosFake(params Produto[] produtos) : IProdutoConsulta
{
    public Task<IReadOnlyList<Produto>> PorCodigoDeBarras(string c, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Produto>>(
            produtos.Where(p => p.EanPreenchidos.Contains(c) || p.DunPreenchidos.Contains(c)).ToList());
    public Task<Produto?> PorCodigo(string c, CancellationToken ct = default)
        => Task.FromResult(produtos.FirstOrDefault(p => p.Codigo == c));
}

file sealed class DocumentosFake(Documento? doc) : IDocumentoRepositorio
{
    public List<string> Gravacoes { get; } = [];
    public Task<Documento?> PorNumeroExibido(string n, CancellationToken ct = default) => Task.FromResult(doc);
    public Task<IReadOnlyList<ResumoDoca>> Docas(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ResumoDoca>>([]);
    public Task<IReadOnlyList<ResumoDocumento>> Listar(FiltroListagem f, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ResumoDocumento>>([]);
    public Task<int> ContarListagem(FiltroListagem f, CancellationToken ct = default) => Task.FromResult(0);
    /// <summary>Quando ConflitaNaProxima é true, simula outra pessoa tendo gravado antes (AD-17).</summary>
    public bool ConflitaNaProxima { get; set; }
    public Task<bool> GravarLancamento(ItemConferencia i, CancellationToken ct = default)
    {
        if (ConflitaNaProxima) return Task.FromResult(false);
        Gravacoes.Add($"{i.Codigo}={i.QtdRec}");
        return Task.FromResult(true);
    }
    public Task Fechar(Documento d, CancellationToken ct = default)
    { Gravacoes.Add("fechado"); return Task.CompletedTask; }
}

file sealed class AuditoriaFake : IAuditoria
{
    public List<string> Registros { get; } = [];
    public Task Registrar(string u, string t, string c, string? a, string? n, CancellationToken ct = default)
    { Registros.Add($"{u}|{t}|{c}|{a}->{n}"); return Task.CompletedTask; }
}

file sealed class RelogioFake : IRelogio
{
    public DateTime AgoraUtc => new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
}

public class RegrasAutenticacao
{
    private static Usuario Cleber => new()
    {
        Matricula = "04127", Nome = "CLEBER SANTOS", SenhaHash = "#mundial", NivelUsuario = "3"
    };

    private static Autenticar Servico(params Usuario[] u)
        => new(new UsuariosFake(u), new AcessosFake(), new HashFake());

    [Fact(DisplayName = "RK-046f5592ef5b · matrícula inexistente recusa com o texto do legado")]
    public async Task RK_046f5592ef5b_matricula_inexistente()
    {
        var (usuario, r) = await Servico(Cleber).Executar(new PedidoLogin("99999", "mundial"));
        Assert.Null(usuario);
        Assert.Equal("Matrícula não cadastrada! Favor contactar supervisor", r.Mensagem);
        Assert.Equal("RK-046f5592ef5b", r.Chave);
    }

    [Fact(DisplayName = "RK-f8293cf9dbb3 · senha errada recusa, e não revela que a matrícula existe")]
    public async Task RK_f8293cf9dbb3_senha_invalida()
    {
        var (usuario, r) = await Servico(Cleber).Executar(new PedidoLogin("04127", "errada"));
        Assert.Null(usuario);
        Assert.Equal("Senha inválida", r.Mensagem);
        Assert.Equal("RK-f8293cf9dbb3", r.Chave);
    }

    [Fact(DisplayName = "RK-046f5592ef5b · credenciais corretas entram")]
    public async Task RK_046f5592ef5b_credenciais_corretas_entram()
    {
        var (usuario, r) = await Servico(Cleber).Executar(new PedidoLogin("04127", "mundial"));
        Assert.NotNull(usuario);
        Assert.True(r.Passou);
        Assert.Equal("CLEBER SANTOS", usuario!.Nome);
    }
}

public class RegrasLeitura
{
    private static Produto Cola => new()
    {
        Codigo = "04127", Descricao = "REFRIGERANTE COLA 2L", Embalagem = "CX c/ 6", EmbalagemQtd = 6,
        Ean = ["7891234567897", null, null], Dun = ["17891234567894", null, null]
    };
    private static Produto Sabao => new()
    {
        Codigo = "06430", Descricao = "SABAO EM PO 1KG",
        Ean = ["7894455000012", null, null], Dun = ["17894455000019", null, null]
    };
    private static Produto Biscoito(string codigo, string nome) => new()
    {
        Codigo = codigo, Descricao = nome,
        Ean = ["7890000111222", null, null], Dun = ["17890000111229", null, null]
    };

    private static Documento DocComCola()
    {
        var chave = new ChaveDocumento("00001", "00110", "NFE", "1", "000148372");
        var d = new Documento
        {
            Chave = chave, NumeroExibido = "000148372/1",
            CodigoFornecedor = "00110", NomeFornecedor = "BEBIDAS PRIMAVERA LTDA"
        };
        d.Itens.Add(ItemConferencia.Reidratar(chave, "04127", null, 1, 40, 0, 40, 0, false, Situacao.Aguardando));
        return d;
    }

    [Fact(DisplayName = "RK-798f00f19690 · código que não existe em produto nenhum é recusado")]
    public async Task RK_798f00f19690_codigo_inexistente()
    {
        var r = await new ResolverLeitura(new ProdutosFake(Cola)).Executar(DocComCola(), "7899999000123");
        Assert.Equal("recusado", r.Estado);
        Assert.Equal("Código Não cadastrado!", r.Mensagem);
    }

    [Fact(DisplayName = "RK-6fef4d31a290 · código válido do documento resolve para um produto")]
    public async Task RK_6fef4d31a290_codigo_valido_resolve()
    {
        var r = await new ResolverLeitura(new ProdutosFake(Cola)).Executar(DocComCola(), "7891234567897");
        Assert.Equal("aceito", r.Estado);
        Assert.Equal("REFRIGERANTE COLA 2L", r.Item!.Descricao);
        Assert.Equal("CX c/ 6", r.Item.Embalagem);
    }

    [Fact(DisplayName = "RK-732bb9300bad · produto que existe mas não está neste documento é recusado")]
    public async Task RK_732bb9300bad_produto_de_outro_documento()
    {
        var r = await new ResolverLeitura(new ProdutosFake(Cola, Sabao)).Executar(DocComCola(), "7894455000012");
        Assert.Equal("recusado", r.Estado);
        Assert.Contains("Código Não cadastrado para", r.Mensagem);
        Assert.Contains("BEBIDAS PRIMAVERA", r.Mensagem);
    }

    [Fact(DisplayName = "FR-13 · código que casa com dois produtos recusa e mostra os candidatos")]
    public async Task FR13_leitura_ambigua_nao_escolhe()
    {
        var produtos = new ProdutosFake(Biscoito("07001", "BISCOITO RECHEADO"), Biscoito("07002", "BISCOITO PROMO"));
        var r = await new ResolverLeitura(produtos).Executar(DocComCola(), "7890000111222");
        Assert.Equal("ambiguo", r.Estado);
        Assert.Equal(2, r.Candidatos!.Count);
    }
}

public class RegrasLancamento
{
    private static Documento Doc(decimal jaLancado = 0, bool fechado = false)
    {
        var chave = new ChaveDocumento("00001", "00110", "NFE", "1", "000148372");
        var d = new Documento { Chave = chave, NumeroExibido = "000148372/1" };
        d.Itens.Add(ItemConferencia.Reidratar(chave, "04127", null, 1, 40, jaLancado, 40, jaLancado,
            false, jaLancado > 0 ? Situacao.EmConferencia : Situacao.Aguardando));
        if (fechado) d.Fechar("04127", DateTime.UtcNow);
        return d;
    }

    [Fact(DisplayName = "RK-bdfbdff6c821 · excluir lançamento pede confirmação antes de apagar")]
    public async Task RK_bdfbdff6c821_exclusao_pede_confirmacao()
    {
        var doc = Doc(jaLancado: 40);
        var repo = new DocumentosFake(doc);
        var servico = new LancarQuantidade(repo, new AuditoriaFake());

        var semConfirmar = await servico.Excluir(doc, "04127", "04127", confirmado: false);
        Assert.Equal(TipoResultado.ExigeConfirmacao, semConfirmar.Tipo);
        Assert.Equal("Confirma Exclusão?", semConfirmar.Mensagem);
        Assert.Empty(repo.Gravacoes);                      // nada foi gravado

        var confirmado = await servico.Excluir(doc, "04127", "04127", confirmado: true);
        Assert.True(confirmado.Passou);
        Assert.Equal(0, doc.Itens[0].QtdRec);
    }

    [Fact(DisplayName = "AD-17 · lançar em item já lançado exige confirmação e depois substitui")]
    public async Task AD17_lancamento_exige_confirmacao_e_substitui()
    {
        var doc = Doc(jaLancado: 40);
        var repo = new DocumentosFake(doc);
        var auditoria = new AuditoriaFake();
        var servico = new LancarQuantidade(repo, auditoria);

        var semConfirmar = await servico.Executar(doc, "04127", 38, "04127", confirmado: false);
        Assert.Equal(TipoResultado.ExigeConfirmacao, semConfirmar.Tipo);
        Assert.Equal(40, doc.Itens[0].QtdRec);             // intocado

        var confirmado = await servico.Executar(doc, "04127", 38, "04127", confirmado: true);
        Assert.True(confirmado.Passou);
        Assert.Equal(38, doc.Itens[0].QtdRec);             // substituiu, não somou
        Assert.Contains(auditoria.Registros, r => r.Contains("QTD_REC = 40->QTD_REC = 38"));
    }

    [Fact(DisplayName = "RK-c0fce5362f62 · documento inexistente é recusado ao abrir")]
    public async Task RK_c0fce5362f62_documento_inexistente()
    {
        var (doc, r) = await new AbrirDocumento(new DocumentosFake(null)).Executar("000000000/9");
        Assert.Null(doc);
        Assert.Equal("Documento não cadastrado!", r.Mensagem);
        Assert.Equal("RK-c0fce5362f62", r.Chave);
    }

    [Fact(DisplayName = "NFR-10 · documento fechado recusa lançamento")]
    public async Task NFR10_fechado_recusa_lancamento()
    {
        var doc = Doc(jaLancado: 40, fechado: true);
        var r = await new LancarQuantidade(new DocumentosFake(doc), new AuditoriaFake())
            .Executar(doc, "04127", 10, "04127", confirmado: true);
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Equal("Este Documento já foi conferido!", r.Mensagem);
    }

    [Fact(DisplayName = "AD-10 · finalizar grava quem fechou, quando, e registra na auditoria")]
    public async Task AD10_finalizar_grava_e_audita()
    {
        var doc = Doc(jaLancado: 40);
        var repo = new DocumentosFake(doc);
        var auditoria = new AuditoriaFake();
        var servico = new FinalizarDocumento(repo, auditoria, new RelogioFake());

        var semConfirmar = await servico.Executar(doc, "04310", confirmado: false);
        Assert.Equal(TipoResultado.ExigeConfirmacao, semConfirmar.Tipo);
        Assert.False(doc.Fechado);

        var r = await servico.Executar(doc, "04310", confirmado: true);
        Assert.True(r.Passou);
        Assert.True(doc.Fechado);
        Assert.Equal("04310", doc.MatrFec);
        Assert.Equal(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), doc.DtHora);
        Assert.Contains("fechado", repo.Gravacoes);
        Assert.Contains(auditoria.Registros, x => x.Contains("fechado = 1"));
    }
}

file sealed class ProdutoRepoFake(params Produto[] produtos) : IProdutoRepositorio
{
    public List<string> Salvos { get; } = [];
    public Task<IReadOnlyList<Produto>> PorCodigoDeBarras(string c, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Produto>>(
            produtos.Where(p => p.EanPreenchidos.Contains(c) || p.DunPreenchidos.Contains(c)).ToList());
    public Task<Produto?> PorCodigo(string c, CancellationToken ct = default)
        => Task.FromResult(produtos.FirstOrDefault(p => p.Codigo == c));
    public Task<Produto?> DonoDoCodigoDeBarras(string cb, string exceto, CancellationToken ct = default)
        => Task.FromResult(produtos.FirstOrDefault(p => p.Codigo != exceto && p.DunPreenchidos.Contains(cb)));
    public Task Salvar(Produto p, CancellationToken ct = default)
    { Salvos.Add(p.Codigo); return Task.CompletedTask; }
}

file sealed class AuditoriaVazia : IAuditoria
{
    public Task Registrar(string u, string t, string c, string? a, string? n, CancellationToken ct = default)
        => Task.CompletedTask;
}

public class RegrasCadastro
{
    private static Produto Cola() => new()
    {
        Codigo = "04127", Descricao = "REFRIGERANTE COLA 2L", Embalagem = "CX c/ 6", EmbalagemQtd = 6,
        Ean = ["7891234567897", null, null], Dun = ["17891234567894", null, null]
    };
    private static Produto Cerveja() => new()
    {
        Codigo = "04982", Descricao = "CERVEJA PILSEN LATA 350ML",
        Ean = ["7891234500013", null, null], Dun = ["17891234500010", null, null]
    };

    [Fact(DisplayName = "RK-e84d750f340a · cadastrar em produto inexistente é recusado")]
    public async Task RK_e84d750f340a_produto_inexistente()
    {
        var repo = new ProdutoRepoFake(Cola());
        var r = await new CadastrarCodigos(repo, new AuditoriaVazia())
            .Executar(new PedidoCadastro("99999", ["17891234511016", "", ""], false), "04310");
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Equal("Código não cadastrado!", r.Mensagem);
        Assert.Empty(repo.Salvos);
    }

    [Fact(DisplayName = "RK-5a7aaaa8862d · produto existente aceita um código novo")]
    public async Task RK_5a7aaaa8862d_produto_existente_aceita()
    {
        var repo = new ProdutoRepoFake(Cola());
        var r = await new CadastrarCodigos(repo, new AuditoriaVazia())
            .Executar(new PedidoCadastro("04127", ["17891234567894", "17891234511016", ""], false), "04310");
        Assert.True(r.Passou);
        Assert.Contains("04127", repo.Salvos);
    }

    [Fact(DisplayName = "RK-2976e3756f6d · código que já pertence a outro produto é recusado, com o nome dele")]
    public async Task RK_2976e3756f6d_codigo_de_outro_produto()
    {
        var repo = new ProdutoRepoFake(Cola(), Cerveja());
        var r = await new CadastrarCodigos(repo, new AuditoriaVazia())
            .Executar(new PedidoCadastro("04127", ["17891234500010", "", ""], false), "04310");
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Contains("Código já cadastrado para o Produto", r.Mensagem);
        Assert.Contains("CERVEJA PILSEN", r.Mensagem);
        Assert.Empty(repo.Salvos);
    }

    [Fact(DisplayName = "RK-ab467d52fa1f · a checagem de outro produto vale para o segundo slot")]
    public async Task RK_ab467d52fa1f_segundo_slot()
    {
        var repo = new ProdutoRepoFake(Cola(), Cerveja());
        var r = await new CadastrarCodigos(repo, new AuditoriaVazia())
            .Executar(new PedidoCadastro("04127", ["17891234567894", "17891234500010", ""], false), "04310");
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Equal("RK-ab467d52fa1f", r.Chave);
    }

    [Fact(DisplayName = "RK-f3bda1fa3b77 · a checagem de outro produto vale para o terceiro slot")]
    public async Task RK_f3bda1fa3b77_terceiro_slot()
    {
        var repo = new ProdutoRepoFake(Cola(), Cerveja());
        var r = await new CadastrarCodigos(repo, new AuditoriaVazia())
            .Executar(new PedidoCadastro("04127", ["17891234567894", "", "17891234500010"], false), "04310");
        Assert.Equal(TipoResultado.Recusado, r.Tipo);
        Assert.Equal("RK-f3bda1fa3b77", r.Chave);
    }

    [Fact(DisplayName = "RK-ade9dd1661d1 · apagar um código exige confirmação antes de gravar")]
    public async Task RK_ade9dd1661d1_exclusao_exige_confirmacao()
    {
        var repo = new ProdutoRepoFake(Cola());
        var servico = new CadastrarCodigos(repo, new AuditoriaVazia());

        var sem = await servico.Executar(new PedidoCadastro("04127", ["", "", ""], false), "04310");
        Assert.Equal(TipoResultado.ExigeConfirmacao, sem.Tipo);
        Assert.Empty(repo.Salvos);

        var com = await servico.Executar(new PedidoCadastro("04127", ["", "", ""], true), "04310");
        Assert.True(com.Passou);
        Assert.Contains("04127", repo.Salvos);
    }
}

public class RegrasSenha
{
    [Fact(DisplayName = "RK-58fefec22db6 · senha e confirmação diferentes são recusadas")]
    public void RK_58fefec22db6_confirmacao_divergente()
    {
        var (hash, r) = new DefinirSenha(new HashFake()).Executar("mundial", "mundail");
        Assert.Null(hash);
        Assert.Equal("Você deve Confirmar a senha", r.Mensagem);
    }

    [Fact(DisplayName = "RK-58fefec22db6 · senha e confirmação iguais geram o hash")]
    public void RK_58fefec22db6_confirmacao_correta()
    {
        var (hash, r) = new DefinirSenha(new HashFake()).Executar("mundial", "mundial");
        Assert.NotNull(hash);
        Assert.True(r.Passou);
    }

    [Fact(DisplayName = "AD-7 · o hasher real nunca deixa a senha legível, e verifica de volta")]
    public void AD7_hash_real_nao_guarda_senha_em_claro()
    {
        var hasher = new Mundial.Infraestrutura.HashSenha();
        var hash = hasher.Gerar("mundial");
        Assert.DoesNotContain("mundial", hash);
        Assert.True(hasher.Verificar("mundial", hash));
        Assert.False(hasher.Verificar("errada", hash));
    }
}

public class RegrasOfertaCadastro
{
    private static Documento Doc()
    {
        var chave = new ChaveDocumento("00001", "00110", "NFE", "1", "000148372");
        var d = new Documento { Chave = chave, NumeroExibido = "000148372/1", NomeFornecedor = "BEBIDAS PRIMAVERA" };
        d.Itens.Add(ItemConferencia.Reidratar(chave, "04127", null, 1, 40, 0, 40, 0, false, Situacao.Aguardando));
        return d;
    }

    [Fact(DisplayName = "RK-dab7d2033e2e · com permissão de inclusão, a recusa oferece cadastrar na hora")]
    public async Task RK_dab7d2033e2e_oferece_cadastro_com_permissao()
    {
        var r = await new ResolverLeitura(new ProdutosFake()).Executar(Doc(), "7899999000123", podeIncluir: true);
        Assert.Equal("recusado", r.Estado);
        Assert.NotNull(r.OfertaCadastro);
        Assert.Contains("Deseja Cadastrar agora?", r.OfertaCadastro);
    }

    [Fact(DisplayName = "RK-dab7d2033e2e · sem permissão de inclusão, a oferta não aparece")]
    public async Task RK_dab7d2033e2e_sem_permissao_nao_oferece()
    {
        var r = await new ResolverLeitura(new ProdutosFake()).Executar(Doc(), "7899999000123", podeIncluir: false);
        Assert.Equal("recusado", r.Estado);
        Assert.Null(r.OfertaCadastro);   // o item fica pendente e o operador segue (UJ-2)
    }
}

public class RegrasConcorrencia
{
    private static Documento Doc()
    {
        var chave = new ChaveDocumento("00001", "00110", "NFE", "1", "000148372");
        var d = new Documento { Chave = chave, NumeroExibido = "000148372/1" };
        d.Itens.Add(ItemConferencia.Reidratar(chave, "04127", null, 1, 40, 0, 40, 0,
            false, Situacao.Aguardando, null, [1, 2, 3]));
        return d;
    }

    [Fact(DisplayName = "AD-17 · gravação concorrente devolve conflito, sem sobrescrever em silêncio")]
    public async Task AD17_conflito_nao_sobrescreve()
    {
        var doc = Doc();
        var repo = new DocumentosFake(doc) { ConflitaNaProxima = true };
        var r = await new LancarQuantidade(repo, new AuditoriaFake())
            .Executar(doc, "04127", 30, "04127", confirmado: true);

        Assert.Equal(TipoResultado.Conflito, r.Tipo);
        Assert.Contains("Outro operador", r.Mensagem);
        Assert.Empty(repo.Gravacoes);
    }

    [Fact(DisplayName = "AD-17 · sem concorrência, a gravação segue normal")]
    public async Task AD17_sem_conflito_grava()
    {
        var doc = Doc();
        var repo = new DocumentosFake(doc);
        var r = await new LancarQuantidade(repo, new AuditoriaFake())
            .Executar(doc, "04127", 30, "04127", confirmado: true);

        Assert.True(r.Passou);
        Assert.Contains("04127=30", repo.Gravacoes);
    }
}
