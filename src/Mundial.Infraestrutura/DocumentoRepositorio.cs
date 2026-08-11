using Dapper;
using Microsoft.Data.SqlClient;
using Mundial.Aplicacao;
using Mundial.Dominio;

namespace Mundial.Infraestrutura;

public sealed class DocumentoRepositorio(FabricaConexao fabrica, IRelogio relogio) : IDocumentoRepositorio
{
    public async Task<Documento?> PorNumeroExibido(string numero, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var linhas = (await c.QueryAsync<dynamic>(@"
            SELECT cf.filial, cf.orig_des, cf.tipo_doc, cf.SERIE, cf.numero, cf.codigo,
                   cf.dun14, cf.itnf, cf.QTD_NF, cf.QTD_REC, cf.QTD_UNID_NF, cf.QTD_UNID_REC,
                   cf.matr_conf, cf.matr_fec, cf.dt_hora, cf.fechado, cf.pendencia, cf.situacao,
                   cf.doca, cf.acesso, cf.codfor, cf.data_mov, cf.versao, fo.descri AS fornecedor, es.descri AS produto
            FROM dbo.conferencia cf
            LEFT JOIN dbo.forne fo ON RTRIM(fo.codfor) = RTRIM(cf.codfor)
            LEFT JOIN dbo.estoq es ON RTRIM(es.codigo) = RTRIM(cf.codigo)
            WHERE RTRIM(cf.acesso) = @n
            ORDER BY cf.itnf", new { n = numero })).ToList();

        if (linhas.Count == 0) return null;
        var p = linhas[0];
        var chave = new ChaveDocumento(
            ((string)p.filial).Trim(), ((string)p.orig_des).Trim(), ((string)p.tipo_doc).Trim(),
            ((string)p.SERIE).Trim(), ((string)p.numero).Trim());

        var doc = new Documento
        {
            Chave = chave,
            NumeroExibido = ((string?)p.acesso)?.Trim() ?? numero,
            Doca = (int?)p.doca,
            CodigoFornecedor = ((string?)p.codfor)?.Trim(),
            NomeFornecedor = ((string?)p.fornecedor)?.Trim(),
            DataMov = (DateTime?)p.data_mov
        };
        doc.Reidratar(((string?)p.matr_conf)?.Trim(), ((string?)p.matr_fec)?.Trim(),
                      (DateTime?)p.dt_hora, (bool?)p.fechado ?? false);

        foreach (var l in linhas)
            doc.Itens.Add(ItemConferencia.Reidratar(
                chave, ((string)l.codigo).Trim(), ((string?)l.dun14)?.Trim(), (decimal?)l.itnf,
                (decimal?)l.QTD_NF ?? 0, (decimal?)l.QTD_REC ?? 0,
                (decimal?)l.QTD_UNID_NF ?? 0, (decimal?)l.QTD_UNID_REC ?? 0,
                (bool?)l.pendencia ?? false, ((string?)l.situacao)?.FirstOrDefault() ?? Situacao.Aguardando,
                ((string?)l.produto)?.Trim(), (byte[]?)l.versao));

        return doc;
    }

    public async Task<IReadOnlyList<ResumoDoca>> Docas(CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        // UX-DR6: ordenado por tempo de doca aberta, não por número da doca.
        var linhas = await c.QueryAsync<dynamic>(@"
            WITH doc AS (
                SELECT cf.doca, RTRIM(cf.acesso) AS documento, MAX(fo.descri) AS fornecedor,
                       MAX(RTRIM(cf.matr_conf)) AS operador,
                       SUM(CASE WHEN cf.QTD_REC > 0 THEN 1 ELSE 0 END) AS lancados,
                       COUNT(*) AS total,
                       MAX(CASE WHEN cf.QTD_REC > 0 AND cf.QTD_REC <> cf.QTD_NF THEN 1 ELSE 0 END) AS divergencia,
                       MAX(CASE WHEN cf.pendencia = 1 THEN 1 ELSE 0 END) AS pendencia,
                       MIN(cf.data_mov) AS desde,
                       MIN(CAST(cf.fechado AS INT)) AS fechado
                FROM dbo.conferencia cf
                LEFT JOIN dbo.forne fo ON RTRIM(fo.codfor) = RTRIM(cf.codfor)
                WHERE cf.doca IS NOT NULL AND cf.doca > 0
                GROUP BY cf.doca, RTRIM(cf.acesso)
            )
            SELECT * FROM doc ORDER BY fechado, desde");
        return linhas.Select(l => new ResumoDoca(
            (int)l.doca,
            (int)l.fechado == 1 ? "fechada" : (int)l.lancados > 0 ? "em conferência" : "aguardando",
            (string?)l.documento, ((string?)l.fornecedor)?.Trim(),
            string.IsNullOrWhiteSpace((string?)l.operador) ? null : ((string?)l.operador)!.Trim(),
            (int)l.lancados, (int)l.total, (int)l.divergencia == 1, (int)l.pendencia == 1,
            (DateTime?)l.desde)).ToList();
    }

    public async Task<IReadOnlyList<ResumoDocumento>> Listar(FiltroListagem f, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var linhas = await c.QueryAsync<dynamic>(@"
            SELECT RTRIM(cf.acesso) AS documento, MAX(fo.descri) AS fornecedor, MAX(cf.doca) AS doca,
                   MAX(RTRIM(cf.matr_conf)) AS matr_conf, MAX(RTRIM(cf.matr_fec)) AS matr_fec,
                   SUM(CASE WHEN cf.QTD_REC > 0 THEN 1 ELSE 0 END) AS lancados, COUNT(*) AS total,
                   MAX(CASE WHEN cf.QTD_REC > 0 AND cf.QTD_REC <> cf.QTD_NF THEN 1 ELSE 0 END) AS divergencia,
                   MIN(CAST(cf.fechado AS INT)) AS fechado, MAX(cf.dt_hora) AS dt_hora
            FROM dbo.conferencia cf
            LEFT JOIN dbo.forne fo ON RTRIM(fo.codfor) = RTRIM(cf.codfor)
            WHERE (@busca IS NULL OR RTRIM(cf.acesso) LIKE '%' + @busca + '%' OR fo.descri LIKE '%' + @busca + '%')
            GROUP BY RTRIM(cf.acesso)
            ORDER BY MAX(cf.data_mov) DESC
            OFFSET @off ROWS FETCH NEXT @tam ROWS ONLY",
            new { busca = f.Busca, off = f.Offset, tam = f.TamanhoEfetivo });

        return linhas.Select(l => new ResumoDocumento(
            (string)l.documento, ((string?)l.fornecedor)?.Trim(), (int?)l.doca,
            string.IsNullOrWhiteSpace((string?)l.matr_conf) ? null : ((string?)l.matr_conf)!.Trim(),
            string.IsNullOrWhiteSpace((string?)l.matr_fec) ? null : ((string?)l.matr_fec)!.Trim(),
            (int)l.lancados, (int)l.total, (int)l.divergencia == 1,
            (int)l.fechado == 1 ? Situacao.Fechada : (int)l.lancados > 0 ? Situacao.EmConferencia : Situacao.Aguardando,
            (DateTime?)l.dt_hora)).ToList();
    }

    public async Task<int> ContarListagem(FiltroListagem f, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        return await c.ExecuteScalarAsync<int>(@"
            SELECT COUNT(DISTINCT RTRIM(cf.acesso)) FROM dbo.conferencia cf
            LEFT JOIN dbo.forne fo ON RTRIM(fo.codfor) = RTRIM(cf.codfor)
            WHERE (@busca IS NULL OR RTRIM(cf.acesso) LIKE '%' + @busca + '%' OR fo.descri LIKE '%' + @busca + '%')",
            new { busca = f.Busca });
    }

    /// <summary>
    /// AD-17: concorrência otimista. O UPDATE só grava se a linha ainda estiver na versão que este
    /// operador leu. Zero linhas afetadas significa que outra pessoa gravou no meio.
    /// </summary>
    public async Task<bool> GravarLancamento(ItemConferencia item, CancellationToken ct = default)
    {
        using var c = fabrica.Abrir();
        var afetadas = await c.ExecuteAsync(@"
            UPDATE dbo.conferencia
               SET QTD_REC = @qtd, QTD_UNID_REC = @qtdUnid, pendencia = @pend,
                   situacao = @sit, data_conf = @agora
             WHERE RTRIM(filial) = @filial AND RTRIM(orig_des) = @orig AND RTRIM(tipo_doc) = @tipo
               AND RTRIM(SERIE) = @serie AND RTRIM(numero) = @numero AND RTRIM(codigo) = @codigo
               AND (@versao IS NULL OR versao = @versao)",
            new
            {
                qtd = item.QtdRec, qtdUnid = item.QtdUnidRec, pend = item.Pendencia,
                sit = item.SituacaoAtual.ToString(), agora = relogio.AgoraUtc,
                filial = item.Documento.Filial, orig = item.Documento.OrigDes, tipo = item.Documento.TipoDoc,
                serie = item.Documento.Serie, numero = item.Documento.Numero, codigo = item.Codigo.Trim(),
                versao = item.Versao
            });
        return afetadas > 0;
    }

    /// <summary>AD-10: fecha todas as linhas do documento em transação única.</summary>
    public async Task Fechar(Documento documento, CancellationToken ct = default)
    {
        using var c = (SqlConnection)fabrica.Abrir();
        await c.OpenAsync(ct);
        using var tx = c.BeginTransaction();
        try
        {
            await c.ExecuteAsync(@"
                UPDATE dbo.conferencia
                   SET fechado = 1, matr_fec = @matr, dt_hora = @quando, situacao = @sit
                 WHERE RTRIM(filial) = @filial AND RTRIM(orig_des) = @orig AND RTRIM(tipo_doc) = @tipo
                   AND RTRIM(SERIE) = @serie AND RTRIM(numero) = @numero",
                new
                {
                    matr = documento.MatrFec, quando = documento.DtHora, sit = Situacao.Fechada.ToString(),
                    filial = documento.Chave.Filial, orig = documento.Chave.OrigDes,
                    tipo = documento.Chave.TipoDoc, serie = documento.Chave.Serie, numero = documento.Chave.Numero
                }, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }
}
