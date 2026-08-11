using Mundial.Dominio;

namespace Mundial.Aplicacao;

/// <summary>AD-16: `estoq` é escrita apenas por este caso de uso.</summary>
public interface IProdutoRepositorio : IProdutoConsulta
{
    Task<Produto?> DonoDoCodigoDeBarras(string codigoBarras, string exceroCodigoProduto,
        CancellationToken ct = default);
    Task Salvar(Produto produto, CancellationToken ct = default);
    Task Inserir(Produto produto, CancellationToken ct = default);
}

public sealed record PedidoCadastro(string Codigo, string[] Dun, bool Confirmado);

public sealed record PedidoProduto(string Codigo, string Descricao, string? Embalagem,
    decimal? EmbalagemQtd, string? Ean, string[] Dun);

/// <summary>
/// FR-28 — cadastro do produto em si: código, descrição, embalagem e quantidade por embalagem.
/// Antes só os três códigos DUN-14 eram editáveis, o que deixava o requisito pela metade.
/// </summary>
public sealed class ManterProduto(IProdutoRepositorio produtos, IAuditoria auditoria)
{
    [RegraNegocio("RK-5a7aaaa8862d", "Código não cadastrado!")]
    public async Task<ResultadoRegra> Criar(PedidoProduto pedido, string matricula,
        CancellationToken ct = default)
    {
        var codigo = pedido.Codigo.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            return ResultadoRegra.Recusa("RK-5a7aaaa8862d", "Informe o código do produto.");
        if (string.IsNullOrWhiteSpace(pedido.Descricao))
            return ResultadoRegra.Recusa("RK-ea5a22eaf219", "descri is required");

        if (await produtos.PorCodigo(codigo, ct) is not null)
            return ResultadoRegra.Recusa("RK-5a7aaaa8862d",
                $"O produto {codigo} já existe. Abra-o para alterar.");

        var produto = Montar(codigo, pedido);

        // As mesmas regras de duplicidade valem no cadastro novo (FR-30, FR-31).
        if (await ValidarCodigos(produto, ct) is { Passou: false } falha) return falha;

        await produtos.Inserir(produto, ct);
        await auditoria.Registrar(matricula, "estoq", codigo, "Registro Incluido",
            $"descri = {produto.Descricao}", ct);
        return ResultadoRegra.Ok;
    }

    /// <summary>FR-28: alterar descrição, embalagem e quantidade — não só os códigos de barras.</summary>
    public async Task<ResultadoRegra> Alterar(PedidoProduto pedido, string matricula,
        CancellationToken ct = default)
    {
        var codigo = pedido.Codigo.Trim();
        var atual = await produtos.PorCodigo(codigo, ct);
        if (atual is null)
            return ResultadoRegra.Recusa("RK-e84d750f340a", "Código não cadastrado!");
        if (string.IsNullOrWhiteSpace(pedido.Descricao))
            return ResultadoRegra.Recusa("RK-ea5a22eaf219", "descri is required");

        var anterior = $"descri = {atual.Descricao.Trim()} · embalag = {atual.Embalagem?.Trim()} · embalqt = {atual.EmbalagemQtd}";
        var produto = Montar(codigo, pedido);

        if (await ValidarCodigos(produto, ct) is { Passou: false } falha) return falha;

        await produtos.Salvar(produto, ct);
        await auditoria.Registrar(matricula, "estoq", codigo, anterior,
            $"descri = {produto.Descricao} · embalag = {produto.Embalagem} · embalqt = {produto.EmbalagemQtd}", ct);
        return ResultadoRegra.Ok;
    }

    private static Produto Montar(string codigo, PedidoProduto p) => new()
    {
        Codigo = codigo,
        Descricao = p.Descricao.Trim(),
        Embalagem = p.Embalagem?.Trim(),
        EmbalagemQtd = p.EmbalagemQtd,
        Ean = [string.IsNullOrWhiteSpace(p.Ean) ? null : p.Ean.Trim(), null, null],
        Dun = [Limpar(p.Dun, 0), Limpar(p.Dun, 1), Limpar(p.Dun, 2)]
    };

    private static string? Limpar(string[] dun, int i)
        => i < dun.Length && !string.IsNullOrWhiteSpace(dun[i]) ? dun[i].Trim() : null;

    private async Task<ResultadoRegra> ValidarCodigos(Produto produto, CancellationToken ct)
    {
        for (var slot = 0; slot < 3; slot++)
        {
            var valor = produto.Dun[slot];
            if (string.IsNullOrWhiteSpace(valor)) continue;

            var interna = produto.AvaliarDuplicidadeInterna(slot, valor);
            if (!interna.Passou) return interna;

            var dono = await produtos.DonoDoCodigoDeBarras(valor, produto.Codigo, ct);
            if (dono is not null)
                return ResultadoRegra.Recusa(
                    slot switch { 0 => "RK-2976e3756f6d", 1 => "RK-ab467d52fa1f", _ => "RK-f3bda1fa3b77" },
                    $"Código já cadastrado para o Produto {dono.Codigo.Trim()} — {dono.Descricao.Trim()}");
        }
        return ResultadoRegra.Ok;
    }
}

public sealed class CadastrarCodigos(IProdutoRepositorio produtos, IAuditoria auditoria)
{
    /// <summary>
    /// RK-5a7aaaa8862d / RK-e84d750f340a — produto inexistente é recusado.
    /// RK-2976e3756f6d / RK-ab467d52fa1f / RK-f3bda1fa3b77 — código já pertence a outro produto.
    /// A duplicidade interna e a confirmação de exclusão vivem no agregado Produto.
    /// </summary>
    [RegraNegocio("RK-5a7aaaa8862d", "Código não cadastrado!")]
    [RegraNegocio("RK-e84d750f340a", "Código não cadastrado!")]
    [RegraNegocio("RK-2976e3756f6d", "Código já cadastrado para o Produto ")]
    [RegraNegocio("RK-ab467d52fa1f", "Código já cadastrado para o Produto ")]
    [RegraNegocio("RK-f3bda1fa3b77", "Código já cadastrado para o Produto ")]
    public async Task<ResultadoRegra> Executar(PedidoCadastro pedido, string matricula,
        CancellationToken ct = default)
    {
        var produto = await produtos.PorCodigo(pedido.Codigo.Trim(), ct);
        if (produto is null)
            return ResultadoRegra.Recusa("RK-e84d750f340a", "Código não cadastrado!");

        var anterior = string.Join(" · ", produto.Dun.Select(d => d ?? ""));

        for (var slot = 0; slot < 3 && slot < pedido.Dun.Length; slot++)
        {
            var novo = pedido.Dun[slot]?.Trim();

            // Apagar um código existente pede confirmação antes de gravar.
            var exclusao = produto.AvaliarExclusao(slot, novo);
            if (exclusao.Tipo == TipoResultado.ExigeConfirmacao && !pedido.Confirmado) return exclusao;

            var interna = produto.AvaliarDuplicidadeInterna(slot, novo);
            if (!interna.Passou) return interna;

            if (!string.IsNullOrWhiteSpace(novo))
            {
                var dono = await produtos.DonoDoCodigoDeBarras(novo, produto.Codigo, ct);
                if (dono is not null)
                    return ResultadoRegra.Recusa(ChaveOutroProduto(slot),
                        $"Código já cadastrado para o Produto {dono.Codigo.Trim()} — {dono.Descricao.Trim()}");
            }

            produto.Dun[slot] = string.IsNullOrWhiteSpace(novo) ? null : novo;
        }

        await produtos.Salvar(produto, ct);
        await auditoria.Registrar(matricula, "estoq", produto.Codigo.Trim(),
            $"barr_emb = {anterior}",
            $"barr_emb = {string.Join(" · ", produto.Dun.Select(d => d ?? ""))}", ct);
        return ResultadoRegra.Ok;
    }

    private static string ChaveOutroProduto(int slot) => slot switch
    {
        0 => "RK-2976e3756f6d",
        1 => "RK-ab467d52fa1f",
        _ => "RK-f3bda1fa3b77"
    };
}
