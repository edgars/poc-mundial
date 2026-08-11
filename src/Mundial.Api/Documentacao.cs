using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Mundial.Api;

/// <summary>
/// Spec OpenAPI 3.1 + Swagger UI. Tudo mora sob /api porque o proxy da POC só encaminha
/// esse prefixo (infra/terraform/modelos/Caddyfile.tftpl) — assim a documentação funciona
/// igual no compose local e na máquina publicada, sem tocar no Caddy.
/// </summary>
public static class Documentacao
{
    public const string PadraoSpec = "/api/openapi/{documentName}.json";
    public const string SpecV1 = "/api/openapi/v1.json";
    public const string CaminhoInterface = "api/docs";
    public const string CaminhoToken = "/api/oauth/token";

    /// <summary>Troca matrícula+senha por bearer, direto do botão Authorize.</summary>
    public const string EsquemaSenha = "senha";

    /// <summary>Cola um bearer já emitido (o mesmo que /api/entrar devolve).</summary>
    public const string EsquemaToken = "token";

    public static IServiceCollection AdicionarDocumentacao(this IServiceCollection servicos) =>
        servicos.AddOpenApi(opcoes =>
        {
            opcoes.AddDocumentTransformer((doc, _, _) =>
            {
                doc.Info = new OpenApiInfo
                {
                    Title = "Mundial · Conferência de Recebimento",
                    Version = "v1",
                    Description =
                        "API da POC. Toda rota fora de /api/saude e /api/entrar exige bearer JWT " +
                        "e a permissão da tabela que a operação toca (AD-7, AD-8). " +
                        "Use Authorize → senha para autenticar com matrícula e senha."
                };

                doc.Components ??= new OpenApiComponents();
                doc.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    [EsquemaSenha] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Description = "Matrícula no campo username, senha no password. " +
                                      "O Swagger guarda o bearer devolvido e o envia sozinho.",
                        Flows = new OpenApiOAuthFlows
                        {
                            Password = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(CaminhoToken, UriKind.Relative),
                                Scopes = new Dictionary<string, string>()
                            }
                        }
                    },
                    [EsquemaToken] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Cole aqui o token devolvido por POST /api/entrar."
                    }
                };

                return Task.CompletedTask;
            });

            // Cadeado só onde há política de autorização — /api/saude e /api/entrar ficam abertos.
            opcoes.AddOperationTransformer((operacao, contexto, _) =>
            {
                var exigeAuth = contexto.Description.ActionDescriptor.EndpointMetadata
                    .OfType<IAuthorizeData>().Any();
                if (!exigeAuth) return Task.CompletedTask;

                // Duas exigências separadas = alternativas: vale o fluxo de senha OU o bearer colado.
                // A referência só vira $ref se souber o documento que a hospeda.
                operacao.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(EsquemaSenha, contexto.Document)] = new List<string>()
                    },
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(EsquemaToken, contexto.Document)] = new List<string>()
                    }
                ];

                var politica = contexto.Description.ActionDescriptor.EndpointMetadata
                    .OfType<IAuthorizeData>().Select(a => a.Policy).FirstOrDefault(p => p is not null);
                if (politica is not null)
                    operacao.Description = $"Exige a permissão `{politica}`.{
                        (string.IsNullOrWhiteSpace(operacao.Description) ? "" : " " + operacao.Description)}";

                return Task.CompletedTask;
            });
        });

    public static void UsarDocumentacao(this WebApplication app)
    {
        app.MapOpenApi(PadraoSpec).AllowAnonymous();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint(SpecV1, "Mundial · Conferência v1");
            o.RoutePrefix = CaminhoInterface;
            o.DocumentTitle = "Mundial · Conferência — API";
            // O fluxo de senha do OAuth2 não usa client secret; o campo fica escondido.
            o.OAuthClientId("swagger-ui");
        });
    }
}
