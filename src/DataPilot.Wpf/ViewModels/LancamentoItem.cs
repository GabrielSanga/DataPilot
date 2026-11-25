using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core.Display;
using DataPilot.Core.Models;

namespace DataPilot.Wpf.ViewModels;

/// <summary>
/// Linha de um lançamento pendente: uma sessão nova do Codex ainda sem a primeira mensagem. Não é uma sessão:
/// só tem "Abrir" (abre o agente de novo, reaproveitando o lançamento) e "Cancelar". Chave: o identificador do
/// lançamento. Todo texto vem limpo do Core e é exibido como texto simples.
/// </summary>
public sealed partial class LancamentoItem : ObservableObject
{
    public string Id { get; }

    [ObservableProperty] private string _agente = "";
    [ObservableProperty] private string _rotulo = "";
    /// <summary>Repositório do lançamento, mostrado só em "Sessões".</summary>
    [ObservableProperty] private string _pasta = "";
    [ObservableProperty] private bool _emSessoes;
    [ObservableProperty] private string _tempo = "";

    public IRelayCommand AbrirCommand { get; }
    public IRelayCommand CancelarCommand { get; }

    private readonly MainViewModel _painel;

    internal LancamentoItem(string id, MainViewModel painel)
    {
        Id = id;
        _painel = painel;
        AbrirCommand = new RelayCommand(() => _painel.AbrirLancamento(this));
        CancelarCommand = new RelayCommand(() => _painel.CancelarLancamento(this));
    }

    public void Atualizar(LancamentoNaVisao v, string pasta, bool emSessoes)
    {
        var l = v.Lancamento;
        Agente = Agentes.Nome(l.Agente);
        Rotulo = l.Rotulo is null ? "" : TextoExibicao.Limpar(l.Rotulo);
        Pasta = TextoExibicao.NomePasta(pasta);
        EmSessoes = emSessoes;
        Tempo = v.Tempo.Length == 0 ? "—" : v.Tempo;
    }
}
