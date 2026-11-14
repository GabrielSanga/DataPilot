using System.Windows;

namespace DataPilot.Wpf;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void NovaDemanda_Click(object sender, RoutedEventArgs e) => Vm.NovaDemanda();
}
