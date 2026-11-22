using System.Windows;
using DataPilot.Core.OpenSpec;
using DataPilot.Wpf.Services;

namespace DataPilot.Wpf.Views;

/// <summary>
/// "Vincular repositório" (abre a escolha de pasta do Windows na raiz dos fontes e só aceita uma pasta direta dela) e
/// "Trocar change" (pasta já conhecida). Lista as changes ativas do repositório e "Nenhuma". Só fecha quando
/// <c>aplicar</c> grava sem recusa.
/// </summary>
public partial class VincularRepositorioWindow : Window
{
    private const string Nenhuma = "Nenhuma";

    private readonly string _raiz;
    private readonly Func<string, string?, string?> _aplicar;
    private readonly string? _changeAtual;
    private string? _pasta;

    public VincularRepositorioWindow(string raiz, string? pastaFixa, string? changeAtual, Func<string, string?, string?> aplicar)
    {
        _raiz = raiz;
        _aplicar = aplicar;
        _changeAtual = changeAtual;
        InitializeComponent();

        var troca = pastaFixa is not null;
        Title = troca ? "Trocar change" : "Vincular repositório";
        TituloJanela.Text = Title;
        Principal.Content = troca ? "Trocar change" : "Vincular";
        BotaoEscolher.Visibility = troca ? Visibility.Collapsed : Visibility.Visible;
        Principal.IsEnabled = false;

        if (troca) DefinirPasta(pastaFixa!);
        else
        {
            // A escolha de pasta do Windows abre junto; cancelá-la fecha o diálogo sem vincular nada.
            Loaded += (_, _) =>
            {
                if (!EscolherPasta()) DialogResult = false;
            };
        }
    }

    private void Escolher_Click(object sender, RoutedEventArgs e) => EscolherPasta();

    /// <summary>Abre a escolha de pasta. Devolve false se o usuário cancelou; pasta recusada mostra o erro e mantém a anterior.</summary>
    private bool EscolherPasta()
    {
        var caminho = InteracaoJanelas.EscolherPasta(_raiz, this);
        if (caminho is null) return false;

        var r = EscolhaDePasta.Validar(_raiz, caminho);
        if (!r.Ok)
        {
            ErroPasta.Text = r.Erro!;
            BotaoEscolher.Focus();
            return true;
        }
        DefinirPasta(r.Pasta!);
        return true;
    }

    private void DefinirPasta(string pasta)
    {
        _pasta = pasta;
        ErroPasta.Text = "";
        ErroGeral.Text = "";
        CampoPasta.Text = pasta;

        var opcoes = new List<string>(ChangesDoRepositorio.Listar(_raiz, pasta));
        // Uma change já escolhida continua oferecida, mesmo que a pasta dela tenha mudado de lugar.
        if (_changeAtual is not null && !opcoes.Contains(_changeAtual, StringComparer.OrdinalIgnoreCase)) opcoes.Insert(0, _changeAtual);
        opcoes.Add(Nenhuma);
        ListaChanges.ItemsSource = opcoes;
        ListaChanges.SelectedItem = _changeAtual is null
            ? Nenhuma
            : opcoes.First(o => string.Equals(o, _changeAtual, StringComparison.OrdinalIgnoreCase));
        Principal.IsEnabled = true;
        ListaChanges.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (_pasta is null) return;
        var escolhida = ListaChanges.SelectedItem as string;
        var erro = _aplicar(_pasta, escolhida is null or Nenhuma ? null : escolhida);
        if (erro is null) DialogResult = true;
        else ErroGeral.Text = erro;
    }
}
