using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public partial class RepositoriosWindow : Window
{
    private readonly MainViewModel _vm;

    public RepositoriosWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        CampoPasta.Focus();
    }

    private void Adicionar_Click(object sender, RoutedEventArgs e)
    {
        var erro = _vm.Cadastrar(CampoPasta.Text);
        Mensagem.Text = erro ?? "";
        if (erro is null) CampoPasta.Clear();
        CampoPasta.Focus();
    }

    private void Remover_Click(object sender, RoutedEventArgs e)
    {
        if (Lista.SelectedItem is not RepositorioCadastrado repo) return;
        Mensagem.Text = _vm.Remover(repo) ?? "";
    }
}
