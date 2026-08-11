using Mundial.Dominio;

namespace Mundial.Aplicacao;

public sealed record ItemResolvido(
    string Codigo, string Descricao, string? Embalagem, decimal? EmbalagemQtd,
    string? Dun14, decimal QtdNf, decimal QtdRec, bool Pendencia);

public sealed record LeituraResultado(
    string Estado,                     // aceito | recusado | ambiguo | confirmar
    string? Chave,
    string? Mensagem,
    ItemResolvido? Item,
    IReadOnlyList<string>? Candidatos);

/// <summary>
/// Abre o documento que o operador bipou e devolve os itens esperados.
/// AD-14: a aplicação nunca cria linha de conferencia — elas vêm da integração da nota fiscal.
/// </summary>
public sealed class AbrirDocumento(IDocumentoRepositorio documentos)
{
    [RegraNegocio("RK-c0fce5362f62", "Documento não cadastrado!")]
    public async Task<(Documento? Documento, ResultadoRegra Resultado)> Executar(
        string numeroExibido, CancellationToken ct = default)
    {
        var doc = await documentos.PorNumeroExibido(numeroExibido.Trim(), ct);
        if (doc is null)
            return (null, ResultadoRegra.Recusa("RK-c0fce5362f62", "Documento não cadastrado!"));

        var abertura = doc.AvaliarAbertura();
        if (!abertura.Passou) return (doc, abertura);

        // Aviso com confirmação, não bloqueio (AD-6).
        var relancamento = doc.AvaliarRelancamento();
        return (doc, relancamento);
    }
}

/// <summary>
/// O operador bipa; o sistema resolve o produto.
/// FR-13: a resolução devolve exatamente um DUN-14 — ambiguidade recusa e mostra candidatos.
/// </summary>
public sealed class ResolverLeitura(IProdutoConsulta produtos)
{
    [RegraNegocio("RK-6fef4d31a290", "Código Não cadastrado!")]
    [RegraNegocio("RK-798f00f19690", "Código Não cadastrado!")]
    [RegraNegocio("RK-732bb9300bad", "Código Não cadastrado para")]
    public async Task<LeituraResultado> Executar(Documento documento, string codigoBipado,
        CancellationToken ct = default)
    {
        var codigo = codigoBipado.Trim();
        var achados = await produtos.PorCodigoDeBarras(codigo, ct);

        if (achados.Count == 0)
            return new("recusado", "RK-798f00f19690", "Código Não cadastrado!", null, null);

        if (achados.Count > 1)
            return new("ambiguo", "RK-798f00f19690",
                "Código responde a mais de um produto. Escolha qual.",
                null, achados.Select(p => $"{p.Codigo.Trim()} · {p.Descricao.Trim()}").ToList());

        var produto = achados[0];
        var item = documento.Itens.FirstOrDefault(i =>
            string.Equals(i.Codigo.Trim(), produto.Codigo.Trim(), StringComparison.OrdinalIgnoreCase));

        if (item is null)
            return new("recusado", "RK-732bb9300bad",
                $"Código Não cadastrado para {documento.NomeFornecedor?.Trim() ?? "este fornecedor"}!",
                null, null);

        var resolvido = new ItemResolvido(
            produto.Codigo.Trim(), produto.Descricao.Trim(), produto.Embalagem?.Trim(),
            produto.EmbalagemQtd, produto.DunPreenchidos.FirstOrDefault()?.Trim(),
            item.QtdNf, item.QtdRec, item.Pendencia);

        var jaLancado = item.AvaliarLancamento();
        return jaLancado.Passou
            ? new("aceito", null, null, resolvido, null)
            : new("confirmar", jaLancado.Chave, jaLancado.Mensagem, resolvido, null);
    }
}

/// <summary>AD-17: substitui, com confirmação quando já havia valor.</summary>
public sealed class LancarQuantidade(IDocumentoRepositorio documentos, IAuditoria auditoria)
{
    public async Task<ResultadoRegra> Executar(Documento documento, string codigo, decimal quantidade,
        string matricula, bool confirmado, CancellationToken ct = default)
    {
        if (documento.Fechado)
            return ResultadoRegra.Recusa("RK-69b41cd017dd", "Este Documento já foi conferido!");

        var item = documento.Itens.FirstOrDefault(i =>
            string.Equals(i.Codigo.Trim(), codigo.Trim(), StringComparison.OrdinalIgnoreCase));
        if (item is null)
            return ResultadoRegra.Recusa("RK-732bb9300bad", "Código Não cadastrado para este documento!");

        var aviso = item.AvaliarLancamento();
        if (!aviso.Passou && !confirmado) return aviso;

        var anterior = item.QtdRec;
        item.Lancar(quantidade, quantidade * (documento.Itens.Count > 0 ? 1 : 1));
        documento.RegistrarConferente(matricula);
        await documentos.GravarLancamento(item, ct);
        await auditoria.Registrar(matricula, "conferencia",
            $"{documento.Chave}/{item.Codigo.Trim()}",
            $"QTD_REC = {anterior:0.###}", $"QTD_REC = {quantidade:0.###}", ct);

        return ResultadoRegra.Ok;
    }

    /// <summary>RK-bdfbdff6c821 — exclusão de lançamento pede confirmação.</summary>
    [RegraNegocio("RK-bdfbdff6c821", "Confirma Exclusão?")]
    public async Task<ResultadoRegra> Excluir(Documento documento, string codigo, string matricula,
        bool confirmado, CancellationToken ct = default)
    {
        if (documento.Fechado)
            return ResultadoRegra.Recusa("RK-69b41cd017dd", "Este Documento já foi conferido!");
        if (!confirmado)
            return ResultadoRegra.Confirma("RK-bdfbdff6c821", "Confirma Exclusão?");

        var item = documento.Itens.First(i =>
            string.Equals(i.Codigo.Trim(), codigo.Trim(), StringComparison.OrdinalIgnoreCase));
        var anterior = item.QtdRec;
        item.LimparLancamento();
        await documentos.GravarLancamento(item, ct);
        await auditoria.Registrar(matricula, "conferencia",
            $"{documento.Chave}/{item.Codigo.Trim()}", $"QTD_REC = {anterior:0.###}", "Registro Excluido", ct);
        return ResultadoRegra.Ok;
    }
}

/// <summary>AD-10: transição atômica sobre todas as linhas do documento.</summary>
public sealed class FinalizarDocumento(IDocumentoRepositorio documentos, IAuditoria auditoria, IRelogio relogio)
{
    [RegraNegocio("RK-fa93a48fbecc", "Finalizar conferência?")]
    public async Task<ResultadoRegra> Executar(Documento documento, string matricula, bool confirmado,
        CancellationToken ct = default)
    {
        var avaliacao = documento.AvaliarFechamento();
        if (avaliacao.Tipo == TipoResultado.Recusado) return avaliacao;
        if (!confirmado) return avaliacao;

        documento.Fechar(matricula, relogio.AgoraUtc);
        await documentos.Fechar(documento, ct);
        await auditoria.Registrar(matricula, "conferencia", documento.Chave.ToString(),
            "fechado = 0", $"fechado = 1 · pendencias = {documento.ItensPendentes}", ct);
        return ResultadoRegra.Ok;
    }
}
