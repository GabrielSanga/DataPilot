using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

public partial class NovaSessaoWindow : Window
{
    public Agente AgenteEscolhido { get; private set; }
    public string? Rotulo { get; private set; }

    public NovaSessaoWindow(string demandaTitulo, string pasta)
    {
        InitializeComponent();
        Destino.Text = $"Repositório {pasta} na demanda {demandaTitulo}";
    }

    private void Abrir_Click(object sender, RoutedEventArgs e)
    {
        var rotulo = CampoRotulo.Text.Trim();
        if (rotulo.Length > 0 && !ValidadorEntrada.RotuloValido(rotulo))
        {
            Mensagem.Text = "Rótulo inválido: use até 40 caracteres, sem \";\" nem caracteres de controle.";
            return;
        }

        AgenteEscolhido = CampoAgente.SelectedIndex == 1 ? Agente.Codex : Agente.Claude;
        Rotulo = rotulo.Length == 0 ? null : rotulo;
        DialogResult = true;
    }
}
