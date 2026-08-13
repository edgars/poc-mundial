using System.Diagnostics;
using System.Reflection;
using DbUp;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// AD-9: toda mudança de schema é um script DbUp numerado, executado exatamente uma vez.
// AD-21: seed nunca entra aqui — migration é schema, seed é dado de demonstração.

var conexao = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException("CONNECTION_STRING não definida. Veja .env.example.");

// NFR-15: o container de migrações roda uma vez e some. Sem trace, uma migração lenta ou travada
// só se manifesta como a api presa em depends_on, sem dizer em qual script nem por quanto tempo.
var fonte = new ActivitySource("Mundial.Migrations");
using var rastros = CriarRastros();
using var execucao = fonte.StartActivity("migracoes");

Console.WriteLine("Aguardando o banco responder...");
using (var espera = fonte.StartActivity("aguardar-banco"))
{
    for (var tentativa = 1; tentativa <= 40; tentativa++)
    {
        try
        {
            EnsureDatabase.For.SqlDatabase(conexao);
            espera?.SetTag("banco.tentativas", tentativa);
            break;
        }
        catch (Exception ex) when (tentativa < 40)
        {
            Console.WriteLine($"  tentativa {tentativa}/40 — {ex.Message.Split('\n')[0]}");
            Thread.Sleep(3000);
        }
    }
}

var upgrader = DeployChanges.To
    .SqlDatabase(conexao)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

// Um span por script, em vez de um PerformUpgrade() só. O motor é o mesmo e o journal também —
// a tabela SchemaVersions não distingue de onde a execução veio —, e o DbUp já roda sem
// transação envolvente por padrão, então rodar um de cada vez não muda a semântica. O que muda é
// poder apontar qual script levou os quarenta segundos.
var pendentes = upgrader.GetScriptsToExecute();
execucao?.SetTag("db.migration.pendentes", pendentes.Count);

foreach (var script in pendentes)
{
    using var passo = fonte.StartActivity("migracao.script");
    passo?.SetTag("db.migration.script", script.Name);

    var resultado = DeployChanges.To
        .SqlDatabase(conexao)
        .WithScripts(script)
        .LogToConsole()
        .Build()
        .PerformUpgrade();

    if (!resultado.Successful)
    {
        passo?.SetStatus(ActivityStatusCode.Error, resultado.Error?.Message);
        execucao?.SetStatus(ActivityStatusCode.Error, $"falhou em {script.Name}");
        Console.Error.WriteLine(resultado.Error);
        return 1;
    }
}

Console.WriteLine("Schema atualizado.");
return 0;

/// <summary>
/// Sem OTEL_EXPORTER_OTLP_ENDPOINT devolve null e nada é registrado — a POC sobe de checkout
/// limpo sem coletor nenhum (NFR-11), igual à API. O destino, o protocolo e o cabeçalho vêm das
/// variáveis padrão do OTEL, lidas pelo próprio SDK.
/// </summary>
TracerProvider? CriarRastros()
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
        return null;

    return Sdk.CreateTracerProviderBuilder()
        .ConfigureResource(recurso => recurso.AddService(
            // O compose dá a este container um OTEL_SERVICE_NAME próprio: no SigNoz as migrações
            // são um serviço à parte da api, com ciclo de vida de segundos, não de dias.
            Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "mundial-migracoes",
            serviceVersion: Environment.GetEnvironmentVariable("VERSAO")))
        .AddSource("Mundial.Migrations")
        // Cada script vira um comando SQL medido, inclusive as tentativas de conexão do laço de
        // espera — que é exatamente o sintoma de "o banco ainda não subiu".
        .AddSqlClientInstrumentation()
        .AddOtlpExporter()
        .Build();
}
