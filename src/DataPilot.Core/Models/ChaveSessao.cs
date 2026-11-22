namespace DataPilot.Core.Models;

/// <summary>Chave que identifica uma sessão: o agente e o id sem diferença de maiúsculas.</summary>
public static class ChaveSessao
{
    public static (Agente, string) De(Agente agente, string sessionId) => (agente, sessionId.ToLowerInvariant());
}
