using System.Text.Json;
using System.Text.Json.Serialization;
using Mundial.Aplicacao;
using Mundial.Demo;
using Mundial.Dominio;
using Mundial.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);

var conexao = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException("CONNECTION_STRING não definida. Veja .env.example.");
var modoDemo = Environment.GetEnvironmentVariable("MODO_DEMO")?.ToLowerInvariant() == "true";
var origemWeb = Environment.GetEnvironmentVariable("ORIGEM_WEB") ?? "http://localhost:3000";

builder.Services.AddSingleton(new FabricaConexao(conexao));
builder.Services.AddSingleton<IRelogio, Relogio>();
builder.Services.AddSingleton<IHashSenha, HashSenha>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IAcessoRepositorio, AcessoRepositorio>();
builder.Services.AddScoped<IProdutoConsulta, ProdutoConsulta>();
builder.Services.AddScoped<IDocumentoRepositorio, DocumentoRepositorio>();
builder.Services.AddScoped<IAuditoria, Auditoria>();
builder.Services.AddScoped<Autenticar>();
builder.Services.AddScoped<AbrirDocumento>();
builder.Services.AddScoped<ResolverLeitura>();
builder.Services.AddScoped<LancarQuantidade>();
builder.Services.AddScoped<FinalizarDocumento>();

// AD-21: o andaime só é registrado com MODO_DEMO=true.
if (modoDemo) builder.Services.AddSingleton(new Seeder(conexao));

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// AD-15 / spine: CORS libera apenas a origem do frontend.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origemWeb).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// --- AD-11: contrato de erro RFC 9457 com extensão ruleKey ---
IResult Problema(ResultadoRegra r, int status = StatusCodes.Status422UnprocessableEntity)
{
    var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = status,
        Title = r.Tipo == TipoResultado.ExigeConfirmacao ? "Confirmação necessária" : "Operação recusada",
        Detail = r.Mensagem
    };
    if (r.Chave is not null) pd.Extensions["ruleKey"] = r.Chave;
    pd.Extensions["tipo"] = r.Tipo.ToString();
    return Results.Problem(pd);
}

app.MapGet("/api/saude", () => Results.Ok(new { estado = "no ar", modoDemo }));

// --- autenticação ---
app.MapPost("/api/entrar", async (PedidoLogin pedido, Autenticar autenticar) =>
{
    var (usuario, resultado) = await autenticar.Executar(pedido);
    return usuario is null
        ? Problema(resultado, StatusCodes.Status401Unauthorized)
        : Results.Ok(new
        {
            matricula = usuario.Matricula,
            nome = usuario.Nome,
            permissoes = usuario.Permissoes.Select(p => new
            {
                tabela = p.Tabela, descricao = p.Descricao,
                consultar = p.Consultar, incluir = p.Incluir, alterar = p.Alterar, excluir = p.Excluir
            })
        });
});

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
});

// --- conferência ---
app.MapGet("/api/conferencia", async (string documento, AbrirDocumento abrir) =>
{
    var (doc, resultado) = await abrir.Executar(documento);
    if (doc is null) return Problema(resultado, StatusCodes.Status404NotFound);
    return Results.Ok(Projetar(doc, resultado));
});

app.MapPost("/api/conferencia/leituras",
    async (string documento, LeituraPedido pedido, AbrirDocumento abrir, ResolverLeitura resolver) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    var leitura = await resolver.Executar(doc, pedido.Codigo);
    return Results.Ok(leitura);
});

app.MapPost("/api/conferencia/lancamentos",
    async (string documento, LancamentoPedido pedido, AbrirDocumento abrir, LancarQuantidade lancar) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    var r = await lancar.Executar(doc, pedido.Codigo, pedido.Quantidade, pedido.Matricula, pedido.Confirmado);
    if (!r.Passou) return Problema(r);
    var (atualizado, resultado) = await abrir.Executar(documento);
    return Results.Ok(Projetar(atualizado!, resultado));
});

app.MapDelete("/api/conferencia/lancamentos",
    async (string documento, string codigo, string matricula, bool confirmado,
           AbrirDocumento abrir, LancarQuantidade lancar) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    var r = await lancar.Excluir(doc, codigo, matricula, confirmado);
    if (!r.Passou) return Problema(r);
    var (atualizado, resultado) = await abrir.Executar(documento);
    return Results.Ok(Projetar(atualizado!, resultado));
});

app.MapPost("/api/conferencia/fechamento",
    async (string documento, FechamentoPedido pedido, AbrirDocumento abrir, FinalizarDocumento finalizar) =>
{
    var (doc, _) = await abrir.Executar(documento);
    if (doc is null) return Results.NotFound();
    var r = await finalizar.Executar(doc, pedido.Matricula, pedido.Confirmado);
    if (!r.Passou) return Problema(r);
    var (atualizado, resultado) = await abrir.Executar(documento);
    return Results.Ok(Projetar(atualizado!, resultado));
});

// --- consultas (AD-15) ---
app.MapGet("/api/conferencias", async (IDocumentoRepositorio repo,
    int pagina = 0, int tamanho = 50, string? busca = null, string? ordem = null) =>
{
    var filtro = new FiltroListagem(pagina, tamanho, busca, ordem);
    var itens = await repo.Listar(filtro);
    var total = await repo.ContarListagem(filtro);
    return Results.Ok(new { itens, total, pagina, tamanho = filtro.TamanhoEfetivo });
});

// --- andaime de demonstração (AD-21) ---
if (modoDemo)
{
    app.MapPost("/api/demo/semear", async (Seeder seeder) =>
    {
        if (await seeder.JaSemeado()) return Results.Ok(new { semeado = false, motivo = "já havia dado" });
        await seeder.Semear();
        return Results.Ok(new { semeado = true });
    });

    app.MapGet("/api/demo/codigos", () => Results.Ok(new[]
    {
        new { codigo = "7891234567897", efeito = "resolve · Refrigerante Cola 2L", tipo = "aceito" },
        new { codigo = "7891234500013", efeito = "resolve · Cerveja Pilsen (item com divergência)", tipo = "aceito" },
        new { codigo = "7891234511019", efeito = "resolve · Água Mineral", tipo = "aceito" },
        new { codigo = "7899999000123", efeito = "não existe em nenhum produto", tipo = "recusado" },
        new { codigo = "7890000111222", efeito = "existe em dois produtos — leitura ambígua", tipo = "ambiguo" },
        new { codigo = "7894455000012", efeito = "existe, mas não pertence a este documento", tipo = "recusado" }
    }));
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
        situacao = i.SituacaoAtual.ToString()
    })
};

record LeituraPedido(string Codigo);
record LancamentoPedido(string Codigo, decimal Quantidade, string Matricula, bool Confirmado);
record FechamentoPedido(string Matricula, bool Confirmado);
