namespace DataPilot.Core.Models;

#region "Enums"

    public enum Agente
    {
        Claude,
        Codex
    }

    public enum StatusGravado
    {
        Ativa,
        Executando,
        Aguardando,
        Atencao,
        Encerrada
    }

    public enum StatusExibido
    {
        PrecisaAtencao = 0,
        Aguardando = 1,
        Executando = 2,
        Ativa = 3,
        Inativa = 4,
        Encerrada = 5,
        /// <summary>Sessão registrada sem arquivo de status. Último na ordem e fora do resumo do cabeçalho.</summary>
        SemStatus = 6,
    }

#endregion

public sealed record Sessao(Agente Agente, string SessionId, string Cwd, StatusGravado Status, DateTimeOffset Atualizado, DateTimeOffset? ArquivoCriadoEm = null);
