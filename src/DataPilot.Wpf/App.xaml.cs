using System.Windows;
using DataPilot.Core.Models;
using DataPilot.Core.OpenSpec;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Wpf.Services;
using DataPilot.Wpf.ViewModels;
using DataPilot.Wpf.Views;

namespace DataPilot.Wpf;

public partial class App : Application
{
    #region "Variáveis"

    private MainViewModel? _ViewModel;

    #endregion

    #region "Métodos (Override)"

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Capturando parâmetros recebidos
        var caminhoSessoes = LerParametro(e.Args, "--pasta-status");
        var caminhoTrabalhos = LerParametro(e.Args, "--dados");
        var caminhoFontes = LerParametro(e.Args, "--raiz-fontes");

        // Montando os objetos
        var pasta = caminhoSessoes ?? Constantes.PastaStatusPadrao;
        var dados = new DadosPainel(caminhoTrabalhos ?? DadosPainel.CaminhoPadrao);
        var abertura = new AberturaSessao(dados, new DisparadorProcesso(), TimeProvider.System);
        var leitorTasks = new LeitorTasks(caminhoFontes ?? Constantes.RaizFontesPadrao);

        // Cria a ViewModel principal
        _ViewModel = new MainViewModel(pasta, TimeProvider.System, Dispatcher, dados, abertura, new InteracaoJanelas(), leitorTasks);

        // Cria o WPF principal com a ViewModel
        var janela = new MainWindow { DataContext = _ViewModel };

        // Seta o WPF como janela principal
        MainWindow = janela;

        // Mostra a janela principal
        janela.Show();

        // Inicia os processos da ViewModel
        _ViewModel.Iniciar();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _ViewModel?.Dispose();
        base.OnExit(e);
    }

    #endregion

    #region "Métodos (Private)"

    private static string? LerParametro(string[] args, string nome)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], nome, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(args[i + 1]))
                return args[i + 1];
        return null;
    }

    #endregion

}
