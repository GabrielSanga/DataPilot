using System.Windows;
using DataPilot.Core;

namespace DataPilot.Wpf;

/// <summary>Criar (existente nulo) ou editar uma demanda. Só fecha quando <c>aplicar</c> grava sem recusa.</summary>
public partial class DemandaWindow : Window
{
    private readonly Func<DadosDemanda, string?> _aplicar;

    public DemandaWindow(Demanda? existente, Func<DadosDemanda, string?> aplicar)
    {
        _aplicar = aplicar;
        InitializeComponent();
        Title = existente is null ? "Nova demanda" : "Editar demanda";
        if (existente is not null)
        {
            CampoId.Text = existente.Id;
            CampoTitulo.Text = existente.Titulo;
            CampoOpenspec.Text = existente.OpenspecChange ?? "";
        }
        CampoId.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var openspec = CampoOpenspec.Text.Trim();
        var erro = _aplicar(new DadosDemanda(CampoId.Text.Trim(), CampoTitulo.Text.Trim(), openspec.Length == 0 ? null : openspec));
        if (erro is null) DialogResult = true;
        else Mensagem.Text = erro;
    }
}
