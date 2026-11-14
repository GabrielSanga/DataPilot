using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public partial class App : Application
{
    private MainViewModel? _vm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var pasta = LerPastaStatus(e.Args) ?? Constantes.PastaStatusPadrao;
        _vm = new MainViewModel(pasta, TimeProvider.System, Dispatcher);
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

    /// <summary>Valor de <c>--pasta-status</c>, informado por quem inicia o painel.</summary>
    private static string? LerPastaStatus(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], "--pasta-status", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(args[i + 1]))
                return args[i + 1];
        return null;
    }
}
