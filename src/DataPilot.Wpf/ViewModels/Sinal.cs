using DataPilot.Core.Models;

namespace DataPilot.Wpf.ViewModels;

/// <summary>
/// Nível visual de um status. Só "precisa de atenção" e "aguardando você" acendem (faixa e texto em destaque);
/// os demais ficam em tom baixo. O tema transforma o nível em cor.
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
