using System.ComponentModel;
using System.Diagnostics;

namespace DataPilot.Core.Sessions;

public sealed class FalhaDisparoException(string mensagem, Exception? interna = null) : Exception(mensagem, interna);

/// <summary>Inicia o comando. Existe para que os testes usem um disparador falso e nenhum abra janela.</summary>
public interface IDisparador
{
    /// <exception cref="FalhaDisparoException">Quando o processo não pode ser iniciado.</exception>
    void Iniciar(ComandoTerminal comando);
}

/// <summary>Disparo real: <see cref="ProcessStartInfo.ArgumentList"/> e <c>UseShellExecute = false</c>, sem shell.</summary>
public sealed class DisparadorProcesso : IDisparador
{
    public void Iniciar(ComandoTerminal comando)
    {
        var info = new ProcessStartInfo(comando.Executavel) { UseShellExecute = false };
        foreach (var argumento in comando.Argumentos) info.ArgumentList.Add(argumento);
        try
        {
            using var processo = Process.Start(info)
                ?? throw new FalhaDisparoException($"Não foi possível iniciar {comando.Executavel}.");
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            throw new FalhaDisparoException(
                $"Não foi possível iniciar {comando.Executavel}. O Windows Terminal está instalado? ({e.Message})", e);
        }
    }
}
