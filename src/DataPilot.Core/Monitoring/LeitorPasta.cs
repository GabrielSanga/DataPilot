using System.Text;
using DataPilot.Core.Models;

namespace DataPilot.Core.Monitoring;

/// <summary>Retrato completo da pasta de status em um instante.</summary>
public sealed record RetratoPasta(bool Disponivel, IReadOnlyList<Sessao> Sessoes)
{
    public static readonly RetratoPasta Indisponivel = new(false, []);
}

/// <summary>
/// Lê a pasta de status somente para leitura: só .json, no máximo 500 arquivos mais recentes,
/// até 16 KB por arquivo, abrindo sem bloquear escrita ou exclusão e ignorando atalhos.
/// </summary>
public sealed class LeitorPasta
{
    private static readonly UTF8Encoding Utf8Estrito = new(false, true);

    private readonly string _pasta;
    private readonly Dictionary<string, (DateTime Modificado, DateTime Criado, long Tamanho, Sessao? Sessao)> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public LeitorPasta(string pasta) => _pasta = pasta;

    public string Pasta => _pasta;

    public RetratoPasta Ler()
    {
        List<FileInfo> arquivos;
        try
        {
            var dir = new DirectoryInfo(_pasta);
            if (!dir.Exists) return Indisponivel();
            arquivos = MaisRecentes(dir);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Indisponivel();
        }

        var sessoes = new List<Sessao>();
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var arq in arquivos)
        {
            vistos.Add(arq.Name);
            var sessao = LerArquivo(arq);
            if (sessao is not null) sessoes.Add(sessao);
        }
        foreach (var chave in _cache.Keys.Where(k => !vistos.Contains(k)).ToList())
            _cache.Remove(chave);

        return new RetratoPasta(true, sessoes);
    }

    private RetratoPasta Indisponivel()
    {
        _cache.Clear();
        return RetratoPasta.Indisponivel;
    }

    private static List<FileInfo> MaisRecentes(DirectoryInfo dir)
    {
        var candidatos = new List<(FileInfo Arquivo, DateTime Modificado)>();
        foreach (var f in dir.EnumerateFiles("*.json"))
        {
            // O filtro do sistema pode casar extensões mais longas (ex.: .jsonx).
            if (!f.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                if ((f.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                candidatos.Add((f, f.LastWriteTimeUtc));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Removido ou inacessível durante a enumeração.
            }
        }
        return candidatos
            .OrderByDescending(c => c.Modificado)
            .Take(Constantes.MaximoArquivos)
            .Select(c => c.Arquivo)
            .ToList();
    }

    private Sessao? LerArquivo(FileInfo arq)
    {
        try
        {
            arq.Refresh();
            if (!arq.Exists || (arq.Attributes & FileAttributes.ReparsePoint) != 0) return null;
            var modificado = arq.LastWriteTimeUtc;
            var criado = arq.CreationTimeUtc;
            var tamanho = arq.Length;
            if (_cache.TryGetValue(arq.Name, out var c) && c.Modificado == modificado && c.Criado == criado && c.Tamanho == tamanho)
                return c.Sessao;

            var sessao = LerEValidar(arq);
            // A data de criação vem do sistema de arquivos; o conteúdo do arquivo não a define.
            if (sessao is not null) sessao = sessao with { ArquivoCriadoEm = new DateTimeOffset(criado, TimeSpan.Zero) };
            _cache[arq.Name] = (modificado, criado, tamanho, sessao);
            return sessao;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _cache.Remove(arq.Name);
            return null;
        }
    }

    private static Sessao? LerEValidar(FileInfo arq)
    {
        var limite = Constantes.TamanhoMaximoArquivo;
        var buffer = new byte[limite + 1];
        int total = 0;
        using (var fs = new FileStream(arq.FullName, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.ReadWrite | FileShare.Delete,
        }))
        {
            int n;
            while (total < buffer.Length && (n = fs.Read(buffer, total, buffer.Length - total)) > 0)
                total += n;
        }
        if (total > limite) return null;

        var inicio = total >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF ? 3 : 0;
        string texto;
        try { texto = Utf8Estrito.GetString(buffer, inicio, total - inicio); }
        catch (DecoderFallbackException) { return null; }

        var nome = Path.GetFileNameWithoutExtension(arq.Name);
        return ValidadorStatus.TentarLer(texto, nome, out var sessao) ? sessao : null;
    }
}
