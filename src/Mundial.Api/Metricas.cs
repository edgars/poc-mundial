using System.Diagnostics.Metrics;
using Mundial.Dominio;

namespace Mundial.Api;

/// <summary>
/// Instrumentos de negócio. O span responde "o que aconteceu nesta requisição"; o contador
/// responde "quantas vezes hoje" — que é a pergunta do armazém — sem varrer trace e sem depender
/// de amostragem. Contar recusa por span custa uma agregação sobre milhões de registros; contar
/// por contador custa uma soma.
///
/// A cardinalidade aqui é escolhida, não herdada. Entram estado da leitura, chave de regra e
/// desfecho: conjuntos fechados, alguns poucos valores cada. Número de documento, código de
/// produto e matrícula ficam de fora — em métrica cada valor distinto vira uma série que não
/// morre mais, e são exatamente os três campos que a instrumentação de traces já mantém fora do
/// coletor (Telemetria.cs § SqlClient).
/// </summary>
public static class Metricas
{
    /// <summary>Mesmo nome da fonte de spans: um só lugar para procurar no SigNoz.</summary>
    public static readonly Meter Medidor = new(Telemetria.NomeFonte);

    private static readonly Counter<long> Leituras = Medidor.CreateCounter<long>(
        "mundial.leituras", "{leitura}",
        "Códigos bipados, por desfecho. Bipagem que não resolve é fila parada na doca.");

    private static readonly Counter<long> Lancamentos = Medidor.CreateCounter<long>(
        "mundial.lancamentos", "{lancamento}",
        "Lançamentos e estornos de quantidade, por desfecho.");

    private static readonly Counter<long> Finalizacoes = Medidor.CreateCounter<long>(
        "mundial.finalizacoes", "{documento}",
        "Documentos fechados, com e sem divergência.");

    private static readonly Counter<long> Recusas = Medidor.CreateCounter<long>(
        "mundial.regras.recusas", "{recusa}",
        "Operações recusadas por regra de negócio, pela chave da regra.");

    private static readonly Histogram<int> ItensPorDocumento = Medidor.CreateHistogram<int>(
        "mundial.conferencia.itens", "{item}",
        "Itens lançados no documento no momento do fecho.");

    /// <summary>Desfecho de uma bipagem: aceito, recusado, ambiguo ou confirmar.</summary>
    public static void Leitura(string estado) =>
        Leituras.Add(1, new KeyValuePair<string, object?>("leitura.estado", estado));

    /// <param name="operacao">lancar ou estornar.</param>
    public static void Lancamento(ResultadoRegra r, string operacao) =>
        Lancamentos.Add(1,
            new KeyValuePair<string, object?>("lancamento.operacao", operacao),
            new KeyValuePair<string, object?>("lancamento.resultado", Desfecho(r)));

    public static void Finalizacao(ResultadoRegra r, bool comDivergencia, int itensLancados)
    {
        Finalizacoes.Add(1,
            new KeyValuePair<string, object?>("finalizacao.resultado", Desfecho(r)),
            new KeyValuePair<string, object?>("conferencia.divergencia", comDivergencia));

        // Só o fecho que passou descreve um documento conferido; contar o recusado misturaria
        // tentativa com conclusão no mesmo histograma.
        if (r.Passou) ItensPorDocumento.Record(itensLancados);
    }

    /// <summary>
    /// Toda recusa da API passa por aqui, porque toda recusa passa pelo Problema() do Program.cs.
    /// Uma operação recusada é contada duas vezes de propósito — uma no contador da sua etapa,
    /// outra aqui —, porque as perguntas são diferentes: "quantos lançamentos falharam" e "qual
    /// regra está recusando".
    /// </summary>
    public static void Recusa(ResultadoRegra r)
    {
        if (r.Passou) return;
        Recusas.Add(1,
            // Conflito de gravação (AD-17) não tem chave de regra; sem o rótulo a série sumiria.
            new KeyValuePair<string, object?>("regra.chave", r.Chave ?? "sem-chave"),
            new KeyValuePair<string, object?>("regra.tipo", r.Tipo.ToString()));
    }

    private static string Desfecho(ResultadoRegra r) => r.Passou ? "gravado" : r.Tipo.ToString();
}
