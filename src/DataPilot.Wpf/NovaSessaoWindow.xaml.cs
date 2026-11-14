using System.Collections.ObjectModel;
using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public partial class NovaSessaoWindow : Window
{
    public string? Pasta { get; private set; }
    public Agente AgenteEscolhido { get; private set; }
    public string? Rotulo { get; private set; }

    public NovaSessaoWindow(ObservableCollection<RepositorioCadastrado> repositorios)
    {
        InitializeComponent();
        CampoRepositorio.ItemsSource = repositorios;
        CampoRepositorio.SelectedIndex = 0;
    }

    private void Abrir_Click(object sender, RoutedEventArgs e)
    {
        if (CampoRepositorio.SelectedItem is not RepositorioCadastrado repo)
        {
            Mensagem.Text = "Escolha um repositório.";
            return;
        }

        var rotulo = CampoRotulo.Text.Trim();
        if (rotulo.Length > 0 && !ValidadorEntrada.RotuloValido(rotulo))
        {
            Mensagem.Text = "Rótulo inválido: use até 40 caracteres, sem \";\" nem caracteres de controle.";
            return;
        }

        Pasta = repo.Pasta;
        AgenteEscolhido = CampoAgente.SelectedIndex == 1 ? Agente.Codex : Agente.Claude;
        Rotulo = rotulo.Length == 0 ? null : rotulo;
        DialogResult = true;
    }
}
