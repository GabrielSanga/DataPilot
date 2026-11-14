using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DataPilot.Core;

namespace DataPilot.Wpf;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MonitorPasta _monitor;
    private readonly TimeProvider _relogio;
    private readonly Dispatcher _dispatcher;
    private RetratoPasta? _retrato;

    public ObservableCollection<SessaoItem> Sessoes { get; } = [];

    [ObservableProperty] private string _resumo = "Carregando…";
    [ObservableProperty] private bool _pastaIndisponivel;
    [ObservableProperty] private string _aviso = "";
    [ObservableProperty] private bool _semSessoes;
    [ObservableProperty] private bool _mostrarAntigas;

    public MainViewModel(string pasta, TimeProvider relogio, Dispatcher dispatcher)
    {
        _relogio = relogio;
        _dispatcher = dispatcher;
        _monitor = new MonitorPasta(pasta);
        Aviso = $"Pasta de status indisponível: {TextoExibicao.Limpar(pasta)}";
        _monitor.RetratoAtualizado += r => _dispatcher.BeginInvoke(() => Receber(r));
    }

    public void Iniciar() => _monitor.Iniciar();

    partial void OnMostrarAntigasChanged(bool value) => Aplicar();

    private void Receber(RetratoPasta retrato)
    {
        _retrato = retrato;
        Aplicar();
    }

    private void Aplicar()
    {
        if (_retrato is null) return;
        PastaIndisponivel = !_retrato.Disponivel;

        var alvo = RegrasExibicao.Montar(_retrato.Sessoes, MostrarAntigas, _relogio);
        Resumo = _retrato.Disponivel ? RegrasExibicao.Resumo(alvo) : "";
        SemSessoes = _retrato.Disponivel && alvo.Count == 0;

        // Atualiza a coleção no lugar, por identificador, para preservar seleção e rolagem.
        var ids = alvo.Select(a => a.Sessao.SessionId).ToHashSet();
        for (var i = Sessoes.Count - 1; i >= 0; i--)
            if (!ids.Contains(Sessoes[i].SessionId)) Sessoes.RemoveAt(i);

        for (var i = 0; i < alvo.Count; i++)
        {
            var e = alvo[i];
            var atual = -1;
            for (var j = i; j < Sessoes.Count; j++)
                if (Sessoes[j].SessionId == e.Sessao.SessionId) { atual = j; break; }

            if (atual < 0) Sessoes.Insert(i, new SessaoItem(e));
            else
            {
                if (atual != i) Sessoes.Move(atual, i);
                Sessoes[i].Atualizar(e);
            }
        }
    }

    public void Dispose() => _monitor.Dispose();
}
