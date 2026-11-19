using System.IO;
using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public sealed record NovaSessaoEscolha(Agente Agente, string? Rotulo);

/// <summary>Trabalho oferecido no diálogo de vínculo, com os repositórios dele e o que coincide com a pasta da sessão (se houver).</summary>
public sealed record OpcaoDemandaVinculo(string Id, string Texto, IReadOnlyList<string> Repositorios, string? RepositorioSugerido);

public sealed record EscolhaVinculo(string DemandaId, string Repositorio, string? Rotulo);

/// <summary>
/// Perguntas, avisos e diálogos ao usuário, atrás de uma interface para a lógica do view model não depender de janelas.
/// Nos diálogos de trabalho, repositório e vínculo, <c>aplicar</c> grava e devolve a mensagem de recusa (ou null); o
/// diálogo só fecha quando a gravação dá certo.
/// </summary>
public interface IInteracao
{
    /// <summary>
    /// Confirmação do próprio painel: <c>titulo</c> diz o que vai acontecer, <c>mensagem</c> explica e o botão de
    /// confirmação leva o nome da ação. "Cancelar" é o botão padrão, então Enter não confirma.
    /// </summary>
    bool Confirmar(string titulo, string mensagem, string acao);

    /// <summary>Criar (<c>tituloAtual</c> nulo) ou editar um trabalho; só o título.</summary>
    bool DialogoTrabalho(string? tituloAtual, Func<string, string?> aplicar);

    /// <summary>
    /// "Vincular repositório" (<c>pastaFixa</c> nulo: abre a escolha de pasta do Windows em <c>raiz</c>) ou "Trocar change"
    /// (pasta já conhecida). <c>aplicar</c> recebe o nome da pasta e a change escolhida (ou nenhuma).
    /// </summary>
    bool DialogoRepositorio(string raiz, string? pastaFixa, string? changeAtual, Func<string, string?, string?> aplicar);

    /// <summary>Diálogo de vínculo. <c>aplicar</c> devolve a mensagem de recusa; "" mantém o diálogo aberto sem mensagem (o usuário não confirmou o aviso).</summary>
    bool DialogoVincularSessao(string descricaoSessao, IReadOnlyList<OpcaoDemandaVinculo> demandas, Func<EscolhaVinculo, string?> aplicar);

    NovaSessaoEscolha? PedirNovaSessao();

    /// <summary>Escolha de pasta do Windows, começando em <c>raiz</c>. Nulo quando o usuário cancela.</summary>
    string? EscolherPasta(string raiz);

    /// <summary>Escolha do repositório entre as pastas diretas da raiz, pré-selecionando <c>sugerida</c>. Nulo quando cancela.</summary>
    string? EscolherRepositorio(IReadOnlyList<string> pastas, string? sugerida);
}

public sealed class InteracaoJanelas : IInteracao
{
    /// <summary>A janela ativa (um diálogo aberto, se houver) ou a janela principal.</summary>
    private static Window? Dono
        => Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

    public bool Confirmar(string titulo, string mensagem, string acao)
        => new ConfirmacaoWindow(titulo, mensagem, acao) { Owner = Dono }.ShowDialog() == true;

    public bool DialogoTrabalho(string? tituloAtual, Func<string, string?> aplicar)
        => new DemandaWindow(tituloAtual, aplicar) { Owner = Dono }.ShowDialog() == true;

    public bool DialogoRepositorio(string raiz, string? pastaFixa, string? changeAtual, Func<string, string?, string?> aplicar)
        => new VincularRepositorioWindow(raiz, pastaFixa, changeAtual, aplicar) { Owner = Dono }.ShowDialog() == true;

    public bool DialogoVincularSessao(string descricaoSessao, IReadOnlyList<OpcaoDemandaVinculo> demandas, Func<EscolhaVinculo, string?> aplicar)
        => new VincularSessaoWindow(descricaoSessao, demandas, aplicar) { Owner = Dono }.ShowDialog() == true;

    public NovaSessaoEscolha? PedirNovaSessao()
    {
        var janela = new NovaSessaoWindow { Owner = Dono };
        return janela.ShowDialog() == true ? new NovaSessaoEscolha(janela.AgenteEscolhido, janela.Rotulo) : null;
    }

    public string? EscolherPasta(string raiz) => EscolhaPastaWindows.Escolher(raiz, Dono);

    public string? EscolherRepositorio(IReadOnlyList<string> pastas, string? sugerida)
    {
        var janela = new EscolherRepositorioWindow(pastas, sugerida) { Owner = Dono };
        return janela.ShowDialog() == true ? janela.Escolhido : null;
    }
}

/// <summary>A escolha de pasta do próprio Windows (diálogo do .NET), sempre começando na raiz dos fontes.</summary>
public static class EscolhaPastaWindows
{
    public static string? Escolher(string raiz, Window? dono)
    {
        var dialogo = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Escolha a pasta do repositório",
            Multiselect = false,
        };
        if (Directory.Exists(raiz)) dialogo.InitialDirectory = raiz;
        return (dono is null ? dialogo.ShowDialog() : dialogo.ShowDialog(dono)) == true ? dialogo.FolderName : null;
    }
}

/// <summary>Conversor para habilitar um controle quando todas as condições são verdadeiras.</summary>
public sealed class TodosVerdadeiros : System.Windows.Data.IMultiValueConverter
{
    public static readonly TodosVerdadeiros Instancia = new();

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => values.All(v => v is true);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
