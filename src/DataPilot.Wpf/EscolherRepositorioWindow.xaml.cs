using System.Windows;

namespace DataPilot.Wpf;

/// <summary>Escolha do repositório entre as pastas diretas da raiz dos fontes, para retomar uma sessão que o painel não registrou.</summary>
public partial class EscolherRepositorioWindow : Window
{
    public string? Escolhido { get; private set; }

    public EscolherRepositorioWindow(IReadOnlyList<string> pastas, string? sugerida)
    {
        InitializeComponent();
        CampoRepositorio.ItemsSource = pastas;
        CampoRepositorio.SelectedItem = pastas.FirstOrDefault(p => string.Equals(p, sugerida, StringComparison.OrdinalIgnoreCase))
            ?? pastas.FirstOrDefault();
        Loaded += (_, _) => CampoRepositorio.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (CampoRepositorio.SelectedItem is not string pasta) return;
        Escolhido = pasta;
        DialogResult = true;
    }
}
