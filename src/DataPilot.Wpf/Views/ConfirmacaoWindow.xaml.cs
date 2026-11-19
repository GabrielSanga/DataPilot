using System.Windows;

namespace DataPilot.Wpf.Views;

/// <summary>
/// Confirmação do próprio painel para ações que removem ou descartam algo. O botão de confirmação leva o nome da
/// ação e "Cancelar" é o padrão (Enter e Esc cancelam); o foco inicial fica em "Cancelar".
/// </summary>
public partial class ConfirmacaoWindow : Window
{
    public ConfirmacaoWindow(string titulo, string mensagem, string acao)
    {
        InitializeComponent();
        Titulo.Text = titulo;
        Mensagem.Text = mensagem;
        Confirmar.Content = acao;
        Loaded += (_, _) => Cancelar.Focus();
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
