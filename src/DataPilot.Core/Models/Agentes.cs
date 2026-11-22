namespace DataPilot.Core.Models;

/// <summary>Conversões do agente: o texto do arquivo e do comando (minúsculo) e o nome na tela.</summary>
public static class Agentes
{
    public static string NoArquivo(Agente agente) => agente == Agente.Claude ? "claude" : "codex";

    public static string Nome(Agente agente) => agente == Agente.Claude ? "Claude" : "Codex";

    public static bool TentarLer(string? texto, out Agente agente)
    {
        switch (texto)
        {
            case "claude": agente = Agente.Claude; return true;
            case "codex": agente = Agente.Codex; return true;
            default: agente = default; return false;
        }
    }
}
