using Mundial.Dominio;

namespace Mundial.Aplicacao;

/// <summary>AD-16: `estoq` é escrita apenas por este caso de uso.</summary>
public interface IProdutoRepositorio : IProdutoConsulta
{
    Task<Produto?> DonoDoCodigoDeBarras(string codigoBarras, string exceroCodigoProduto,
        CancellationToken ct = default);
    Task Salvar(Produto produto, CancellationToken ct = default);
}

public sealed record PedidoCadastro(string Codigo, string[] Dun, bool Confirmado);

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
