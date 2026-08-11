using System.Diagnostics;
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
        var versao = typeof(Telemetria).Assembly.GetName().Version?.ToString() ?? "0.0.0";

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
                // Sem opções de propósito. No 1.17 o texto da query não é mais emitido, e a
                // captura de parâmetros — que levaria número de nota, código de fornecedor e
                // matrícula para fora da máquina — é experimental (a propriedade nem é pública
                // no release estável) e vem desligada. O span já traz servidor, duração e erro,
                // que é o que se investiga.
                .AddSqlClientInstrumentation()
                .AddSource(NomeFonte)
                .AddOtlpExporter())
            .WithMetrics(metricas => metricas
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

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
    /// Carimba o span do request em curso. Uso: marcar o documento conferido e a regra que
    /// recusou a operação, que é por onde se procura no SigNoz quando o armazém reclama.
    /// Nunca recebe senha nem token.
    /// </summary>
    public static void Marcar(string chave, object? valor)
    {
        if (valor is not null) Activity.Current?.SetTag(chave, valor);
    }
}
