using System.Windows;
using DataPilot.Core.Models;

namespace DataPilot.Wpf.Views;

/// <summary>Pasta de status (só leitura) e nome do container. Só fecha quando <c>aplicar</c> grava sem recusa.</summary>
public partial class ConfiguracoesWindow : Window
{
    private readonly Func<Configuracoes, string?> _aplicar;

    public ConfiguracoesWindow(string pastaStatus, Configuracoes atual, string? semSalvar, Func<Configuracoes, string?> aplicar)
    {
        _aplicar = aplicar;
        InitializeComponent();
        CampoPasta.Text = pastaStatus;
        CampoContainer.Text = atual.Container;
        if (semSalvar is not null)
        {
            Principal.IsEnabled = false;
            Mensagem.Text = semSalvar;
        }
    }

    private void Salvar_Click(object sender, RoutedEventArgs e)
    {
        var erro = _aplicar(new Configuracoes(CampoContainer.Text.Trim()));
        if (erro is null)
        {
            DialogResult = true;
            return;
        }

        Mensagem.Text = erro;
        CampoContainer.Focus();
        CampoContainer.SelectAll();
    }
}
