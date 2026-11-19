using System.Windows;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Wpf.Views;

public partial class NovaSessaoWindow : Window
{
    public Agente AgenteEscolhido { get; private set; }
    public string? Rotulo { get; private set; }

    public NovaSessaoWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => CampoRotulo.Focus();
    }

    private void Abrir_Click(object sender, RoutedEventArgs e)
    {
        var rotulo = CampoRotulo.Text.Trim();
        if (rotulo.Length > 0 && !ValidadorEntrada.RotuloValido(rotulo))
        {
            Mensagem.Text = "Rótulo inválido: use até 40 caracteres, sem \";\" nem caracteres de controle.";
            CampoRotulo.Focus();
            return;
        }

        AgenteEscolhido = EscolhaCodex.IsChecked == true ? Agente.Codex : Agente.Claude;
        Rotulo = rotulo.Length == 0 ? null : rotulo;
        DialogResult = true;
    }
}
