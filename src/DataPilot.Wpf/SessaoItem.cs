using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core;

namespace DataPilot.Wpf;

/// <summary>
/// Linha de sessão. Todo texto vem limpo e cortado pelo Core; é exibido só como texto simples.
/// Sessões registradas (no card de um repositório) têm ações; as não vinculadas não têm nenhuma.
/// </summary>
public sealed partial class SessaoItem : ObservableObject
{
    public string SessionId { get; }

    [ObservableProperty] private string _agente = "";
    [ObservableProperty] private string _pasta = "";
    [ObservableProperty] private string _rotulo = "";
    [ObservableProperty] private string _dica = "";
    [ObservableProperty] private string _statusTexto = "";
    [ObservableProperty] private string _tempo = "";
    [ObservableProperty] private Brush _corFundo = Cores.Cinza;
    [ObservableProperty] private Brush _corTexto = Brushes.White;

    // Só as sessões registradas no arquivo de dados oferecem ações.
    [ObservableProperty] private bool _temAcoes;

    public StatusExibido Status { get; private set; }
    public SessaoRegistrada? Registrada { get; private set; }
    public IRelayCommand RetomarCommand { get; }
    public IRelayCommand MoverCommand { get; }
    public IRelayCommand RemoverCommand { get; }

    public SessaoItem(string sessionId, Action<SessaoItem> retomar, Action<SessaoItem> mover, Action<SessaoItem> remover)
    {
        SessionId = sessionId;
        RetomarCommand = new RelayCommand(() => retomar(this));
        MoverCommand = new RelayCommand(() => mover(this));
        RemoverCommand = new RelayCommand(() => remover(this));
    }

    /// <summary>Sessão registrada, com ou sem arquivo de status.</summary>
    public void Atualizar(SessaoNaVisao v)
    {
        var r = v.Registrada;
        Registrada = r;
        TemAcoes = true;
        Aplicar(v.Status, v.Tempo, v.Lida is null ? r.Pasta : TextoExibicao.NomePasta(v.Lida.Cwd), r.Agente);
        Rotulo = r.Rotulo is null ? "" : TextoExibicao.Limpar(r.Rotulo);
        Dica = (v.Lida is null ? "" : $"{TextoExibicao.Limpar(v.Lida.Cwd)}\n")
            + $"Sessão: {TextoExibicao.Limpar(r.SessionId)}\nRetomar abre em: {r.Pasta}";
    }

    /// <summary>Sessão lida da pasta de status que o painel não registrou: só exibição.</summary>
    public void AtualizarNaoVinculada(SessaoExibida e)
    {
        Registrada = null;
        TemAcoes = false;
        var s = e.Sessao;
        Aplicar(e.Status, e.Tempo, TextoExibicao.NomePasta(s.Cwd), s.Agente);
        Rotulo = "";
        Dica = $"{TextoExibicao.Limpar(s.Cwd)}\nSessão: {TextoExibicao.Limpar(s.SessionId)}";
    }

    private void Aplicar(StatusExibido status, string tempo, string pasta, Agente agente)
    {
        Status = status;
        Agente = agente == DataPilot.Core.Agente.Claude ? "Claude" : "Codex";
        Pasta = pasta;
        StatusTexto = RegrasExibicao.Texto(status);
        Tempo = tempo.Length == 0 ? "—" : tempo;
        CorFundo = Cores.Fundo(status);
        CorTexto = Cores.Texto(status);
    }
}

public static class Cores
{
    private static SolidColorBrush Congelado(byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(Color.FromRgb(r, g, b));
        br.Freeze();
        return br;
    }

    public static readonly Brush Vermelho = Congelado(0xC6, 0x28, 0x28);
    public static readonly Brush Azul = Congelado(0x15, 0x65, 0xC0);
    public static readonly Brush Amarelo = Congelado(0xF9, 0xA8, 0x25);
    public static readonly Brush Verde = Congelado(0x2E, 0x7D, 0x32);
    public static readonly Brush Cinza = Congelado(0x75, 0x75, 0x75);
    public static readonly Brush TextoEscuro = Congelado(0x21, 0x21, 0x21);

    public static Brush Fundo(StatusExibido s) => s switch
    {
        StatusExibido.PrecisaAtencao => Vermelho,
        StatusExibido.Executando => Azul,
        StatusExibido.Aguardando => Amarelo,
        StatusExibido.Ativa => Verde,
        _ => Cinza,
    };

    public static Brush Texto(StatusExibido s) => s == StatusExibido.Aguardando ? TextoEscuro : Brushes.White;
}
