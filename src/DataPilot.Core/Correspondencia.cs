namespace DataPilot.Core;

/// <summary>Funções puras que ligam a lista de sessões (pasta de status) ao que o painel registrou.</summary>
public static class Correspondencia
{
    /// <summary>
    /// A sessão registrada que corresponde à da lista, por identificador e agente; null se o painel não a abriu.
    /// O que sai daqui para a ação "Retomar" é sempre o tipo do arquivo de dados.
    /// </summary>
    public static SessaoRegistrada? Registrada(Sessao sessao, IEnumerable<RepositorioCadastrado> repositorios)
    {
        foreach (var repo in repositorios)
            foreach (var r in repo.Sessoes)
                if (r.Agente == sessao.Agente
                    && string.Equals(r.SessionId, sessao.SessionId, StringComparison.OrdinalIgnoreCase))
                    return r;
        return null;
    }

    /// <summary>Avisa antes de retomar quando a sessão não está encerrada nem inativa.</summary>
    public static bool ExigeConfirmacao(StatusExibido status)
        => status is not (StatusExibido.Encerrada or StatusExibido.Inativa);
}
