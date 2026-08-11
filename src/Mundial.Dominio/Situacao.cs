namespace Mundial.Dominio;

/// <summary>
/// conferencia.situacao é char(1) e nenhum artefato do RNC define o domínio — só que é obrigatório
/// (RK-16bc1acd7b74). Convenção adotada em A-9; ver seção 10 do PRD.
/// TODO(A-9): confirmar com a Mundial se o legado já usa outra convenção.
/// </summary>
public static class Situacao
{
    public const char Aguardando = 'A';
    public const char EmConferencia = 'C';
    public const char Fechada = 'F';

    public static string Descrever(char c) => c switch
    {
        Aguardando => "Aguardando",
        EmConferencia => "Em conferência",
        Fechada => "Fechada",
        _ => "Indefinida"
    };
}
