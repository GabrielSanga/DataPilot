namespace DataPilot.Core.Models;

/// <summary>Configurações do painel: o nome do container Docker em que os agentes rodam.</summary>
public sealed record Configuracoes(string Container)
{
    public static Configuracoes Padrao { get; } = new(Constantes.ContainerPadrao);
}
