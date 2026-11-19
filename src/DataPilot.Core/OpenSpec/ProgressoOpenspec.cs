using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;
using Microsoft.Win32.SafeHandles;

namespace DataPilot.Core.OpenSpec;

/// <summary>Tarefas concluídas e total de um <c>tasks.md</c>.</summary>
public sealed record ContagemTarefas(int Concluidas, int Total);

/// <summary>Conta as tarefas de um texto de <c>tasks.md</c>. O resultado são só dois números.</summary>
public static partial class ContadorTarefas
{
    // Mesma regra do PRD e do CLI do OpenSpec: caixa no início da linha, marcada com x ou X.
    [GeneratedRegex(@"\A\s*-\s\[( |x|X)\]")]
    private static partial Regex PadraoTarefa();

    public static ContagemTarefas Contar(string texto)
    {
        int concluidas = 0, total = 0;
        foreach (var linha in texto.Split('\n'))
        {
            var m = PadraoTarefa().Match(linha);
            if (!m.Success) continue;
            total++;
            if (m.Groups[1].Value != " ") concluidas++;
        }
        return new ContagemTarefas(concluidas, total);
    }
}

public enum EstadoLocalizacao
{
    /// <summary>A change não existe no repositório (nem ativa, nem arquivada).</summary>
    Nenhum,
    Encontrado,
    /// <summary>Entrada inválida ou trecho do caminho que é atalho ou junção.</summary>
    Recusado,
}

/// <summary>Onde está o <c>tasks.md</c> de uma change num repositório. <see cref="Trechos"/> é o caminho a conferir.</summary>
public sealed record LocalTasks(EstadoLocalizacao Estado, string? Caminho, bool Arquivada, IReadOnlyList<string> Trechos)
{
    public static readonly LocalTasks SemChange = new(EstadoLocalizacao.Nenhum, null, false, []);
    public static readonly LocalTasks Negado = new(EstadoLocalizacao.Recusado, null, false, []);
}

/// <summary>
/// Localiza o <c>tasks.md</c> da change: a ativa (<c>openspec\changes\&lt;change&gt;</c>) tem prioridade; senão, a
/// arquivada de data mais recente (<c>openspec\changes\archive\AAAA-MM-DD-&lt;change&gt;</c>). O caminho é montado só
/// com a raiz recebida e com <c>pasta</c> e <c>change</c> revalidados.
/// </summary>
public static class LocalizadorTasks
{
    public const string NomeArquivo = "tasks.md";

    public static LocalTasks Localizar(string raiz, string pasta, string change)
    {
        if (!ValidadorEntrada.PastaValida(pasta) || !ValidadorEntrada.OpenspecChangeValido(change)) return LocalTasks.Negado;

        var repo = Path.Combine(raiz, pasta);
        var inicio = Tipo(repo);
        if (inicio == TipoEntrada.Inexistente) return LocalTasks.SemChange;
        if (inicio == TipoEntrada.Atalho) return LocalTasks.Negado;

        var changes = Path.Combine(repo, "openspec", "changes");
        var ativa = Path.Combine(changes, change);
        switch (Tipo(ativa))
        {
            case TipoEntrada.Normal:
            case TipoEntrada.Atalho:
                return Encontrado(raiz, [pasta, "openspec", "changes", change, NomeArquivo], false);
        }

        var tipoOpenspec = TiposAteChanges(repo);
        if (tipoOpenspec == TipoEntrada.Atalho) return LocalTasks.Negado;

        var arquivo = Path.Combine(changes, "archive");
        var tipoArquivo = Tipo(arquivo);
        if (tipoArquivo == TipoEntrada.Inexistente) return LocalTasks.SemChange;
        if (tipoArquivo == TipoEntrada.Atalho) return LocalTasks.Negado;

        string? melhor = null;
        DateTime melhorData = default;
        try
        {
            foreach (var entrada in new DirectoryInfo(arquivo).EnumerateDirectories())
            {
                if ((entrada.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                if (!TentarData(entrada.Name, change, out var data)) continue;
                if (melhor is null || data > melhorData || (data == melhorData && string.CompareOrdinal(entrada.Name, melhor) > 0))
                {
                    melhor = entrada.Name;
                    melhorData = data;
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return LocalTasks.Negado;
        }
        return melhor is null
            ? LocalTasks.SemChange
            : Encontrado(raiz, [pasta, "openspec", "changes", "archive", melhor, NomeArquivo], true);
    }

    private static LocalTasks Encontrado(string raiz, string[] trechos, bool arquivada)
        => new(EstadoLocalizacao.Encontrado, Path.Combine([raiz, .. trechos]), arquivada, trechos);

    /// <summary>Verifica <c>openspec</c> e <c>changes</c>: um atalho em qualquer um recusa.</summary>
    private static TipoEntrada TiposAteChanges(string repo)
    {
        foreach (var parte in new[] { "openspec", Path.Combine("openspec", "changes") })
            if (Tipo(Path.Combine(repo, parte)) == TipoEntrada.Atalho) return TipoEntrada.Atalho;
        return TipoEntrada.Normal;
    }

    private static bool TentarData(string nome, string change, out DateTime data)
    {
        data = default;
        var sufixo = "-" + change;
        // AAAA-MM-DD + "-" + change, comparando como texto.
        if (nome.Length != 10 + sufixo.Length || !nome.EndsWith(sufixo, StringComparison.Ordinal)) return false;
        var parte = nome[..10];
        foreach (var i in new[] { 0, 1, 2, 3, 5, 6, 8, 9 })
            if (parte[i] is < '0' or > '9') return false;
        return DateTime.TryParseExact(parte, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out data);
    }

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
}

public enum EstadoRepositorio
{
    NaoEncontrada,
    Lido,
    SemTarefas,
    Ilegivel,
}

/// <summary>Progresso da change de um repositório de um trabalho.</summary>
public sealed record ProgressoRepositorio(string Pasta, EstadoRepositorio Estado, bool Arquivada, int Concluidas, int Total)
{
    public static ProgressoRepositorio NaoEncontrada(string pasta) => new(pasta, EstadoRepositorio.NaoEncontrada, false, 0, 0);
    public static ProgressoRepositorio Ilegivel(string pasta, bool arquivada = false) => new(pasta, EstadoRepositorio.Ilegivel, arquivada, 0, 0);

    /// <summary>Valor de 0 a 1 para a barra.</summary>
    public double Fracao => Total == 0 ? 0 : (double)Concluidas / Total;

    /// <summary>Texto do card, abaixo do nome da change.</summary>
    public string Texto => Estado switch
    {
        EstadoRepositorio.Lido => $"{Concluidas} de {Total}" + (Arquivada ? " · arquivada" : ""),
        EstadoRepositorio.SemTarefas => "sem tarefas" + (Arquivada ? " · arquivada" : ""),
        EstadoRepositorio.Ilegivel => "progresso indisponível",
        _ => "change não encontrada",
    };
}

/// <summary>
/// Lê o <c>tasks.md</c> de uma pasta controlada pelo usuário <c>ia</c> só para contar: sem atalhos nem junções em
/// nenhum trecho do caminho, conferindo o arquivo aberto, até 256 KB, sem bloquear quem escreve, UTF-8 estrito e
/// nunca gravando. A raiz é do painel e não é verificada.
/// </summary>
public sealed class LeitorTasks
{
    public const int TamanhoMaximo = 256 * 1024;
    private const int LimiteCache = 256;
    private static readonly UTF8Encoding Utf8Estrito = new(false, true);

    private readonly string _raiz;
    private readonly Dictionary<string, (DateTime Modificado, long Tamanho, ContagemTarefas? Contagem)> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public LeitorTasks(string raiz) => _raiz = raiz;

    public string Raiz => _raiz;

    public ProgressoRepositorio Ler(string pasta, string change)
    {
        try
        {
            var local = LocalizadorTasks.Localizar(_raiz, pasta, change);
            switch (local.Estado)
            {
                case EstadoLocalizacao.Nenhum: return ProgressoRepositorio.NaoEncontrada(pasta);
                case EstadoLocalizacao.Recusado: return ProgressoRepositorio.Ilegivel(pasta);
            }
            return LerLocal(pasta, local);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return ProgressoRepositorio.Ilegivel(pasta);
        }
    }

    private ProgressoRepositorio LerLocal(string pasta, LocalTasks local)
    {
        var caminho = local.Caminho!;
        var arquivada = local.Arquivada;

        // Cada trecho a partir de <raiz>\<pasta>, inclusive o próprio arquivo.
        var parcial = _raiz;
        for (var i = 0; i < local.Trechos.Count; i++)
        {
            parcial = Path.Combine(parcial, local.Trechos[i]);
            var tipo = LocalizadorTasks.Tipo(parcial);
            if (tipo == LocalizadorTasks.TipoEntrada.Atalho) return ProgressoRepositorio.Ilegivel(pasta, arquivada);
            if (tipo == LocalizadorTasks.TipoEntrada.Inexistente)
            {
                // Change ativa sem tasks.md ainda: não há tarefas. Qualquer outro trecho sumido é ilegível.
                return i == local.Trechos.Count - 1
                    ? new ProgressoRepositorio(pasta, EstadoRepositorio.SemTarefas, arquivada, 0, 0)
                    : ProgressoRepositorio.Ilegivel(pasta, arquivada);
            }
        }

        var info = new FileInfo(caminho);
        var modificado = info.LastWriteTimeUtc;
        var tamanho = info.Length;
        if (tamanho > TamanhoMaximo) return ProgressoRepositorio.Ilegivel(pasta, arquivada);

        ContagemTarefas? contagem;
        if (_cache.TryGetValue(caminho, out var c) && c.Modificado == modificado && c.Tamanho == tamanho)
        {
            contagem = c.Contagem;
        }
        else
        {
            contagem = LerEContar(caminho, local.Trechos);
            if (_cache.Count >= LimiteCache) _cache.Clear();
            _cache[caminho] = (modificado, tamanho, contagem);
        }
        return Montar(pasta, arquivada, contagem);
    }

    private static ProgressoRepositorio Montar(string pasta, bool arquivada, ContagemTarefas? contagem)
    {
        if (contagem is null) return ProgressoRepositorio.Ilegivel(pasta, arquivada);
        return contagem.Total == 0
            ? new ProgressoRepositorio(pasta, EstadoRepositorio.SemTarefas, arquivada, 0, 0)
            : new ProgressoRepositorio(pasta, EstadoRepositorio.Lido, arquivada, contagem.Concluidas, contagem.Total);
    }

    /// <summary>Abre, confere que o arquivo aberto é o esperado e conta. Null quando é ilegível.</summary>
    private ContagemTarefas? LerEContar(string caminho, IReadOnlyList<string> trechos)
    {
        var buffer = new byte[TamanhoMaximo + 1];
        int total = 0;
        try
        {
            using var fs = new FileStream(caminho, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite | FileShare.Delete,
            });
            if (!ArquivoAbertoEhEsperado(fs.SafeFileHandle, trechos)) return null;
            int n;
            while (total < buffer.Length && (n = fs.Read(buffer, total, buffer.Length - total)) > 0)
                total += n;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        if (total > TamanhoMaximo) return null;

        var inicio = total >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF ? 3 : 0;
        try { return ContadorTarefas.Contar(Utf8Estrito.GetString(buffer, inicio, total - inicio)); }
        catch (DecoderFallbackException) { return null; }
    }

    private bool ArquivoAbertoEhEsperado(SafeFileHandle arquivo, IReadOnlyList<string> trechos)
    {
        var final = CaminhoFinal(arquivo);
        var raiz = CaminhoFinalDaRaiz();
        if (final is null || raiz is null) return false;
        var esperado = Path.Combine([raiz, .. trechos]);
        return string.Equals(final, esperado, StringComparison.OrdinalIgnoreCase);
    }

    private string? CaminhoFinalDaRaiz()
    {
        // FILE_FLAG_BACKUP_SEMANTICS permite abrir pasta; só atributos, sem acesso a dados.
        using var h = CreateFileW(_raiz, 0, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
        return h.IsInvalid ? null : CaminhoFinal(h);
    }

    private static string? CaminhoFinal(SafeFileHandle h)
    {
        var sb = new StringBuilder(1024);
        var n = GetFinalPathNameByHandleW(h, sb, (uint)sb.Capacity, 0);
        if (n == 0 || n >= sb.Capacity) return null;
        var texto = sb.ToString();
        if (texto.StartsWith(@"\\?\UNC\", StringComparison.Ordinal)) return @"\\" + texto[8..];
        if (texto.StartsWith(@"\\?\", StringComparison.Ordinal)) return texto[4..];
        return texto;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file, StringBuilder caminho, uint tamanho, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string nome, uint acesso, uint compartilhamento, IntPtr seguranca,
        uint disposicao, uint flags, IntPtr modelo);
}

/// <summary>Lê o progresso de cada repositório com change (a própria ou a herdada); sem change, nada é lido.</summary>
public static class ProgressoDosRepositorios
{
    public static IReadOnlyDictionary<string, ProgressoRepositorio> Montar(
        IEnumerable<RepositorioCadastrado> repositorios, LeitorTasks leitor)
    {
        var resultado = new Dictionary<string, ProgressoRepositorio>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in repositorios)
            if (r.OpenspecChange is { } change) resultado[r.Pasta] = leitor.Ler(r.Pasta, change);
        return resultado;
    }
}
