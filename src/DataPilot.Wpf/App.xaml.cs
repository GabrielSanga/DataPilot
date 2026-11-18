using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public partial class App : Application
{
    private MainViewModel? _vm;
    private ResourceDictionary? _altoContraste;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // O tema é escolhido na abertura e trocado quando o alto contraste do Windows liga, desliga ou muda de esquema.
        AplicarTema();
        SystemParameters.StaticPropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(SystemParameters.HighContrast) or null)
                Dispatcher.BeginInvoke(AplicarTema);
        };
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += (_, _) => Dispatcher.BeginInvoke(AplicarTema);

        var pasta = LerParametro(e.Args, "--pasta-status") ?? Constantes.PastaStatusPadrao;
        var dados = new DadosPainel(LerParametro(e.Args, "--dados") ?? DadosPainel.CaminhoPadrao);
        var abertura = new AberturaSessao(dados, new DisparadorProcesso(), TimeProvider.System);

        var leitorTasks = new LeitorTasks(LerParametro(e.Args, "--raiz-fontes") ?? Constantes.RaizFontesPadrao);

        _vm = new MainViewModel(pasta, TimeProvider.System, Dispatcher, dados, abertura, new InteracaoJanelas(), leitorTasks);
        var janela = new MainWindow { DataContext = _vm };
        MainWindow = janela;
        janela.Show();
        _vm.Iniciar();
    }

    /// <summary>
    /// Com o alto contraste ligado, mescla depois do tema o dicionário que aponta os mesmos nomes para as cores do
    /// sistema. Todos os pincéis são referenciados como recurso dinâmico, então a troca vale para as janelas abertas.
    /// </summary>
    private void AplicarTema()
    {
        var mesclados = Resources.MergedDictionaries;
        if (_altoContraste is not null)
        {
            mesclados.Remove(_altoContraste);
            _altoContraste = null;
        }
        if (SystemParameters.HighContrast)
        {
            _altoContraste = new ResourceDictionary
            {
                Source = new Uri("/DataPilot.Wpf;component/TemaAltoContraste.xaml", UriKind.Relative),
            };
            mesclados.Add(_altoContraste);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _vm?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Valor de um parâmetro como <c>--pasta-status</c>, <c>--dados</c> ou <c>--raiz-fontes</c>, informado por quem inicia o painel.</summary>
    private static string? LerParametro(string[] args, string nome)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], nome, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(args[i + 1]))
                return args[i + 1];
        return null;
    }
}
