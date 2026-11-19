using DataPilot.Core;

namespace DataPilot.Core.Tests;

public sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;
    public override DateTimeOffset GetUtcNow() => Agora;
}

public static class Fabrica
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    public static Sessao Sessao(StatusGravado status, TimeSpan idade, string id = "s1", Agente agente = Agente.Claude, string cwd = @"C:\Fontes\App")
        => new(agente, id, cwd, status, Agora - idade);

    public static string Json(string id = "s1", string agente = "claude", string status = "executando",
        string atualizado = "2026-11-14T09:00:00.1234567-03:00", string cwd = @"C:\\Fontes\\App", string extra = "")
        => $$"""{"agente":"{{agente}}","session_id":"{{id}}","cwd":"{{cwd}}","status":"{{status}}","evento":"Stop","atualizado":"{{atualizado}}"{{extra}}}""";
}

public sealed class PastaTemporaria : IDisposable
{
    public string Caminho { get; } = Path.Combine(Path.GetTempPath(), "datapilot-" + Guid.NewGuid().ToString("N"));

    public PastaTemporaria(bool criar = true)
    {
        if (criar) Directory.CreateDirectory(Caminho);
    }

    public string Escrever(string nome, string conteudo)
    {
        var p = Path.Combine(Caminho, nome);
        File.WriteAllText(p, conteudo, new System.Text.UTF8Encoding(false));
        return p;
    }

    public void Dispose()
    {
        try { Directory.Delete(Caminho, true); } catch { }
    }
}

public static class Espera
{
    public static bool Ate(Func<bool> condicao, TimeSpan limite)
    {
        var fim = DateTime.UtcNow + limite;
        while (DateTime.UtcNow < fim)
        {
            if (condicao()) return true;
            Thread.Sleep(25);
        }
        return condicao();
    }
}

public static class DadosPainelDeTeste
{
    /// <summary>Cria um trabalho com identificador escolhido, para montar cenários (o painel gera o identificador).</summary>
    public static ResultadoOperacao CriarDemanda(this DadosPainel dados, string id, string titulo, string? change, DateTimeOffset criadaEm)
        => dados.CriarDemandaComId(id, titulo, criadaEm);
}
