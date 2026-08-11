using System.Text.RegularExpressions;
using Xunit;

namespace Mundial.Testes.Arquitetura;

/// <summary>
/// Fiscaliza as decisões do spine que nenhum outro teste alcança.
///
/// Sem isto, um AD só existe enquanto alguém lembra dele — foi assim que a API ficou sem
/// autenticação por três commits, com AD-7 e AD-8 escritos e ninguém verificando.
/// Lê o código como texto de propósito: não depende de compilar nem de subir nada.
/// </summary>
public class Invariantes
{
    private static readonly string Raiz = Localizar();

    private static string Localizar()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("raiz do repositório não encontrada");
    }

    private static string Ler(params string[] partes) => File.ReadAllText(Path.Combine([Raiz, .. partes]));
    private static IEnumerable<string> Csproj(string projeto) =>
        Directory.GetFiles(Path.Combine(Raiz, "src", projeto), "*.csproj").Select(File.ReadAllText);

    [Fact(DisplayName = "AD-1 · Mundial.Dominio não referencia nenhum outro projeto")]
    public void AD1_dominio_nao_referencia_ninguem()
    {
        foreach (var conteudo in Csproj("Mundial.Dominio"))
            Assert.DoesNotContain("<ProjectReference", conteudo);
    }

    [Fact(DisplayName = "AD-1 · Mundial.Aplicacao só depende do domínio")]
    public void AD1_aplicacao_so_depende_do_dominio()
    {
        foreach (var conteudo in Csproj("Mundial.Aplicacao"))
        {
            var refs = Regex.Matches(conteudo, @"ProjectReference Include=""[^""]*?([\w.]+)\.csproj""")
                            .Select(m => m.Groups[1].Value).ToList();
            Assert.Equal(["Mundial.Dominio"], refs);
        }
    }

    [Fact(DisplayName = "AD-1 · o domínio não conhece Dapper, SQL nem ASP.NET")]
    public void AD1_dominio_sem_infraestrutura()
    {
        foreach (var arquivo in Directory.GetFiles(Path.Combine(Raiz, "src", "Mundial.Dominio"), "*.cs"))
        {
            var texto = File.ReadAllText(arquivo);
            foreach (var proibido in new[] { "using Dapper", "SqlConnection", "Microsoft.AspNetCore", "HttpClient" })
                Assert.DoesNotContain(proibido, texto);
        }
    }

    [Fact(DisplayName = "AD-21 · nenhum projeto de produção referencia Mundial.Demo, exceto a Api")]
    public void AD21_andaime_e_removivel()
    {
        foreach (var projeto in new[] { "Mundial.Dominio", "Mundial.Aplicacao", "Mundial.Infraestrutura" })
            foreach (var conteudo in Csproj(projeto))
                Assert.DoesNotContain("Mundial.Demo", conteudo);
    }

    [Fact(DisplayName = "AD-9 · nenhum CREATE ou ALTER TABLE fora do projeto de migrations")]
    public void AD9_ddl_so_no_dbup()
    {
        foreach (var arquivo in Directory.GetFiles(Path.Combine(Raiz, "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (arquivo.Contains("Mundial.Migrations")) continue;
            var texto = File.ReadAllText(arquivo).ToUpperInvariant();
            Assert.DoesNotContain("CREATE TABLE", texto);
            Assert.DoesNotContain("ALTER TABLE", texto);
        }
    }

    [Fact(DisplayName = "AD-19 · nenhum DateTime.Now no código — só o port IRelogio")]
    public void AD19_sem_datetime_now()
    {
        foreach (var arquivo in Directory.GetFiles(Path.Combine(Raiz, "src"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (var linha in File.ReadAllLines(arquivo))
            {
                var codigo = SemComentario(linha);   // a proibição vale para código, não para prosa
                Assert.DoesNotContain("DateTime.Now", codigo);
            }
        }
    }

    /// <summary>Corta comentário de linha, para uma menção em prosa não acusar violação.</summary>
    private static string SemComentario(string linha)
    {
        var i = linha.IndexOf("//", StringComparison.Ordinal);
        return i >= 0 ? linha[..i] : linha;
    }

    /// <summary>
    /// AD-7 e AD-8: o buraco que motivou este arquivo. Todo endpoint precisa declarar sua
    /// autorização, ou estar na lista de públicos — que é curta e explícita de propósito.
    /// </summary>
    [Fact(DisplayName = "AD-8 · todo endpoint exige autorização, salvo os públicos declarados")]
    public void AD8_todo_endpoint_exige_autorizacao()
    {
        string[] publicos = ["/api/saude", "/api/entrar", "/api/demo/codigos", "/api/demo/semear"];
        var programa = Ler("src", "Mundial.Api", "Program.cs");

        // cada bloco começa em app.MapX(" e termina no ");" que fecha a chamada
        var blocos = Regex.Matches(programa, @"app\.Map(?:Get|Post|Put|Delete)\(""(?<rota>[^""]+)""(?<corpo>.*?)\n(?:\}\)|\s*\)\s*)(?<sufixo>[^;]*);",
            RegexOptions.Singleline);

        var desprotegidos = new List<string>();
        foreach (Match b in blocos)
        {
            var rota = b.Groups["rota"].Value;
            if (publicos.Contains(rota)) continue;
            var trecho = b.Value;
            if (!trecho.Contains("RequireAuthorization")) desprotegidos.Add(rota);
        }

        Assert.True(desprotegidos.Count == 0,
            "Endpoints sem autorização: " + string.Join(", ", desprotegidos));
    }

    [Fact(DisplayName = "AD-7 · nenhum endpoint aceita matrícula vinda do corpo da requisição")]
    public void AD7_matricula_nunca_vem_do_cliente()
    {
        var programa = Ler("src", "Mundial.Api", "Program.cs");
        // os records de pedido não podem carregar Matricula — a identidade vem do token
        var pedidos = Regex.Matches(programa, @"record \w*Pedido\([^)]*\)");
        foreach (Match p in pedidos)
            Assert.DoesNotContain("Matricula", p.Value);
    }

    [Fact(DisplayName = "AD-12 · a aplicação fala português — nenhuma mensagem em inglês no domínio")]
    public void AD12_mensagens_em_portugues()
    {
        string[] suspeitas = ["\"Error", "\"Invalid", "\"Not found", "\"Failed"];
        foreach (var arquivo in Directory.GetFiles(Path.Combine(Raiz, "src", "Mundial.Dominio"), "*.cs"))
        {
            var texto = File.ReadAllText(arquivo);
            foreach (var s in suspeitas) Assert.DoesNotContain(s, texto);
        }
    }
}
