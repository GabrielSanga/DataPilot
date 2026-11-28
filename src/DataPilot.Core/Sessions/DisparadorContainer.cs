using System.ComponentModel;
using System.Diagnostics;
using DataPilot.Core.Display;
using DataPilot.Core.Models;

namespace DataPilot.Core.Sessions;

public sealed record ResultadoProcesso(int Codigo, string Saida, string Erro);

/// <summary>Executa um processo curto e devolve a saída. Existe para que os testes não dependam do Docker.</summary>
public interface IExecutorProcesso
{
    /// <exception cref="Win32Exception">O executável não existe ou não pode ser iniciado.</exception>
    /// <exception cref="TimeoutException">O processo não terminou dentro do limite.</exception>
    ResultadoProcesso Executar(string executavel, IReadOnlyList<string> argumentos, TimeSpan limite);
}

public sealed class ExecutorProcesso : IExecutorProcesso
{
    public ResultadoProcesso Executar(string executavel, IReadOnlyList<string> argumentos, TimeSpan limite)
    {
        var info = new ProcessStartInfo(executavel)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argumento in argumentos) info.ArgumentList.Add(argumento);
        using var processo = Process.Start(info) ?? throw new Win32Exception($"Não foi possível iniciar {executavel}.");
        var saida = processo.StandardOutput.ReadToEndAsync();
        var erro = processo.StandardError.ReadToEndAsync();
        if (!processo.WaitForExit(limite))
        {
            try { processo.Kill(entireProcessTree: true); }
            catch (Exception e) when (e is Win32Exception or InvalidOperationException) { } // já terminou
            throw new TimeoutException();
        }
        return new ResultadoProcesso(processo.ExitCode, saida.GetAwaiter().GetResult(), erro.GetAwaiter().GetResult());
    }
}

/// <summary>
/// Antes de abrir uma sessão, confere se o Docker responde e se o container existe, e o inicia se
/// estiver parado. Sem isso a aba abriria só para mostrar o erro, e o registro ficaria para trás.
/// </summary>
public sealed class DisparadorContainer(IDisparador interno, IExecutorProcesso executor) : IDisparador
{
    public static readonly TimeSpan LimiteVerificacao = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan LimiteInicio = TimeSpan.FromSeconds(20);

    private const string SemResposta = "O Docker não respondeu. Abra o Docker Desktop, espere ele iniciar e tente de novo.";
    private const string Alternativa = "Suba o ambiente com \"docker compose up -d\" na pasta docker.";

    public void Iniciar(ComandoTerminal comando)
    {
        Preparar(comando.Container);
        interno.Iniciar(comando);
    }

    private void Preparar(string container)
    {
        // "--type container": a imagem tem o mesmo nome do container padrão e não pode ser confundida com ele.
        var estado = Docker(LimiteVerificacao, "inspect", "--type", "container", "--format", "{{.State.Running}}", container);
        if (estado.Codigo != 0)
        {
            // "No such object" ou "No such container", conforme a versão do Docker.
            var inexistente = estado.Erro.Contains("no such object", StringComparison.OrdinalIgnoreCase)
                || estado.Erro.Contains("no such container", StringComparison.OrdinalIgnoreCase);
            throw new FalhaDisparoException(inexistente ? $"O container {container} não existe. {Alternativa}" : SemResposta);
        }
        if (estado.Saida.Trim() == "true") return;

        var inicio = Docker(LimiteInicio, "start", container);
        if (inicio.Codigo != 0)
            throw new FalhaDisparoException(
                $"Não foi possível iniciar o container {container} ({TextoExibicao.Limpar(inicio.Erro.Trim())}). {Alternativa}");
    }

    private ResultadoProcesso Docker(TimeSpan limite, params string[] argumentos)
    {
        try
        {
            return executor.Executar(Constantes.ExecutavelDocker, argumentos, limite);
        }
        catch (TimeoutException e)
        {
            throw new FalhaDisparoException(SemResposta, e);
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or IOException)
        {
            throw new FalhaDisparoException(
                $"Não foi possível executar o docker. O Docker Desktop está instalado? Instale-o e tente de novo. ({e.Message})", e);
        }
    }
}
