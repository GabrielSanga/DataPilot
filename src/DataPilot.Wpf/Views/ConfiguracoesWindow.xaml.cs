using System.Windows;
using DataPilot.Core.Models;

namespace DataPilot.Wpf.Views;

/// <summary>Pasta de status (só leitura), "Abrir pelo SSH" e usuário. Só fecha quando <c>aplicar</c> grava sem recusa.</summary>
public partial class ConfiguracoesWindow : Window
{
    private readonly Func<Configuracoes, string?> _aplicar;
    private readonly string _usuarioGravado;

    public ConfiguracoesWindow(string pastaStatus, Configuracoes atual, string? semSalvar, Func<Configuracoes, string?> aplicar)
    {
        _aplicar = aplicar;
        _usuarioGravado = atual.UsuarioSsh;
        InitializeComponent();
        CampoPasta.Text = pastaStatus;
        CampoSsh.IsChecked = atual.UsarSsh;
        CampoUsuario.Text = atual.UsuarioSsh;
        if (semSalvar is not null)
        {
            Principal.IsEnabled = false;
            Mensagem.Text = semSalvar;
        }
    }

    private void Salvar_Click(object sender, RoutedEventArgs e)
    {
        var usarSsh = CampoSsh.IsChecked == true;
        // Com o SSH desligado o campo está desabilitado e o usuário gravado continua o mesmo.
        var erro = _aplicar(new Configuracoes(usarSsh, usarSsh ? CampoUsuario.Text.Trim() : _usuarioGravado));
        if (erro is null) DialogResult = true;
        else
        {
            Mensagem.Text = erro;
            CampoUsuario.Focus();
            CampoUsuario.SelectAll();
        }
    }
}
