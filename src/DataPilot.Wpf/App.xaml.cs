using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public partial class App : Application
{
    private MainViewModel? _vm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var pasta = LerParametro(e.Args, "--pasta-status") ?? Constantes.PastaStatusPadrao;
        var dados = new DadosPainel(LerParametro(e.Args, "--dados") ?? DadosPainel.CaminhoPadrao);
        var abertura = new AberturaSessao(dados, new DisparadorProcesso(), TimeProvider.System);

        _vm = new MainViewModel(pasta, TimeProvider.System, Dispatcher, dados, abertura, new InteracaoJanelas());
        var janela = new MainWindow { DataContext = _vm };
        MainWindow = janela;
        janela.Show();
        _vm.Iniciar();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _vm?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Valor de um parâmetro como <c>--pasta-status</c> ou <c>--dados</c>, informado por quem inicia o painel.</summary>
    private static string? LerParametro(string[] args, string nome)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], nome, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(args[i + 1]))
                return args[i + 1];
        return null;
    }
}
