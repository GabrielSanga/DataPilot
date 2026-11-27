namespace DataPilot.Core.Models;

/// <summary>Onde o agente de uma sessão roda.</summary>
public enum Execucao { Container, Windows }

/// <summary>Conversões do ambiente de execução para o texto do arquivo de dados.</summary>
public static class Execucoes
{
    public static string NoArquivo(Execucao execucao) => execucao == Execucao.Container ? "container" : "windows";

    public static bool TentarLer(string? texto, out Execucao execucao)
    {
        switch (texto)
        {
            case "container": execucao = Execucao.Container; return true;
            case "windows": execucao = Execucao.Windows; return true;
            default: execucao = default; return false;
        }
    }
}

/// <summary>
/// Onde o painel abre os agentes: no container Docker ou no Windows, este pelo SSH local (com o usuário escolhido)
/// ou direto.
/// </summary>
public sealed record Configuracoes(Execucao Execucao, string Container, bool UsarSsh, string UsuarioSsh)
{
    public static Configuracoes Padrao { get; } = new(Execucao.Container, Constantes.ContainerPadrao, true, Constantes.UsuarioSshPadrao);
}
