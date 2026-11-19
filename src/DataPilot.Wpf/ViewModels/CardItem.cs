using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core.Display;
using DataPilot.Core.Models;
using DataPilot.Core.OpenSpec;
using DataPilot.Wpf.Helpers;

namespace DataPilot.Wpf.ViewModels;

/// <summary>Card de um repositório dentro de um trabalho. Chave: a pasta (única dentro do trabalho).</summary>
public sealed partial class CardItem : ObservableObject
{
    public string Pasta { get; }
    public string DemandaId { get; private set; } = "";
    public ObservableCollection<SessaoItem> Sessoes { get; } = [];
    public ObservableCollection<LancamentoItem> Lancamentos { get; } = [];

    [ObservableProperty] private bool _vazio;

    /// <summary>Change do OpenSpec deste repositório, própria ou herdada; vazio quando não há.</summary>
    [ObservableProperty] private string _change = "";
    [ObservableProperty] private bool _temChange;
    /// <summary>Progresso da change ("4 de 6") ou a indicação que o substitui; vazio quando não há o que mostrar.</summary>
    [ObservableProperty] private string _progressoTexto = "";
    [ObservableProperty] private bool _temProgresso;
    [ObservableProperty] private bool _temBarra;
    [ObservableProperty] private double _progressoFracao;

    [ObservableProperty] private bool _podeNovaSessao;
    public IRelayCommand NovaSessaoCommand { get; }
    public IRelayCommand TrocarChangeCommand { get; }
    public IRelayCommand RemoverRepositorioCommand { get; }

    public CardItem(string pasta, Action<CardItem> novaSessao, Action<CardItem> trocarChange, Action<CardItem> remover)
    {
        Pasta = pasta;
        NovaSessaoCommand = new RelayCommand(() => novaSessao(this));
        TrocarChangeCommand = new RelayCommand(() => trocarChange(this));
        RemoverRepositorioCommand = new RelayCommand(() => remover(this));
    }

    public void Atualizar(CardNaVisao card, Demanda demanda, Func<string, SessaoItem> criarSessao,
        Func<string, LancamentoItem> criarLancamento)
    {
        DemandaId = demanda.Id;
        PodeNovaSessao = !demanda.Arquivada;
        Vazio = card.Sessoes.Count == 0 && card.Lancamentos.Count == 0;
        var repo = demanda.Repositorios.FirstOrDefault(r => string.Equals(r.Pasta, card.Pasta, StringComparison.OrdinalIgnoreCase));
        Change = repo?.OpenspecChange is null ? "" : TextoExibicao.Limpar(repo.OpenspecChange);
        TemChange = Change.Length > 0;
        if (!TemChange) AplicarProgresso(null);
        SincronizacaoColecao.Sincronizar(Sessoes, card.Sessoes,
            s => s.SessionId, s => s.Registrada.SessionId,
            s => criarSessao(s.Registrada.SessionId),
            (item, dado) => item.Atualizar(dado, emSessoes: false));
        SincronizacaoColecao.Sincronizar(Lancamentos, card.Lancamentos,
            l => l.Id, l => l.Lancamento.Id,
            l => criarLancamento(l.Lancamento.Id),
            (item, dado) => item.Atualizar(dado, card.Pasta, emSessoes: false));
    }

    /// <summary>Mostra o progresso da change deste repositório. Texto vem do Core, exibido como texto simples.</summary>
    public void AplicarProgresso(ProgressoRepositorio? p)
    {
        ProgressoTexto = p?.Texto ?? "";
        TemProgresso = ProgressoTexto.Length > 0;
        TemBarra = p?.Estado == EstadoRepositorio.Lido;
        ProgressoFracao = p?.Fracao ?? 0;
    }
}
