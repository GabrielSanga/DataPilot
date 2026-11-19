using System.Windows;

namespace DataPilot.Wpf.Views;

/// <summary>Criar (título atual nulo) ou editar um trabalho, só pelo título. Só fecha quando <c>aplicar</c> grava sem recusa.</summary>
public partial class DemandaWindow : Window
{
    private readonly Func<string, string?> _aplicar;

    public DemandaWindow(string? tituloAtual, Func<string, string?> aplicar)
    {
        _aplicar = aplicar;
        InitializeComponent();
        var novo = tituloAtual is null;
        Title = novo ? "Novo trabalho" : "Editar trabalho";
        TituloJanela.Text = Title;
        Principal.Content = novo ? "Criar trabalho" : "Salvar trabalho";
        if (tituloAtual is not null) CampoTitulo.Text = tituloAtual;
        Loaded += (_, _) =>
        {
            CampoTitulo.Focus();
            CampoTitulo.SelectAll();
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var erro = _aplicar(CampoTitulo.Text.Trim());
        if (erro is null) DialogResult = true;
        else
        {
            // O erro aparece junto do campo. Nada é apagado.
            ErroTitulo.Text = erro;
            CampoTitulo.Focus();
            CampoTitulo.SelectAll();
        }
    }
}
