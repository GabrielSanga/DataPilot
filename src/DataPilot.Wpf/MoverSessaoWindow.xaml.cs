using System.Windows;
using System.Windows.Input;

namespace DataPilot.Wpf;

public partial class MoverSessaoWindow : Window
{
    public string? DestinoEscolhido { get; private set; }

    public MoverSessaoWindow(IReadOnlyList<DestinoDemanda> destinos)
    {
        InitializeComponent();
        Lista.ItemsSource = destinos;
        Lista.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Confirmar();

    private void Lista_DoubleClick(object sender, MouseButtonEventArgs e) => Confirmar();

    private void Confirmar()
    {
        if (Lista.SelectedItem is not DestinoDemanda destino) return;
        DestinoEscolhido = destino.Id;
        DialogResult = true;
    }
}
