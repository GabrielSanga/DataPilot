using System.Windows;
using DataPilot.Wpf.Models;
using DataPilot.Wpf.Views;

namespace DataPilot.Wpf.Services;

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
