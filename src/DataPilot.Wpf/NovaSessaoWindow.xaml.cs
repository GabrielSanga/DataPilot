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
        Destino.Text = $"Repositório {pasta}, demanda {demandaTitulo}";
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
