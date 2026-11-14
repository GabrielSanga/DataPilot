using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public sealed record DadosDemanda(string Id, string Titulo, string? OpenspecChange);

public sealed record NovaSessaoEscolha(Agente Agente, string? Rotulo);

public sealed record DestinoDemanda(string Id, string Texto);

/// <summary>
/// Perguntas, avisos e diálogos ao usuário, atrás de uma interface para a lógica do view model não depender de janelas.
/// Nos diálogos de demanda e de vínculo, <c>aplicar</c> grava e devolve a mensagem de recusa (ou null); o diálogo
/// só fecha quando a gravação dá certo.
/// </summary>
public interface IInteracao
{
    bool Confirmar(string mensagem);
    void Erro(string mensagem);
    void Informar(string mensagem);

    bool DialogoDemanda(Demanda? existente, Func<DadosDemanda, string?> aplicar);
    bool DialogoVincularRepositorio(string demandaTitulo, IReadOnlyList<string> sugestoes, Func<string, string?> aplicar);
    NovaSessaoEscolha? PedirNovaSessao(string demandaTitulo, string pasta);
    string? EscolherDemandaDestino(IReadOnlyList<DestinoDemanda> destinos);
}

public sealed class InteracaoJanelas : IInteracao
{
    private const string Titulo = "DataPilot";

    private static Window? Dono => Application.Current?.MainWindow;

    public bool Confirmar(string mensagem)
        => MessageBox.Show(mensagem, Titulo, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    public void Erro(string mensagem) => MessageBox.Show(mensagem, Titulo, MessageBoxButton.OK, MessageBoxImage.Error);

    public void Informar(string mensagem) => MessageBox.Show(mensagem, Titulo, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool DialogoDemanda(Demanda? existente, Func<DadosDemanda, string?> aplicar)
        => new DemandaWindow(existente, aplicar) { Owner = Dono }.ShowDialog() == true;

    public bool DialogoVincularRepositorio(string demandaTitulo, IReadOnlyList<string> sugestoes, Func<string, string?> aplicar)
        => new VincularRepositorioWindow(demandaTitulo, sugestoes, aplicar) { Owner = Dono }.ShowDialog() == true;

    public NovaSessaoEscolha? PedirNovaSessao(string demandaTitulo, string pasta)
    {
        var janela = new NovaSessaoWindow(demandaTitulo, pasta) { Owner = Dono };
        return janela.ShowDialog() == true ? new NovaSessaoEscolha(janela.AgenteEscolhido, janela.Rotulo) : null;
    }

    public string? EscolherDemandaDestino(IReadOnlyList<DestinoDemanda> destinos)
    {
        var janela = new MoverSessaoWindow(destinos) { Owner = Dono };
        return janela.ShowDialog() == true ? janela.DestinoEscolhido : null;
    }
}
