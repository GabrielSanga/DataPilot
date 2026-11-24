using DataPilot.Core.Arquivos;
using System.Runtime.InteropServices;
using System.Text;
using DataPilot.Core.Models;
using Microsoft.Win32.SafeHandles;

namespace DataPilot.Core.OpenSpec;

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

    private readonly string _raiz;
    private readonly Dictionary<string, (DateTime Modificado, long Tamanho, ContagemTarefas? Contagem)> _cache = new(StringComparer.OrdinalIgnoreCase);

    public LeitorTasks(string raiz) => _raiz = raiz;

    public string Raiz => _raiz;

    /// <summary>Lê o progresso de cada repositório com change (a própria ou a herdada); sem change, nada é lido.</summary>
    public IReadOnlyDictionary<string, ProgressoRepositorio> LerRepositorios(IEnumerable<RepositorioCadastrado> repositorios)
    {
        var resultado = new Dictionary<string, ProgressoRepositorio>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in repositorios)
            if (r.OpenspecChange is { } change) resultado[r.Pasta] = Ler(r.Pasta, change);
        return resultado;
    }

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
            var tipo = LeituraSegura.Tipo(parcial);
            if (tipo == LeituraSegura.TipoEntrada.Atalho) return ProgressoRepositorio.Ilegivel(pasta, arquivada);
            if (tipo == LeituraSegura.TipoEntrada.Inexistente)
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

    /// <summary>Lê, conferindo o arquivo aberto, e conta. Null quando é ilegível.</summary>
    private ContagemTarefas? LerEContar(string caminho, IReadOnlyList<string> trechos)
    {
        try
        {
            var texto = LeituraSegura.LerTexto(caminho, TamanhoMaximo, h => ArquivoAbertoEhEsperado(h, trechos));
            return texto is null ? null : ContadorTarefas.Contar(texto);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
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
