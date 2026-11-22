using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core.Display;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;

namespace DataPilot.Wpf.ViewModels;

/// <summary>
/// Linha de sessão. Todo texto vem limpo e cortado pelo Core; é exibido só como texto simples.
/// Sessões de um trabalho têm "Retomar" e "Excluir"; as de "Sessões" também mostram o repositório e têm o menu
/// "Vincular a um trabalho". O status aparece como indicador colorido; o nome dele fica na dica e no nome acessível.
/// </summary>
public sealed partial class SessaoItem : ObservableObject
{
    public string SessionId { get; }

    [ObservableProperty] private string _agente = "";
    [ObservableProperty] private string _pasta = "";
    /// <summary>Rótulo da sessão; em branco quando não há.</summary>
    [ObservableProperty] private string _rotulo = "";
    [ObservableProperty] private string _dica = "";
    [ObservableProperty] private string _statusTexto = "";
    [ObservableProperty] private string _tempo = "";
    [ObservableProperty] private NivelSinal _nivel;

    /// <summary>A linha está em "Sessões": mostra o repositório e o menu "Vincular a um trabalho".</summary>
    [ObservableProperty] private bool _emSessoes;
    [ObservableProperty] private bool _podeRetomar;
    [ObservableProperty] private bool _podeVincular;
    /// <summary>O motivo de "Retomar" e "Vincular" estarem indisponíveis, mostrado em texto na própria linha.</summary>
    [ObservableProperty] private string _motivo = "";
    [ObservableProperty] private bool _mostrarMotivo;

    public StatusExibido Status { get; private set; }
    public SessaoRegistrada? Registrada { get; private set; }

    /// <summary>Sessão lida da pasta de status, quando há; nunca vai a um comando.</summary>
    public Sessao? Lida { get; private set; }

    /// <summary>Agente da sessão, do registro ou da leitura.</summary>
    public Agente AgenteDaSessao { get; private set; }

    public IRelayCommand RetomarCommand { get; }
    public IRelayCommand ExcluirCommand { get; }
    public IRelayCommand VincularCommand { get; }

    private readonly MainViewModel _painel;

    internal SessaoItem(string sessionId, MainViewModel painel)
    {
        SessionId = sessionId;
        _painel = painel;
        RetomarCommand = new RelayCommand(() => _painel.Retomar(this));
        ExcluirCommand = new RelayCommand(() => _painel.ExcluirSessao(this));
        VincularCommand = new RelayCommand(() => _painel.VincularSessao(this));
    }

    /// <summary>Sessão registrada, com ou sem arquivo de status. <paramref name="emSessoes"/>: a linha está em "Sessões".</summary>
    public void Atualizar(SessaoNaVisao v, bool emSessoes)
    {
        var r = v.Registrada;
        Registrada = r;
        Lida = v.Lida;
        AgenteDaSessao = r.Agente;
        EmSessoes = emSessoes;
        PodeRetomar = true;
        PodeVincular = true;
        Motivo = "";
        MostrarMotivo = false;
        Aplicar(v.Status, v.Tempo, r.Pasta, r.Agente);
        Rotulo = r.Rotulo is null ? "" : TextoExibicao.Limpar(r.Rotulo);
        Dica = $"Status: {StatusTexto}\n" + (v.Lida is null ? "" : $"{TextoExibicao.Limpar(v.Lida.Cwd)}\n")
            + $"Sessão: {TextoExibicao.Limpar(r.SessionId)}\nRetomar abre em: {r.Pasta}";
    }

    /// <summary>Sessão da pasta de status que o painel não registrou: aparece em "Sessões" com "Retomar", "Excluir" e vínculo.</summary>
    public void AtualizarNaoRegistrada(SessaoExibida e)
    {
        Registrada = null;
        Lida = e.Sessao;
        AgenteDaSessao = e.Sessao.Agente;
        EmSessoes = true;
        var s = e.Sessao;
        var pode = PedidoVinculo.Criar(s) is not null;
        PodeRetomar = pode;
        PodeVincular = pode;
        Motivo = pode ? "" : RegrasVinculo.MotivoIdentificadorInvalido;
        MostrarMotivo = !pode;
        Aplicar(e.Status, e.Tempo, TextoExibicao.NomePasta(s.Cwd), s.Agente);
        Rotulo = "";
        Dica = $"Status: {StatusTexto}\n{TextoExibicao.Limpar(s.Cwd)}\nSessão: {TextoExibicao.Limpar(s.SessionId)}"
            + (pode ? "" : $"\n{Motivo}");
    }

    private void Aplicar(StatusExibido status, string tempo, string pasta, Agente agente)
    {
        Status = status;
        Agente = Agentes.Nome(agente);
        Pasta = pasta;
        StatusTexto = RegrasExibicao.Texto(status);
        Tempo = tempo.Length == 0 ? "—" : tempo;
        Nivel = Sinal.De(status);
    }
}
