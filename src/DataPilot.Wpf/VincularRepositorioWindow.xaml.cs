using System.Windows;

namespace DataPilot.Wpf;

public partial class VincularRepositorioWindow : Window
{
    private readonly Func<string, string?> _aplicar;

    public VincularRepositorioWindow(string demandaTitulo, IReadOnlyList<string> sugestoes, Func<string, string?> aplicar)
    {
        _aplicar = aplicar;
        InitializeComponent();
        Destino.Text = $"Demanda: {demandaTitulo}";
        CampoPasta.ItemsSource = sugestoes;
        CampoPasta.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var erro = _aplicar(CampoPasta.Text.Trim());
        if (erro is null) DialogResult = true;
        else Mensagem.Text = erro;
    }
}
