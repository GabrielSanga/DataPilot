using System.Globalization;
using System.Text.RegularExpressions;

namespace DataPilot.Core;

/// <summary>
/// Regras de validação das entradas que podem chegar a um comando: pasta, identificador de sessão e rótulo.
/// É o único lugar onde elas estão escritas; cadastro, carga do arquivo de dados e montador usam este tipo.
/// </summary>
public static partial class ValidadorEntrada
{
    public const int TamanhoMaximoRotulo = 40;
    public const int TamanhoMaximoIdDemanda = 30;
    public const int TamanhoMaximoTitulo = 80;
    public const int TamanhoMaximoOpenspecChange = 100;

    // As duas primeiras repetem as regras do abrir.ps1; \z evita aceitar uma quebra de linha final.
    [GeneratedRegex(@"\A[A-Za-z0-9_.-]+\z")]
    private static partial Regex PadraoPasta();

    [GeneratedRegex(@"\A[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\z")]
    private static partial Regex PadraoGuid();

    /// <summary>Só <c>[A-Za-z0-9_.-]</c> e nunca formada apenas por pontos.</summary>
    public static bool PastaValida(string? pasta)
        => pasta is not null && PadraoPasta().IsMatch(pasta) && pasta.Any(c => c != '.');

    /// <summary>Regra da pasta, com até 30 caracteres. O <c>id</c> vai para o título da aba, sem precisar de escape.</summary>
    public static bool IdDemandaValido(string? id)
        => PastaValida(id) && id!.Length <= TamanhoMaximoIdDemanda;

    /// <summary>Regra do <c>id</c>; será usado para montar um caminho (Etapa 5), por isso é validado desde já.</summary>
    public static bool OpenspecChangeValido(string? nome)
        => PastaValida(nome) && nome!.Length <= TamanhoMaximoOpenspecChange;

    /// <summary>De 1 a 80 caracteres, sem caracteres de controle nem separadores de linha. Só é exibido.</summary>
    public static bool TituloValido(string? titulo)
        => TextoSimplesValido(titulo, TamanhoMaximoTitulo, permitirPontoEVirgula: true);

    [GeneratedRegex(@"\A[A-Za-z0-9_-]{1,100}\z")]
    private static partial Regex PadraoIdentificadorHook();

    /// <summary>
    /// Regra do identificador que o hook aceita (1 a 100 caracteres de <c>[A-Za-z0-9_-]</c>). Vale para as sessões
    /// excluídas: o valor só é comparado, nunca entra em comando nem em caminho.
    /// </summary>
    public static bool IdentificadorExclusaoValido(string? id)
        => id is not null && PadraoIdentificadorHook().IsMatch(id);

    [GeneratedRegex(@"\At[0-9a-f]{8}\z", RegexOptions.IgnoreCase)]
    private static partial Regex PadraoIdTrabalho();

    /// <summary>Identificador gerado pelo painel: <c>t</c> e 8 dígitos hexadecimais.</summary>
    public static bool IdTrabalhoGeradoValido(string? id) => id is not null && PadraoIdTrabalho().IsMatch(id);

    /// <summary>GUID na forma canônica 8-4-4-4-12.</summary>
    public static bool IdentificadorValido(string? id)
        => id is not null && PadraoGuid().IsMatch(id);

    /// <summary>
    /// De 1 a 40 caracteres, sem <c>;</c> (o Windows Terminal o trata como separador de comandos)
    /// e sem caracteres de controle ou separadores de linha.
    /// </summary>
    public static bool RotuloValido(string? rotulo)
        => TextoSimplesValido(rotulo, TamanhoMaximoRotulo, permitirPontoEVirgula: false);

    private static bool TextoSimplesValido(string? texto, int maximo, bool permitirPontoEVirgula)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length > maximo) return false;
        foreach (var c in texto)
        {
            if ((c == ';' && !permitirPontoEVirgula) || char.IsControl(c)) return false;
            var cat = char.GetUnicodeCategory(c);
            if (cat is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator) return false;
        }
        return true;
    }

    /// <summary>Texto do agente como usado no arquivo de dados e no comando.</summary>
    public static string NomeAgente(Agente agente) => agente == Agente.Claude ? "claude" : "codex";

    public static bool TentarAgente(string? texto, out Agente agente)
    {
        switch (texto)
        {
            case "claude": agente = Agente.Claude; return true;
            case "codex": agente = Agente.Codex; return true;
            default: agente = default; return false;
        }
    }
}
