using DataPilot.Core.Sessions;

namespace DataPilot.Core.Tests.Helpers;

public sealed class DisparadorFalso : IDisparador
{
    public List<ComandoTerminal> Comandos { get; } = [];
    public bool Falhar { get; set; }

    public void Iniciar(ComandoTerminal comando)
    {
        if (Falhar) throw new FalhaDisparoException("wt.exe ausente");
        Comandos.Add(comando);
    }
}
