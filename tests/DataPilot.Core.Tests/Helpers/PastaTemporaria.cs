namespace DataPilot.Core.Tests.Helpers;

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
