using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Mundial.Api;

/// <summary>
/// OpenTelemetry: traces, métricas e logs saem em OTLP para o coletor (SigNoz).
///
/// Toda a configuração de destino vem das variáveis padrão do OTEL — OTEL_EXPORTER_OTLP_ENDPOINT,
/// _PROTOCOL, _HEADERS, OTEL_SERVICE_NAME, OTEL_RESOURCE_ATTRIBUTES — lidas pelo próprio SDK.
/// Não existe nome de variável inventado aqui, e trocar de coletor é trocar o .env.
///
/// Sem OTEL_EXPORTER_OTLP_ENDPOINT nada é registrado: a POC sobe de checkout limpo sem coletor
/// nenhum (NFR-11), e a instrumentação não custa um span sequer.
/// </summary>
public static class Telemetria
{
    /// <summary>Origem dos spans de negócio — o que a instrumentação automática não sabe nomear.</summary>
    public const string NomeFonte = "Mundial";

    public static readonly ActivitySource Fonte = new(NomeFonte);

    public static WebApplicationBuilder AdicionarTelemetria(this WebApplicationBuilder builder)
    {
        var destino = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (string.IsNullOrWhiteSpace(destino)) return builder;

        var servico = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "mundial-api";

        // VERSAO é gravada na imagem pelo build (ARG no Dockerfile; o deploy passa o commit).
        // Sem ela a versão do assembly nunca muda entre commits, e o SigNoz não tem como
        // responder em qual deploy a latência mudou — que é metade do motivo de ter isto.
        var versao = Environment.GetEnvironmentVariable("VERSAO") is { Length: > 0 } marcada
            ? marcada
            : typeof(Telemetria).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(recurso => recurso.AddService(servico, serviceVersion: versao))
            .WithTracing(rastros => rastros
                .AddAspNetCoreInstrumentation(o =>
                {
                    // /api/saude é sondado pelo healthcheck do compose a cada 10s. Traçar isso
                    // enche o SigNoz de ruído e some com o sinal das rotas que interessam.
                    o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/api/saude");
                    o.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                // Sem opções de propósito. O span de banco sai com o texto da consulta em
                // db.query.text, mas parametrizado — "WHERE matric = @m", nunca o valor.
                // A captura de parâmetros, que é o que levaria número de nota, código de
                // fornecedor e matrícula para fora da máquina, vem desligada e fica assim.
                .AddSqlClientInstrumentation()
                .AddSource(NomeFonte)
                // Antes do exportador, e não depois: os processadores rodam na ordem em que
                // são registrados, e o AddOtlpExporter registra o dele. Invertido, o span
                // sairia da máquina antes de receber os atributos espelhados.
                .AddProcessor(new EspelhoDeConvencaoDeBanco())
                .AddOtlpExporter())
            .WithMetrics(metricas => metricas
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                // db.client.operation.duration: latência e taxa de erro do banco como série
                // temporal. Sem isto, banco só existia span a span — dá para investigar uma
                // conferência lenta, não para ver a lentidão chegando nem para alertar.
                .AddSqlClientInstrumentation()
                // Contadores de negócio: recusa por regra, bipagem por desfecho, fecho com
                // divergência. Ver Metricas.cs.
                .AddMeter(Metricas.Medidor.Name)
                .AddOtlpExporter());

        RegistrarMetricasDeProcesso();

        builder.Logging.AddOpenTelemetry(o =>
        {
            // Com o log dentro do request, o SDK carimba trace_id e span_id em cada linha —
            // é isso que liga o log ao trace na tela do SigNoz.
            o.IncludeScopes = true;
            o.IncludeFormattedMessage = true;
            o.AddOtlpExporter();
        });

        return builder;
    }

    /// <summary>
    /// Os instrumentos observáveis precisam ficar vivos: soltos, o coletor de lixo os leva e a
    /// série simplesmente para de chegar, sem erro nenhum.
    /// </summary>
    private static readonly List<object> InstrumentosDeProcesso = [];

    /// <summary>
    /// Métricas do processo. A instrumentação de Runtime já cobre GC, heap e thread pool — o que
    /// falta é o que o sistema operacional vê: memória residente, CPU e descritores de arquivo.
    ///
    /// À mão, sem OpenTelemetry.Instrumentation.Process de propósito: esse pacote só existe em
    /// prerelease, e o resto do projeto está inteiro em versões estáveis. São cinco medidas; não
    /// vale trazer um beta para dentro do deploy por elas.
    /// </summary>
    private static void RegistrarMetricasDeProcesso()
    {
        // O Meter é estático e sobrevive ao builder. Chamado duas vezes — um teste que sobe dois
        // hosts, por exemplo —, registraria o mesmo instrumento de novo no mesmo Meter, e a
        // mesma medida sairia duplicada a cada coleta.
        if (InstrumentosDeProcesso.Count > 0) return;

        var medidor = Metricas.Medidor;

        InstrumentosDeProcesso.Add(medidor.CreateObservableUpDownCounter(
            "process.memory.usage", () => DoProcesso(p => p.WorkingSet64), "By",
            "Memória residente do processo."));

        InstrumentosDeProcesso.Add(medidor.CreateObservableUpDownCounter(
            "process.memory.virtual", () => DoProcesso(p => p.VirtualMemorySize64), "By",
            "Memória virtual reservada pelo processo."));

        InstrumentosDeProcesso.Add(medidor.CreateObservableCounter(
            "process.cpu.time", TemposDeCpu, "s",
            "Tempo de CPU consumido pelo processo, separado por modo."));

        InstrumentosDeProcesso.Add(medidor.CreateObservableUpDownCounter(
            "process.thread.count", () => DoProcesso(p => (long)p.Threads.Count), "{thread}",
            "Threads do processo."));

        // Só faz sentido em Linux, que é onde o container roda. Em outro sistema a leitura
        // falharia a cada coleta e encheria o log de exceção por nada.
        if (OperatingSystem.IsLinux())
            InstrumentosDeProcesso.Add(medidor.CreateObservableUpDownCounter(
                "process.open_file_descriptors", DescritoresAbertos, "{fd}",
                "Descritores abertos — socket vazando aparece aqui muito antes de virar queda."));
    }

    private static long DoProcesso(Func<Process, long> leitura)
    {
        // Instância nova a cada coleta: a que fica guardada devolve o valor congelado da
        // primeira leitura, e o gráfico vira uma reta.
        using var processo = Process.GetCurrentProcess();
        return leitura(processo);
    }

    private static IEnumerable<Measurement<double>> TemposDeCpu()
    {
        using var processo = Process.GetCurrentProcess();
        return
        [
            new(processo.UserProcessorTime.TotalSeconds,
                new KeyValuePair<string, object?>("cpu.mode", "user")),
            new(processo.PrivilegedProcessorTime.TotalSeconds,
                new KeyValuePair<string, object?>("cpu.mode", "system")),
        ];
    }

    private static long DescritoresAbertos()
    {
        try
        {
            return Directory.GetFileSystemEntries("/proc/self/fd").LongLength;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Telemetria que derruba o processo é pior que telemetria faltando.
            return 0;
        }
    }

    /// <summary>
    /// Carimba o span do request em curso. Uso: marcar o documento conferido e a regra que
    /// recusou a operação, que é por onde se procura no SigNoz quando o armazém reclama.
    /// Nunca recebe senha nem token.
    /// </summary>
    public static void Marcar(string chave, object? valor)
    {
        if (valor is not null) Activity.Current?.SetTag(chave, valor);
    }
}

/// <summary>
/// Repete os atributos de banco nos nomes da convenção antiga.
///
/// A instrumentação do SqlClient no 1.17 já migrou para a convenção estável — o span sai com
/// db.system.name, db.namespace e db.query.text, e nenhum dos nomes antigos. A tela de banco do
/// SigNoz procura db.system: sem ele os spans existem, aparecem no trace da requisição, e a tela
/// que responde "o banco está lento?" fica vazia como se a instrumentação não existisse.
///
/// OTEL_SEMCONV_STABILITY_OPT_IN=database/dup não resolve — testado contra esta versão, continua
/// saindo só o nome novo. Daí espelhar aqui, que é o único ponto sob nosso controle.
///
/// Isto é ponte para o coletor de hoje, não convenção nossa: quando o SigNoz ler os nomes
/// estáveis, esta classe sai inteira e nada mais muda.
/// </summary>
internal sealed class EspelhoDeConvencaoDeBanco : BaseProcessor<Activity>
{
    public override void OnEnd(Activity atividade)
    {
        // Só spans de banco; os outros saem daqui na primeira linha.
        if (atividade.GetTagItem("db.system.name") is not string sistema) return;

        // Se um dia a instrumentação voltar a emitir os dois, quem manda é ela.
        if (atividade.GetTagItem("db.system") is not null) return;

        // O valor também mudou: era "mssql" na convenção antiga, virou "microsoft.sql_server".
        // Espelhar o nome sem traduzir o valor não adiantaria nada — o filtro da tela é por
        // db.system=mssql.
        atividade.SetTag("db.system", sistema == "microsoft.sql_server" ? "mssql" : sistema);

        if (atividade.GetTagItem("db.namespace") is string banco)
            atividade.SetTag("db.name", banco);

        // Já parametrizado pela instrumentação; nenhum valor de negócio entra aqui.
        if (atividade.GetTagItem("db.query.text") is string consulta)
            atividade.SetTag("db.statement", consulta);
    }
}
