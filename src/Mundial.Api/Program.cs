using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Mundial.Api;
using Mundial.Aplicacao;
using Mundial.Demo;
using Mundial.Dominio;
using Mundial.Infraestrutura;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var conexao = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException("CONNECTION_STRING não definida. Veja .env.example.");
var modoDemo = Environment.GetEnvironmentVariable("MODO_DEMO")?.ToLowerInvariant() == "true";
var origemWeb = Environment.GetEnvironmentVariable("ORIGEM_WEB") ?? "http://localhost:3000";
var segredoJwt = Environment.GetEnvironmentVariable("JWT_SEGREDO")
    ?? throw new InvalidOperationException("JWT_SEGREDO não definida. Veja .env.example.");
var validadeSessao = TimeSpan.FromMinutes(
    int.TryParse(Environment.GetEnvironmentVariable("SESSAO_MINUTOS"), out var m) ? m : 480);
// Spec OpenAPI e Swagger UI. Ligados por padrão na POC; em produção basta DOCS_ABERTOS=false.
var docsAbertos = Environment.GetEnvironmentVariable("DOCS_ABERTOS")?.ToLowerInvariant() != "false";

// NFR-15: traces, métricas e logs em OTLP. Sem OTEL_EXPORTER_OTLP_ENDPOINT, não faz nada.
builder.AdicionarTelemetria();

builder.Services.AddSingleton(new FabricaConexao(conexao));
builder.Services.AddSingleton<IRelogio, Relogio>();
builder.Services.AddSingleton<IHashSenha, HashSenha>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IAcessoRepositorio, AcessoRepositorio>();
builder.Services.AddScoped<IProdutoConsulta, ProdutoConsulta>();
builder.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
builder.Services.AddScoped<CadastrarCodigos>();
builder.Services.AddScoped<ManterProduto>();
builder.Services.AddScoped<IFornecedorConsulta, FornecedorConsulta>();
builder.Services.AddScoped<IAuditoriaConsulta, AuditoriaConsulta>();
builder.Services.AddScoped<ConsultarFornecedores>();
builder.Services.AddScoped<DefinirSenha>();
builder.Services.AddSingleton<Mundial.Infraestrutura.Etiquetas.GeradorZpl>();
builder.Services.AddScoped<IDocumentoRepositorio, DocumentoRepositorio>();
builder.Services.AddScoped<IAuditoria, Auditoria>();
builder.Services.AddScoped<Autenticar>();
builder.Services.AddScoped<AbrirDocumento>();
builder.Services.AddScoped<ResolverLeitura>();
builder.Services.AddScoped<LancarQuantidade>();
builder.Services.AddScoped<FinalizarDocumento>();

// AD-21: o andaime só é registrado com MODO_DEMO=true.
if (modoDemo) builder.Services.AddSingleton(new Seeder(conexao));

// AD-7: autenticação por JWT. AD-8: uma policy por tabela e operação.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // Sem isto o ASP.NET remapeia "sub" para NameIdentifier e a busca pela claim falha.
        o.MapInboundClaims = false;
        o.TokenValidationParameters = Seguranca.Validacao(segredoJwt);
    });
builder.Services.AddAuthorization(Seguranca.RegistrarPolicies);

if (docsAbertos) builder.Services.AdicionarDocumentacao();

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// AD-15 / spine: CORS libera apenas a origem do frontend.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origemWeb.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// O proxy (Caddy) termina o TLS e fala http com a API. Sem honrar o X-Forwarded-Proto,
// o Kestrel se enxerga em http e a spec OpenAPI anuncia servers: [http://...]. Como o
// Swagger UI resolve o tokenUrl relativo contra esse servers, o botão Authorize dispara
// um POST http a partir de uma página https e o navegador bloqueia por mixed content
// ("NetworkError when attempting to fetch resource"). As redes conhecidas são limpas de
// propósito: o proxy é outro container, com IP da rede do compose, não loopback.
var encaminhados = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
                       | ForwardedHeaders.XForwardedFor
};
encaminhados.KnownIPNetworks.Clear();
encaminhados.KnownProxies.Clear();
app.UseForwardedHeaders(encaminhados);

// O healthcheck do compose sonda /api/saude a cada 10 segundos. O trace já o ignora
// (Telemetria.cs), mas a métrica não tinha como: a opção Filter da instrumentação de ASP.NET
// Core vale só para spans. Com isso, http.server.request.duration passava a maior parte do dia
// descrevendo a sonda — a mediana da API era a mediana do healthcheck. Este é o desligamento por
// requisição que o próprio ASP.NET oferece desde o .NET 8.
app.Use((contexto, proxima) =>
{
    if (contexto.Request.Path.StartsWithSegments("/api/saude"))
    {
        var marcadores = contexto.Features.Get<IHttpMetricsTagsFeature>();
        if (marcadores is not null) marcadores.MetricsDisabled = true;
    }
    return proxima(contexto);
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (docsAbertos) app.UsarDocumentacao();

// --- AD-11: contrato de erro RFC 9457 com extensão ruleKey ---
IResult Problema(ResultadoRegra r, int status = StatusCodes.Status422UnprocessableEntity)
{
    if (r.Tipo == TipoResultado.Conflito) status = StatusCodes.Status409Conflict;
    var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = status,
        Title = r.Tipo switch
        {
            TipoResultado.ExigeConfirmacao => "Confirmação necessária",
            TipoResultado.Conflito => "Conflito de gravação",
            _ => "Operação recusada"
        },
        Detail = r.Mensagem
    };
    if (r.Chave is not null) pd.Extensions["ruleKey"] = r.Chave;
    pd.Extensions["tipo"] = r.Tipo.ToString();
    // A mesma chave de regra que vai no corpo vai no span: no SigNoz dá para perguntar
    // "quantas vezes a RK-xxxx recusou hoje" sem abrir log nenhum.
    Telemetria.Marcar("regra.chave", r.Chave);
    Telemetria.Marcar("regra.tipo", r.Tipo.ToString());
    // Todo caminho de recusa da API desemboca aqui, então um contador só cobre a API inteira.
    Metricas.Recusa(r);
    return Results.Problem(pd);
}

app.MapGet("/api/saude", () => Results.Ok(new { estado = "no ar", modoDemo }))
   .WithTags("Saúde").WithSummary("Diz se a API está no ar e se o modo demo está ligado.");

// --- autenticação ---
app.MapPost("/api/entrar", async (PedidoLogin pedido, Autenticar autenticar) =>
{
    var (usuario, resultado) = await autenticar.Executar(pedido);
    return usuario is null
        ? Problema(resultado, StatusCodes.Status401Unauthorized)
        : Results.Ok(new
        {
            token = Seguranca.GerarToken(usuario, segredoJwt, validadeSessao),
            expiraEm = DateTime.UtcNow.Add(validadeSessao),
            matricula = usuario.Matricula,
            nome = usuario.Nome,
            permissoes = usuario.Permissoes.Select(p => new
            {
                tabela = p.Tabela, descricao = p.Descricao,
                consultar = p.Consultar, incluir = p.Incluir, alterar = p.Alterar, excluir = p.Excluir
            })
        });
}).WithTags("Autenticação").WithSummary("Troca matrícula e senha por um bearer JWT.");

// O botão Authorize do Swagger fala OAuth2 password grant, que é form-urlencoded e espera
// access_token/token_type. Mesmas regras de /api/entrar — só muda o envelope.
if (docsAbertos)
{
    app.MapPost(Documentacao.CaminhoToken, async (HttpRequest requisicao, Autenticar autenticar) =>
    {
        if (!requisicao.HasFormContentType)
            return Results.BadRequest(new { error = "invalid_request", error_description = "Envie form-urlencoded." });

        var formulario = await requisicao.ReadFormAsync();
        var pedido = new PedidoLogin(formulario["username"].ToString(), formulario["password"].ToString());
        var (usuario, resultado) = await autenticar.Executar(pedido);
        if (usuario is null)
            return Results.Json(new { error = "invalid_grant", error_description = resultado.Mensagem },
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Ok(new
        {
            access_token = Seguranca.GerarToken(usuario, segredoJwt, validadeSessao),
            token_type = "Bearer",
            expires_in = (int)validadeSessao.TotalSeconds
        });
    }).AllowAnonymous().DisableAntiforgery()
      .Accepts<PedidoToken>("application/x-www-form-urlencoded")
      .WithTags("Autenticação")
      .WithSummary("Password grant OAuth2 — é isto que o Authorize do Swagger chama.");
}

// --- painel de docas (FR-43..48) ---
app.MapGet("/api/docas", async (IDocumentoRepositorio repo) =>
{
    var docas = await repo.Docas();
    return Results.Ok(docas.Select(d => new
    {
        doca = d.Doca, estado = d.Estado, documento = d.Documento, fornecedor = d.Fornecedor,
        operador = d.Operador, itensLancados = d.ItensLancados, itensTotal = d.ItensTotal,
        temDivergencia = d.TemDivergencia, temPendencia = d.TemPendencia,
        abertaDesde = d.AbertaDesdeUtc
    }));
}).RequireAuthorization("conferenci:consultar")
  .WithTags("Docas").WithSummary("Painel das docas com estado, progresso e divergências.");

// --- conferência ---
app.MapGet("/api/conferencia", async (string documento, AbrirDocumento abrir) =>
{
    var (doc, resultado) = await abrir.Executar(documento);
    if (doc is null) return Problema(resultado, StatusCodes.Status404NotFound);
    return Results.Ok(Projetar(doc, resultado));
}).RequireAuthorization("conferenci:consultar")
  .WithTags("Conferência").WithSummary("Abre um documento e devolve seus itens e situação.");

app.MapPost("/api/conferencia/leituras",
    async (string documento, LeituraPedido pedido, ClaimsPrincipal quem,
           AbrirDocumento abrir, ResolverLeitura resolver) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    // A oferta de cadastrar depende da permissão real, lida do token — não do que o cliente afirma.
    var podeIncluir = quem.HasClaim("perm", Seguranca.ClaimPermissao("estoq", Operacao.Incluir));
    using var atividade = Telemetria.Fonte.StartActivity("ResolverLeitura");
    atividade?.SetTag("conferencia.documento", documento);
    var leitura = await resolver.Executar(doc, pedido.Codigo, podeIncluir);
    // O estado da leitura (aceito/recusado/ambiguo) é a métrica que o armazém sente: bipagem
    // que não resolve é fila parada na doca.
    atividade?.SetTag("leitura.estado", leitura.Estado);
    atividade?.SetTag("regra.chave", leitura.Chave);
    // A leitura recusada não passa pelo Problema() — devolve 200 com estado "recusado" —, então
    // sem este contador ela não apareceria em métrica nenhuma.
    Metricas.Leitura(leitura.Estado);
    return Results.Ok(leitura);
}).RequireAuthorization("conferenci:consultar")
  .WithTags("Conferência").WithSummary("Resolve um código lido (EAN/DUN-14) contra os itens do documento.");

app.MapPost("/api/conferencia/lancamentos",
    async (string documento, LancamentoPedido pedido, ClaimsPrincipal quem,
           AbrirDocumento abrir, LancarQuantidade lancar) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    using var atividade = Telemetria.Fonte.StartActivity("LancarQuantidade");
    atividade?.SetTag("conferencia.documento", documento);
    atividade?.SetTag("item.codigo", pedido.Codigo);
    atividade?.SetTag("item.quantidade", (double)pedido.Quantidade);
    var r = await lancar.Executar(doc, pedido.Codigo, pedido.Quantidade, quem.Matricula(), pedido.Confirmado, pedido.Versao);
    Metricas.Lancamento(r, "lancar");
    if (!r.Passou) return Problema(r);
    var (atualizado, resultado) = await abrir.Executar(documento);
    return Results.Ok(Projetar(atualizado!, resultado));
}).RequireAuthorization("conferenci:alterar")
  .WithTags("Conferência").WithSummary("Lança quantidade recebida de um item (usa versao para concorrência otimista).");

app.MapDelete("/api/conferencia/lancamentos",
    async (string documento, string codigo, bool confirmado, ClaimsPrincipal quem,
           AbrirDocumento abrir, LancarQuantidade lancar) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    // O estorno é a única operação de escrita da conferência que não tinha span próprio; sem ele,
    // "sumiu quantidade lançada" só aparecia como um DELETE genérico.
    using var atividade = Telemetria.Fonte.StartActivity("EstornarLancamento");
    atividade?.SetTag("conferencia.documento", documento);
    atividade?.SetTag("item.codigo", codigo);
    var r = await lancar.Excluir(doc, codigo, quem.Matricula(), confirmado);
    Metricas.Lancamento(r, "estornar");
    if (!r.Passou) return Problema(r);
    var (atualizado, resultado) = await abrir.Executar(documento);
    return Results.Ok(Projetar(atualizado!, resultado));
}).RequireAuthorization("conferenci:excluir")
  .WithTags("Conferência").WithSummary("Estorna o lançamento de um item.");

app.MapPost("/api/conferencia/fechamento",
    async (string documento, FechamentoPedido pedido, ClaimsPrincipal quem,
           AbrirDocumento abrir, FinalizarDocumento finalizar) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    using var atividade = Telemetria.Fonte.StartActivity("FinalizarDocumento");
    atividade?.SetTag("conferencia.documento", documento);
    atividade?.SetTag("conferencia.divergencia", doc.TemDivergencia);
    var r = await finalizar.Executar(doc, quem.Matricula(), pedido.Confirmado);
    Metricas.Finalizacao(r, doc.TemDivergencia, doc.ItensLancados);
    if (!r.Passou) return Problema(r);
    var (atualizado, resultado) = await abrir.Executar(documento);
    return Results.Ok(Projetar(atualizado!, resultado));
}).RequireAuthorization("conferenci:alterar")
  .WithTags("Conferência").WithSummary("Fecha o documento; com divergência exige confirmado=true.");

// --- cadastro de códigos DUN-14 (Épico 3) ---
app.MapGet("/api/produtos/{codigo}", async (string codigo, IProdutoConsulta produtos) =>
{
    var p = await produtos.PorCodigo(codigo);
    return p is null
        ? Results.NotFound()
        : Results.Ok(new
        {
            codigo = p.Codigo.Trim(), descricao = p.Descricao.Trim(),
            embalagem = p.Embalagem, embalagemQtd = p.EmbalagemQtd,
            ean = p.Ean, dun = p.Dun
        });
}).RequireAuthorization("estoq:consultar")
  .WithTags("Produtos").WithSummary("Consulta produto por código, com EAN e DUN-14 cadastrados.");

// FR-28: criar produto. É o único endpoint que exige `incluir` — antes disso a quarta permissão
// do legado viajava no token e nenhum caminho a consultava.
app.MapPost("/api/produtos", async (ProdutoPedido pedido, ClaimsPrincipal quem, ManterProduto manter) =>
{
    var r = await manter.Criar(
        new PedidoProduto(pedido.Codigo, pedido.Descricao, pedido.Embalagem,
                          pedido.EmbalagemQtd, pedido.Ean, pedido.Dun), quem.Matricula());
    return r.Passou ? Results.Ok(new { criado = true, codigo = pedido.Codigo.Trim() }) : Problema(r);
}).RequireAuthorization("estoq:incluir");

// FR-28: alterar descrição, embalagem e quantidade — não só os códigos de barras.
app.MapPut("/api/produtos/{codigo}", async (string codigo, ProdutoPedido pedido,
    ClaimsPrincipal quem, ManterProduto manter) =>
{
    var r = await manter.Alterar(
        new PedidoProduto(codigo, pedido.Descricao, pedido.Embalagem,
                          pedido.EmbalagemQtd, pedido.Ean, pedido.Dun), quem.Matricula());
    return r.Passou ? Results.Ok(new { gravado = true }) : Problema(r);
}).RequireAuthorization("estoq:alterar");

app.MapPut("/api/produtos/{codigo}/codigos", async (string codigo, CadastroPedido pedido,
    ClaimsPrincipal quem, CadastrarCodigos cadastrar) =>
{
    var r = await cadastrar.Executar(
        new PedidoCadastro(codigo, pedido.Dun, pedido.Confirmado), quem.Matricula());
    return r.Passou ? Results.Ok(new { gravado = true }) : Problema(r);
}).RequireAuthorization("estoq:alterar")
  .WithTags("Produtos").WithSummary("Grava os DUN-14 do produto.");

// --- etiqueta ZPL (Épico 3) ---
app.MapGet("/api/produtos/{codigo}/etiqueta", async (string codigo, string? codigoBarras,
    IProdutoConsulta produtos, Mundial.Infraestrutura.Etiquetas.GeradorZpl gerador) =>
{
    var p = await produtos.PorCodigo(codigo);
    if (p is null) return Results.NotFound();
    var cb = codigoBarras?.Trim()
             ?? p.DunPreenchidos.FirstOrDefault()
             ?? p.EanPreenchidos.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(cb))
        return Results.Problem("Produto sem código de barras para imprimir.", statusCode: 422);
    return Results.Ok(new { codigo = p.Codigo.Trim(), descricao = p.Descricao.Trim(),
        embalagem = p.Embalagem, embalagemQtd = p.EmbalagemQtd, codigoBarras = cb,
        zpl = gerador.Gerar(p, cb) });
}).RequireAuthorization("estoq:consultar")
  .WithTags("Produtos").WithSummary("Gera o ZPL da etiqueta do produto.");

// --- consultas (AD-15) ---
app.MapGet("/api/conferencias", async (IDocumentoRepositorio repo,
    int pagina = 0, int tamanho = 50, string? busca = null, string? ordem = null) =>
{
    var filtro = new FiltroListagem(pagina, tamanho, busca, ordem);
    var itens = await repo.Listar(filtro);
    var total = await repo.ContarListagem(filtro);
    return Results.Ok(new { itens, total, pagina, tamanho = filtro.TamanhoEfetivo });
}).RequireAuthorization("conferenci:consultar")
  .WithTags("Consultas").WithSummary("Lista conferências paginadas, com busca e ordenação.");

// --- fornecedores (FR-34, FR-35) ---
app.MapGet("/api/fornecedores", async (ConsultarFornecedores consultar,
    int pagina = 0, int tamanho = 50, string? busca = null) =>
{
    var r = await consultar.Executar(new FiltroListagem(pagina, tamanho, busca));
    return Results.Ok(new { itens = r.Itens, total = r.Total, pagina = r.PaginaAtual, tamanho = r.Tamanho });
}).RequireAuthorization("forne:consultar")
  .WithTags("Consultas").WithSummary("Lista fornecedores paginados.");

// --- auditoria (FR-41, FR-42) ---
app.MapGet("/api/auditoria", async (IAuditoriaConsulta auditoria,
    int pagina = 0, int tamanho = 50, string? busca = null) =>
{
    var filtro = new FiltroListagem(pagina, tamanho, busca);
    var itens = await auditoria.Listar(filtro);
    var total = await auditoria.Contar(filtro);
    return Results.Ok(new { itens, total, pagina, tamanho = filtro.TamanhoEfetivo });
}).RequireAuthorization("log_even:consultar")
  .WithTags("Consultas").WithSummary("Lista o log de eventos (auditoria).");

// --- troca de senha (FR-3) ---
app.MapPost("/api/senha", (SenhaPedido pedido, DefinirSenha definir, ClaimsPrincipal quem) =>
{
    var (hash, r) = definir.Executar(pedido.Senha, pedido.Confirmacao);
    if (hash is null) return Problema(r);
    // O POC não persiste a troca: usuários vêm do seed e voltam a ele no reset (Story 5.3).
    return Results.Ok(new { trocada = true, matricula = quem.Matricula() });
}).RequireAuthorization("usuario:alterar")
  .WithTags("Autenticação").WithSummary("Valida e troca a senha do operador logado.");

// --- andaime de demonstração (AD-21) ---
if (modoDemo)
{
    app.MapPost("/api/demo/semear", async (Seeder seeder) =>
    {
        if (await seeder.JaSemeado()) return Results.Ok(new { semeado = false, motivo = "já havia dado" });
        await seeder.Semear();
        return Results.Ok(new { semeado = true });
    }).RequireAuthorization("conferenci:alterar")
      .WithTags("Demo").WithSummary("Semeia a massa de demonstração se o banco estiver vazio.");

    // Story 5.3 / FR-51: reset entre apresentações.
    app.MapPost("/api/demo/reset", async (Seeder seeder) =>
    {
        await seeder.Resetar();
        return Results.Ok(new { resetado = true });
    }).RequireAuthorization("conferenci:alterar")
      .WithTags("Demo").WithSummary("Reseta a massa entre apresentações.");

    app.MapGet("/api/demo/codigos", () => Results.Ok(new[]
    {
        new { codigo = "7891234567897", efeito = "resolve · Refrigerante Cola 2L", tipo = "aceito" },
        new { codigo = "7891234500013", efeito = "resolve · Cerveja Pilsen (item com divergência)", tipo = "aceito" },
        new { codigo = "7891234511019", efeito = "resolve · Água Mineral", tipo = "aceito" },
        new { codigo = "7899999000123", efeito = "não existe em nenhum produto", tipo = "recusado" },
        new { codigo = "7890000111222", efeito = "existe em dois produtos — leitura ambígua", tipo = "ambiguo" },
        new { codigo = "7894455000012", efeito = "existe, mas não pertence a este documento", tipo = "recusado" }
    })).WithTags("Demo").WithSummary("Códigos de roteiro e o efeito de cada um.");
}

// Semeia na subida quando o banco está vazio (NFR-11: demo funciona de checkout limpo).
if (modoDemo)
{
    using var escopo = app.Services.CreateScope();
    var seeder = escopo.ServiceProvider.GetRequiredService<Seeder>();
    for (var tentativa = 1; tentativa <= 30; tentativa++)
    {
        try
        {
            if (!await seeder.JaSemeado()) { await seeder.Semear(); app.Logger.LogInformation("Massa de demonstração semeada."); }
            else app.Logger.LogInformation("Massa já existente; seed ignorado.");
            break;
        }
        catch (Exception ex)
        {
            // Só o banco ainda subindo justifica repetir; erro de dado precisa aparecer na hora.
            if (ex is not Microsoft.Data.SqlClient.SqlException sql || sql.Number is not (-2 or 53 or 4060 or 18456 or 40613))
            {
                app.Logger.LogError(ex, "Seed falhou: {Mensagem}", ex.Message);
                break;
            }
            if (tentativa == 30) app.Logger.LogError(ex, "Banco não respondeu a tempo.");
            else await Task.Delay(3000);
        }
    }
}

app.Run();

static object Projetar(Documento doc, ResultadoRegra aviso) => new
{
    documento = doc.NumeroExibido,
    chave = doc.Chave.ToString(),
    doca = doc.Doca,
    fornecedor = doc.NomeFornecedor,
    codigoFornecedor = doc.CodigoFornecedor,
    fechado = doc.Fechado,
    situacao = doc.SituacaoAtual.ToString(),
    situacaoDescricao = Situacao.Descrever(doc.SituacaoAtual),
    matrConf = doc.MatrConf,
    matrFec = doc.MatrFec,
    dtHora = doc.DtHora,
    itensLancados = doc.ItensLancados,
    itensPendentes = doc.ItensPendentes,
    temDivergencia = doc.TemDivergencia,
    aviso = aviso.Tipo == TipoResultado.Aceito ? null : new { chave = aviso.Chave, mensagem = aviso.Mensagem },
    itens = doc.Itens.Select(i => new
    {
        codigo = i.Codigo.Trim(), descricao = i.Descricao, dun14 = i.Dun14, itNf = i.ItNf,
        qtdNf = i.QtdNf, qtdRec = i.QtdRec,
        divergencia = i.QtdRec > 0 ? i.Divergencia : (decimal?)null,
        temDivergencia = i.TemDivergencia, pendencia = i.Pendencia,
        situacao = i.SituacaoAtual.ToString(),
        versao = i.Versao is null ? null : Convert.ToBase64String(i.Versao)
    })
};

record LeituraPedido(string Codigo);
record LancamentoPedido(string Codigo, decimal Quantidade, bool Confirmado, string? Versao = null);
record FechamentoPedido(bool Confirmado);
record CadastroPedido(string[] Dun, bool Confirmado);
record SenhaPedido(string Senha, string Confirmacao);
record ProdutoPedido(string Codigo, string Descricao, string? Embalagem, decimal? EmbalagemQtd,
    string? Ean, string[] Dun);
/// Só documenta o corpo do password grant; o endpoint lê o formulário direto da requisição.
record PedidoToken(string grant_type, string username, string password);
