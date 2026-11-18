using System.Windows;
using System.Windows.Controls;
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
        var novo = existente is null;
        Title = novo ? "Nova demanda" : "Editar demanda";
        TituloJanela.Text = Title;
        Principal.Content = novo ? "Criar demanda" : "Salvar demanda";
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
        else MostrarErro(erro);
    }

    /// <summary>O erro aparece junto do campo a que se refere; sem campo claro, junto dos botões. Nada é apagado.</summary>
    private void MostrarErro(string erro)
    {
        foreach (var t in new[] { ErroId, ErroTitulo, ErroOpenspec, ErroGeral }) t.Text = "";
        TextBlock alvo;
        TextBox campo;
        if (erro.Contains("OpenSpec", StringComparison.OrdinalIgnoreCase)) { alvo = ErroOpenspec; campo = CampoOpenspec; }
        else if (erro.Contains("Título", StringComparison.Ordinal)) { alvo = ErroTitulo; campo = CampoTitulo; }
        else if (erro.Contains("dentificador", StringComparison.Ordinal)) { alvo = ErroId; campo = CampoId; }
        else { alvo = ErroGeral; campo = CampoId; }
        alvo.Text = erro;
        campo.Focus();
        campo.SelectAll();
    }
}
