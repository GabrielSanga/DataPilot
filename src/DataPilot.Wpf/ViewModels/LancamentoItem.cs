using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core.Display;

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

    public LancamentoItem(string id, Action<LancamentoItem> abrir, Action<LancamentoItem> cancelar)
    {
        Id = id;
        AbrirCommand = new RelayCommand(() => abrir(this));
        CancelarCommand = new RelayCommand(() => cancelar(this));
    }

    public void Atualizar(LancamentoNaVisao v, string pasta, bool emSessoes)
    {
        var l = v.Lancamento;
        Agente = l.Agente == DataPilot.Core.Models.Agente.Claude ? "Claude" : "Codex";
        Rotulo = l.Rotulo is null ? "" : TextoExibicao.Limpar(l.Rotulo);
        Pasta = TextoExibicao.Limpar(pasta);
        EmSessoes = emSessoes;
        Tempo = v.Tempo.Length == 0 ? "—" : v.Tempo;
    }
}
