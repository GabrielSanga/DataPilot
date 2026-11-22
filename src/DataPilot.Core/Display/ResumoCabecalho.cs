using DataPilot.Core.Models;

namespace DataPilot.Core.Display;

/// <summary>
/// Resumo estruturado do cabeçalho: contagens por status e os textos com plural correto. A tela monta as partes
/// (alerta, "aguardando você", "executando") a partir daqui.
/// </summary>
public sealed record ResumoCabecalho(int PrecisaAtencao, int Aguardando, int Executando)
{
    public const string TextoNada = "Nada precisa de você agora";

    public string TextoPrecisaAtencao => PrecisaAtencao == 1 ? "1 precisa de atenção" : $"{PrecisaAtencao} precisam de atenção";
    public string TextoAguardando => $"{Aguardando} aguardando você";
    public string TextoExecutando => $"{Executando} executando";

    /// <summary>Status destacado no topo: atenção antes de aguardando; null quando nada precisa do usuário.</summary>
    public StatusExibido? StatusDestacado
        => PrecisaAtencao > 0 ? StatusExibido.PrecisaAtencao
         : Aguardando > 0 ? StatusExibido.Aguardando
         : null;

    /// <summary>Texto do alerta: "2 precisam de atenção", "1 aguardando você" ou "Nada precisa de você agora".</summary>
    public string TextoAlerta => StatusDestacado switch
    {
        StatusExibido.PrecisaAtencao => TextoPrecisaAtencao,
        StatusExibido.Aguardando => TextoAguardando,
        _ => TextoNada,
    };

    /// <summary>Contagens não nulas em uma frase, separadas por vírgula; "Nada precisa de você agora" se não houver.</summary>
    public string Texto
    {
        get
        {
            var partes = new List<string>();
            if (PrecisaAtencao > 0) partes.Add(TextoPrecisaAtencao);
            if (Aguardando > 0) partes.Add(TextoAguardando);
            if (Executando > 0) partes.Add(TextoExecutando);
            return partes.Count == 0 ? TextoNada : string.Join(", ", partes);
        }
    }
}
