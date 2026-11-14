using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core;

namespace DataPilot.Wpf;

/// <summary>Linha da lista. Todo texto vem limpo e cortado pelo Core; é exibido só como texto simples.</summary>
public sealed partial class SessaoItem : ObservableObject
{
    public string SessionId { get; }

    [ObservableProperty] private string _agente = "";
    [ObservableProperty] private string _pasta = "";
    [ObservableProperty] private string _pastaCompleta = "";
    [ObservableProperty] private string _dica = "";
    [ObservableProperty] private string _statusTexto = "";
    [ObservableProperty] private string _tempo = "";
    [ObservableProperty] private Brush _corFundo = Cores.Cinza;
    [ObservableProperty] private Brush _corTexto = Brushes.White;

    // Dados do painel (arquivo de dados), nunca do arquivo de status.
    [ObservableProperty] private bool _podeRetomar;
    [ObservableProperty] private string _registro = "";

    public StatusExibido Status { get; private set; }
    public SessaoRegistrada? Registrada { get; private set; }
    public IRelayCommand RetomarCommand { get; }

    public SessaoItem(SessaoExibida e, SessaoRegistrada? registrada, Action<SessaoItem> retomar)
    {
        SessionId = e.Sessao.SessionId;
        RetomarCommand = new RelayCommand(() => retomar(this));
        Atualizar(e, registrada);
    }

    public void Atualizar(SessaoExibida e, SessaoRegistrada? registrada)
    {
        var s = e.Sessao;
        Status = e.Status;
        Registrada = registrada;
        Agente = s.Agente == DataPilot.Core.Agente.Claude ? "Claude" : "Codex";
        Pasta = TextoExibicao.NomePasta(s.Cwd);
        PastaCompleta = TextoExibicao.Limpar(s.Cwd);
        StatusTexto = RegrasExibicao.Texto(e.Status);
        Tempo = e.Tempo;
        CorFundo = Cores.Fundo(e.Status);
        CorTexto = Cores.Texto(e.Status);

        PodeRetomar = registrada is not null;
        Registro = registrada is null ? ""
            : registrada.Rotulo is null ? $"Repositório: {registrada.Pasta}"
            : $"Repositório: {registrada.Pasta} · {registrada.Rotulo}";
        Dica = $"{PastaCompleta}\nSessão: {TextoExibicao.Limpar(s.SessionId)}"
            + (registrada is null ? "" : $"\nRetomar abre em: {registrada.Pasta}");
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
