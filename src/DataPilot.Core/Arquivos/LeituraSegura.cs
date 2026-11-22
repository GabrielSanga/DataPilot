using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DataPilot.Core.Arquivos;

/// <summary>
/// Único lugar que conhece as regras de leitura de pastas que pertencem ao usuário <c>ia</c>: nunca seguir atalho ou
/// junção, ler com limite de bytes, sem bloquear quem escreve, e só aceitar UTF-8 estrito.
/// </summary>
public static class LeituraSegura
{
    private static readonly UTF8Encoding Utf8Estrito = new(false, true);

    internal enum TipoEntrada { Inexistente, Normal, Atalho }

    internal static TipoEntrada Tipo(string caminho)
    {
        try
        {
            var atributos = File.GetAttributes(caminho);
            return (atributos & FileAttributes.ReparsePoint) != 0 ? TipoEntrada.Atalho : TipoEntrada.Normal;
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return TipoEntrada.Inexistente;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return TipoEntrada.Atalho; // inacessível: tratado como recusado
        }
    }

    /// <summary>
    /// Lê o arquivo sem bloquear escrita nem exclusão, sem o BOM e em UTF-8 estrito. Null quando passa de
    /// <paramref name="limite"/> bytes, não é UTF-8 válido ou <paramref name="conferirAberto"/> recusa o arquivo aberto.
    /// Falhas de E/S sobem para quem chama.
    /// </summary>
    public static string? LerTexto(string caminho, int limite, Func<SafeFileHandle, bool>? conferirAberto = null)
    {
        var buffer = new byte[limite + 1];
        int total = 0;
        using (var fs = new FileStream(caminho, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.ReadWrite | FileShare.Delete,
        }))
        {
            if (conferirAberto is not null && !conferirAberto(fs.SafeFileHandle)) return null;
            int n;
            while (total < buffer.Length && (n = fs.Read(buffer, total, buffer.Length - total)) > 0)
                total += n;
        }
        if (total > limite) return null;

        var inicio = total >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF ? 3 : 0;
        try { return Utf8Estrito.GetString(buffer, inicio, total - inicio); }
        catch (DecoderFallbackException) { return null; }
    }

    /// <summary>Nomes das subpastas diretas que não são atalho nem junção. Falhas de E/S sobem para quem chama.</summary>
    public static IEnumerable<string> Subpastas(string pasta)
        => new DirectoryInfo(pasta).EnumerateDirectories()
            .Where(d => (d.Attributes & FileAttributes.ReparsePoint) == 0)
            .Select(d => d.Name);
}
