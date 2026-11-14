using System.Windows;

namespace DataPilot.Wpf;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Repositorios_Click(object sender, RoutedEventArgs e)
    {
        new RepositoriosWindow(Vm) { Owner = this }.ShowDialog();
    }

    private void NovaSessao_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Repositorios.Count == 0)
        {
            Vm.AvisarSemRepositorios();
            return;
        }

        var dialogo = new NovaSessaoWindow(Vm.Repositorios) { Owner = this };
        if (dialogo.ShowDialog() != true || dialogo.Pasta is null) return;

        var r = Vm.NovaSessao(dialogo.Pasta, dialogo.AgenteEscolhido, dialogo.Rotulo);
        if (!r.Ok) Vm.MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }
}
