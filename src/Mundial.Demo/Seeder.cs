using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Mundial.Dominio;

namespace Mundial.Demo;

/// <summary>
/// AD-21: andaime removível. Só roda com MODO_DEMO=true, nunca entra em migration.
/// FR-50: planta os cinco estados de exceção que a demonstração precisa mostrar.
/// </summary>
public sealed class Seeder(string connectionString)
{
    private static readonly PasswordHasher<string> Hasher = new();
    private static string Hash(string senha) => Hasher.HashPassword("mundial", senha);

    /// <summary>
    /// Verifica a ÚLTIMA tabela semeada, não a primeira: assim uma falha no meio não trava
    /// o seed para sempre — que foi exatamente o que aconteceu na primeira subida.
    /// </summary>
    public async Task<bool> JaSemeado()
    {
        await using var c = new SqlConnection(connectionString);
        return await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.conferencia") > 0;
    }

    /// <summary>Tudo ou nada: se qualquer passo falhar, o banco volta ao estado vazio.</summary>
    public async Task Semear()
    {
        await using var c = new SqlConnection(connectionString);
        await c.OpenAsync();
        await c.ExecuteAsync(@"
            DELETE FROM dbo.conferencia; DELETE FROM dbo.acesso;
            DELETE FROM dbo.estoq; DELETE FROM dbo.forne; DELETE FROM dbo.usuario;");

        // --- usuários: perfis distintos (FR-49) ---
        await c.ExecuteAsync(@"
            INSERT INTO dbo.usuario (matric, nome, senha_hash, niv_usu, loja) VALUES
              (@m1, 'CLEBER SANTOS',        @h, '3', '001'),
              (@m2, 'ROSANA MEIRELES',      @h, '3', '001'),
              (@m3, 'MARCOS TEIXEIRA',      @h, '3', '001'),
              (@m4, 'PAULO ANDRADE',        @h, '1', '001');",
            new { m1 = "04127", m2 = "04310", m3 = "04982", m4 = "05001", h = Hash("mundial") });

        // Cleber: opera, não cadastra. Rosana: tudo. Marcos: opera. Paulo: nível insuficiente.
        var permissoes = new List<Permissao>();
        void Perm(string m, string tabela, bool cons, bool inc, bool alt, bool exc, string desc)
            => permissoes.Add(new Permissao(m, Tabelas.Chave(tabela), desc, cons, inc, alt, exc));

        foreach (var (mat, podeIncluir) in new[] { ("04127", false), ("04310", true), ("04982", false) })
        {
            Perm(mat, "conferencia", true, podeIncluir, true, true, "Conferencia de NF");
            Perm(mat, "estoq", true, podeIncluir, podeIncluir, podeIncluir, "Cadastro de produtos");
            Perm(mat, "forne", true, false, false, false, "Fornecedores");
            Perm(mat, "log_even", mat == "04310", false, false, false, "Auditoria");
        }
        await c.ExecuteAsync(@"
            INSERT INTO dbo.acesso (matric, arquivo, descri, consultar, incluir, alterar, excluir)
            VALUES (@Matricula, @Tabela, @Descricao, @Consultar, @Incluir, @Alterar, @Excluir)", permissoes);

        // --- fornecedores ---
        await c.ExecuteAsync(@"
            INSERT INTO dbo.forne (codfor, descri, cgc, cod_com, categ, tiplog, lograd, bairro, cep,
                                   cidade, uf, inscr, situacao, data_grav, sub_trib, Mov_Est) VALUES
              ('00110','BEBIDAS PRIMAVERA LTDA','12.345.678/0001-90','00021','01','RUA','DAS INDUSTRIAS','DISTRITO INDUSTRIAL','21540-000','RIO DE JANEIRO','RJ','86.412.330','A',SYSUTCDATETIME(),1,1),
              ('00120','LATICINIOS SERRA AZUL SA','98.765.432/0001-10','00034','02','ROD','BR-040 KM 32','ZONA RURAL','35701-970','SETE LAGOAS','MG','12.998.771','A',SYSUTCDATETIME(),0,1),
              ('00130','HIGIENE TOTAL DISTRIBUIDORA','45.221.907/0001-55','00012','03','AV','PRESIDENTE VARGAS','CENTRO','20040-020','RIO DE JANEIRO','RJ','77.310.442','A',SYSUTCDATETIME(),1,1);");

        // --- produtos: EAN-13 e DUN-14 coerentes entre si ---
        await c.ExecuteAsync(@"
            INSERT INTO dbo.estoq (codigo, descri, embalag, embalqt, codbarr, codbarr2, codbarr3,
                                   barr_emb, barr_emb2, barr_emb3) VALUES
              ('04127','REFRIGERANTE COLA 2L','CX c/ 6',6,'7891234567897',NULL,NULL,'17891234567894',NULL,NULL),
              ('04982','CERVEJA PILSEN LATA 350ML','CX c/ 12',12,'7891234500013',NULL,NULL,'17891234500010',NULL,NULL),
              ('05310','AGUA MINERAL S/GAS 500ML','FD c/ 12',12,'7891234511019',NULL,NULL,'17891234511016',NULL,NULL),
              ('05877','SUCO UVA INTEGRAL 1L','CX c/ 6',6,'7891234522015',NULL,NULL,'17891234522012',NULL,NULL),
              ('06120','LEITE INTEGRAL 1L','CX c/ 12',12,'7899876500019',NULL,NULL,'17899876500016',NULL,NULL),
              ('06430','SABAO EM PO 1KG','CX c/ 10',10,'7894455000012',NULL,NULL,'17894455000019',NULL,NULL),
              -- FR-50: mesmo código de barras em dois produtos, para demonstrar a leitura ambígua
              ('07001','BISCOITO RECHEADO 140G','CX c/ 30',30,'7890000111222',NULL,NULL,'17890000111229',NULL,NULL),
              ('07002','BISCOITO RECHEADO 140G PROMO','CX c/ 30',30,'7890000111222',NULL,NULL,'17890000111229',NULL,NULL);");

        // --- documentos ---
        // doca 1: em conferência, com divergência plantada
        await Documento(c, "00001","00110","NFE","1","000148372","00110", 1, "000148372/1", -22,
            [("04127", 40, 40), ("04982", 120, 114), ("05310", 60, 60), ("05877", 24, 0)]);
        // doca 2: aberta há muito tempo, pouco lançada
        await Documento(c, "00001","00120","NFE","1","000147901","00120", 2, "000147901/1", -221,
            [("06120", 200, 60), ("05310", 80, 0), ("06430", 50, 0)]);
        // doca 3: aguardando
        await Documento(c, "00001","00130","NFE","2","000148415","00130", 3, "000148415/2", -4,
            [("06430", 120, 0), ("07001", 30, 0)]);
        // doca 4: já fechada
        await Documento(c, "00001","00130","NFE","1","000147744","00130", 4, "000147744/1", -43,
            [("05877", 36, 36), ("06120", 90, 90)], fechado: true, matrFec: "04127");

        // FR-50: item pendente por código não cadastrado, na doca 1
        await c.ExecuteAsync(@"
            UPDATE dbo.conferencia SET pendencia = 1
             WHERE RTRIM(acesso) = '000148372/1' AND RTRIM(codigo) = '05877'");
    }

    private sealed record Permissao(string Matricula, string Tabela, string Descricao,
        bool Consultar, bool Incluir, bool Alterar, bool Excluir);

    private static async Task Documento(SqlConnection c, string filial, string orig, string tipo,
        string serie, string numero, string codfor, int doca, string exibido, int minutosAtras,
        (string codigo, decimal qtdNf, decimal qtdRec)[] itens, bool fechado = false, string? matrFec = null)
    {
        var it = 1;
        foreach (var (codigo, qtdNf, qtdRec) in itens)
        {
            var situacao = fechado ? "F" : qtdRec > 0 ? "C" : "A";
            await c.ExecuteAsync(@"
                INSERT INTO dbo.conferencia
                    (filial, orig_des, tipo_doc, SERIE, numero, codigo, itnf, dun14, data_mov,
                     QTD_NF, QTD_REC, QTD_UNID_NF, QTD_UNID_REC, matr_conf, matr_fec, dt_hora,
                     situacao, acesso, fechado, doca, codfor, peso_bruto_col, balanca)
                VALUES (@filial, @orig, @tipo, @serie, @numero, @codigo, @it,
                        (SELECT barr_emb FROM dbo.estoq WHERE RTRIM(codigo) = @codigo),
                        DATEADD(MINUTE, @min, SYSUTCDATETIME()),
                        @qtdNf, @qtdRec, @qtdNf, @qtdRec, @matrConf, @matrFec,
                        CASE WHEN @fechado = 1 THEN SYSUTCDATETIME() ELSE NULL END,
                        @situacao, @exibido, @fechado, @doca, @codfor, 0, 0)",
                new
                {
                    filial, orig, tipo, serie, numero, codigo, it = it++,
                    min = minutosAtras, qtdNf, qtdRec,
                    matrConf = qtdRec > 0 || fechado ? (doca == 2 ? "04982" : "04127") : "",
                    matrFec = matrFec ?? "", fechado, situacao, exibido, doca, codfor
                });
        }
    }
}
