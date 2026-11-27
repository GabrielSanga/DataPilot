using System.Windows;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Wpf.Views;

/// <summary>
/// Pasta de status (só leitura), ambiente de execução, nome do container, "Abrir pelo SSH" e usuário. Só fecha quando
/// <c>aplicar</c> grava sem recusa.
/// </summary>
public partial class ConfiguracoesWindow : Window
{
    private readonly Func<Configuracoes, string?> _aplicar;
    private readonly Configuracoes _gravadas;

    public ConfiguracoesWindow(string pastaStatus, Configuracoes atual, string? semSalvar, Func<Configuracoes, string?> aplicar)
    {
        _aplicar = aplicar;
        _gravadas = atual;
        InitializeComponent();
        CampoPasta.Text = pastaStatus;
        EscolhaContainer.IsChecked = atual.Execucao == Execucao.Container;
        EscolhaWindows.IsChecked = atual.Execucao == Execucao.Windows;
        CampoContainer.Text = atual.Container;
        CampoSsh.IsChecked = atual.UsarSsh;
        CampoUsuario.Text = atual.UsuarioSsh;
        EscolhaWindows.Checked += (_, _) => MensagemContainer.Text = "";
        if (semSalvar is not null)
        {
            Principal.IsEnabled = false;
            Mensagem.Text = semSalvar;
        }
    }

    private void Salvar_Click(object sender, RoutedEventArgs e)
    {
        var execucao = EscolhaWindows.IsChecked == true ? Execucao.Windows : Execucao.Container;
        var usarSsh = CampoSsh.IsChecked == true;
        // Campo desabilitado não muda o valor gravado.
        var container = execucao == Execucao.Container ? CampoContainer.Text.Trim() : _gravadas.Container;
        var usuario = usarSsh ? CampoUsuario.Text.Trim() : _gravadas.UsuarioSsh;
        var erro = _aplicar(new Configuracoes(execucao, container, usarSsh, usuario));
        if (erro is null)
        {
            DialogResult = true;
            return;
        }

        // O erro aparece junto do campo recusado.
        var noContainer = !ValidadorEntrada.ContainerValido(container);
        MensagemContainer.Text = noContainer ? erro : "";
        Mensagem.Text = noContainer ? "" : erro;
        var campo = noContainer ? CampoContainer : CampoUsuario;
        campo.Focus();
        campo.SelectAll();
    }
}
