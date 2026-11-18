using System.Windows;
using System.Windows.Controls;

namespace DataPilot.Wpf;

public partial class VincularSessaoWindow : Window
{
    private readonly Func<EscolhaVinculo, string?> _aplicar;

    public VincularSessaoWindow(string descricaoSessao, IReadOnlyList<OpcaoDemandaVinculo> demandas, Func<EscolhaVinculo, string?> aplicar)
    {
        _aplicar = aplicar;
        InitializeComponent();
        Descricao.Text = descricaoSessao;
        CampoDemanda.ItemsSource = demandas;
        // Pré-seleção: a primeira demanda com repositório de mesmo nome que a pasta da sessão.
        var inicial = demandas.FirstOrDefault(d => d.RepositorioSugerido is not null) ?? demandas.FirstOrDefault();
        CampoDemanda.SelectedItem = inicial;
        Loaded += (_, _) => CampoDemanda.Focus();
    }

    private void Demanda_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CampoDemanda.SelectedItem is not OpcaoDemandaVinculo d) return;
        CampoRepositorio.ItemsSource = d.Repositorios;
        CampoRepositorio.Text = d.RepositorioSugerido ?? (d.Repositorios.Count > 0 ? d.Repositorios[0] : "");
        LimparErros();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (CampoDemanda.SelectedItem is not OpcaoDemandaVinculo d) return;
        var rotulo = CampoRotulo.Text.Trim();
        var erro = _aplicar(new EscolhaVinculo(d.Id, CampoRepositorio.Text.Trim(), rotulo.Length == 0 ? null : rotulo));
        if (erro is null) DialogResult = true;
        else MostrarErro(erro);
    }

    private void LimparErros()
    {
        ErroRepositorio.Text = "";
        ErroRotulo.Text = "";
        ErroGeral.Text = "";
    }

    /// <summary>O erro aparece junto do campo a que se refere. "" é a recusa sem mensagem (o aviso não foi confirmado).</summary>
    private void MostrarErro(string erro)
    {
        LimparErros();
        if (erro.Length == 0) return;
        if (erro.StartsWith("Repositório", StringComparison.Ordinal)) ErroRepositorio.Text = erro;
        else if (erro.StartsWith("Rótulo", StringComparison.Ordinal)) ErroRotulo.Text = erro;
        else ErroGeral.Text = erro;
    }
}
