using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core;

namespace DataPilot.Wpf;

/// <summary>
/// Nível visual de um status. Só "precisa de atenção" e "aguardando você" acendem (faixa e texto em destaque);
/// os demais ficam em tom baixo. O tema transforma o nível em cor, o que permite trocar as cores no alto contraste.
/// </summary>
public enum NivelSinal { Neutro, Atencao, Aguardando, Executando, Ativa }

public static class Sinal
{
    public static NivelSinal De(StatusExibido? status) => status switch
    {
        StatusExibido.PrecisaAtencao => NivelSinal.Atencao,
        StatusExibido.Aguardando => NivelSinal.Aguardando,
        StatusExibido.Executando => NivelSinal.Executando,
        StatusExibido.Ativa => NivelSinal.Ativa,
        _ => NivelSinal.Neutro,
    };

    /// <summary>O status acende a faixa da linha: só os dois que precisam do usuário.</summary>
    public static bool Acende(StatusExibido? status) => status is StatusExibido.PrecisaAtencao or StatusExibido.Aguardando;
}

/// <summary>
/// Linha de sessão. Todo texto vem limpo e cortado pelo Core; é exibido só como texto simples.
/// Sessões registradas (no repositório) têm "Retomar" e o menu de mais ações (Mover, Remover); as não vinculadas
/// só têm "Vincular".
/// </summary>
public sealed partial class SessaoItem : ObservableObject
{
    public string SessionId { get; }

    [ObservableProperty] private string _agente = "";
    [ObservableProperty] private string _pasta = "";
    [ObservableProperty] private string _rotulo = "";
    /// <summary>Texto principal da linha: o rótulo (ou "sem rótulo") nas registradas; a pasta nas não vinculadas.</summary>
    [ObservableProperty] private string _textoPrincipal = "";
    [ObservableProperty] private bool _principalEhVazio;
    [ObservableProperty] private string _dica = "";
    [ObservableProperty] private string _statusTexto = "";
    [ObservableProperty] private string _tempo = "";
    [ObservableProperty] private NivelSinal _nivel;

    // Só as sessões registradas no arquivo de dados oferecem ações.
    [ObservableProperty] private bool _temAcoes;

    public StatusExibido Status { get; private set; }
    public SessaoRegistrada? Registrada { get; private set; }
    public IRelayCommand RetomarCommand { get; }
    public IRelayCommand MoverCommand { get; }
    public IRelayCommand RemoverCommand { get; }
    public IRelayCommand VincularCommand { get; }

    // Sessão não vinculada (na entrada "Não vinculadas"): só a ação de vincular.
    [ObservableProperty] private bool _temVincular;
    [ObservableProperty] private bool _podeVincular;
    [ObservableProperty] private string _motivoVincular = "";
    /// <summary>O motivo de "Vincular" indisponível, mostrado na própria linha.</summary>
    [ObservableProperty] private bool _mostrarMotivo;

    /// <summary>Sessão lida da pasta de status (só para sessões não vinculadas); nunca vai a um comando.</summary>
    public Sessao? Lida { get; private set; }

    public SessaoItem(string sessionId, Action<SessaoItem> retomar, Action<SessaoItem> mover, Action<SessaoItem> remover,
        Action<SessaoItem> vincular)
    {
        SessionId = sessionId;
        VincularCommand = new RelayCommand(() => vincular(this));
        RetomarCommand = new RelayCommand(() => retomar(this));
        MoverCommand = new RelayCommand(() => mover(this));
        RemoverCommand = new RelayCommand(() => remover(this));
    }

    /// <summary>Sessão registrada, com ou sem arquivo de status.</summary>
    public void Atualizar(SessaoNaVisao v)
    {
        var r = v.Registrada;
        Registrada = r;
        Lida = null;
        TemAcoes = true;
        TemVincular = false;
        MostrarMotivo = false;
        Aplicar(v.Status, v.Tempo, v.Lida is null ? r.Pasta : TextoExibicao.NomePasta(v.Lida.Cwd), r.Agente);
        Rotulo = r.Rotulo is null ? "" : TextoExibicao.Limpar(r.Rotulo);
        PrincipalEhVazio = Rotulo.Length == 0;
        TextoPrincipal = PrincipalEhVazio ? "sem rótulo" : Rotulo;
        Dica = (v.Lida is null ? "" : $"{TextoExibicao.Limpar(v.Lida.Cwd)}\n")
            + $"Sessão: {TextoExibicao.Limpar(r.SessionId)}\nRetomar abre em: {r.Pasta}";
    }

    /// <summary>Sessão lida da pasta de status que o painel não registrou: só exibição e a ação de vincular.</summary>
    public void AtualizarNaoVinculada(SessaoExibida e)
    {
        Registrada = null;
        Lida = e.Sessao;
        TemAcoes = false;
        TemVincular = true;
        var s = e.Sessao;
        var pode = RegrasVinculo.PodeVincularSessao(s);
        PodeVincular = pode.Sim;
        MotivoVincular = pode.Motivo ?? "";
        MostrarMotivo = !pode.Sim && MotivoVincular.Length > 0;
        Aplicar(e.Status, e.Tempo, TextoExibicao.NomePasta(s.Cwd), s.Agente);
        Rotulo = "";
        PrincipalEhVazio = false;
        TextoPrincipal = Pasta;
        Dica = $"{TextoExibicao.Limpar(s.Cwd)}\nSessão: {TextoExibicao.Limpar(s.SessionId)}"
            + (pode.Sim ? "" : $"\n{pode.Motivo}");
    }

    private void Aplicar(StatusExibido status, string tempo, string pasta, Agente agente)
    {
        Status = status;
        Agente = agente == DataPilot.Core.Agente.Claude ? "Claude" : "Codex";
        Pasta = pasta;
        StatusTexto = RegrasExibicao.Texto(status);
        Tempo = tempo.Length == 0 ? "—" : tempo;
        Nivel = Sinal.De(status);
    }
}
