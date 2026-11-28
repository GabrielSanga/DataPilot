using DataPilot.Core.Models;

namespace DataPilot.Core.Tests.Helpers;

public static class Fabrica
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    public static Sessao Sessao(StatusGravado status, TimeSpan idade, string id = "s1", Agente agente = Agente.Claude, string cwd = "/workspace/App")
        => new(agente, id, cwd, status, Agora - idade);

    public static string Json(string id = "s1", string agente = "claude", string status = "executando",
        string atualizado = "2026-11-14T09:00:00.1234567-03:00", string cwd = "/workspace/App", string extra = "")
        => $$"""{"agente":"{{agente}}","session_id":"{{id}}","cwd":"{{cwd}}","status":"{{status}}","evento":"Stop","atualizado":"{{atualizado}}"{{extra}}}""";
}
