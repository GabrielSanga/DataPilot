using DataPilot.Core.Models;

namespace DataPilot.Core.Display;

/// <summary>Regras de exibição, como funções puras. O relógio entra por <see cref="TimeProvider"/>.</summary>
public static class RegrasExibicao
{
    /// <summary>Tempo desde a última atualização; data no futuro conta como agora.</summary>
    public static TimeSpan Idade(Sessao s, TimeProvider relogio)
    {
        var idade = relogio.GetUtcNow() - s.Atualizado;
        return idade < TimeSpan.Zero ? TimeSpan.Zero : idade;
    }

    public static StatusExibido StatusExibido(Sessao s, TimeProvider relogio)
    {
        var idade = Idade(s, relogio);
        return s.Status switch
        {
            StatusGravado.Encerrada => Models.StatusExibido.Encerrada,
            StatusGravado.Ativa => idade > Constantes.InatividadeAtiva ? Models.StatusExibido.Inativa : Models.StatusExibido.Ativa,
            StatusGravado.Executando => idade > Constantes.InatividadeAtiva ? Models.StatusExibido.Inativa : Models.StatusExibido.Executando,
            StatusGravado.Aguardando => idade > Constantes.InatividadeEspera ? Models.StatusExibido.Inativa : Models.StatusExibido.Aguardando,
            StatusGravado.Atencao => idade > Constantes.InatividadeEspera ? Models.StatusExibido.Inativa : Models.StatusExibido.PrecisaAtencao,
            _ => Models.StatusExibido.Inativa,
        };
    }

    public static string Texto(StatusExibido status) => status switch
    {
        Models.StatusExibido.PrecisaAtencao => "precisa de atenção",
        Models.StatusExibido.Aguardando => "aguardando você",
        Models.StatusExibido.Executando => "executando",
        Models.StatusExibido.Ativa => "ativa",
        Models.StatusExibido.Inativa => "inativa",
        Models.StatusExibido.SemStatus => "sem status",
        _ => "encerrada",
    };

    /// <summary>Encerrada ou inativa há mais de 3 dias.</summary>
    public static bool EhAntiga(Sessao s, TimeProvider relogio)
    {
        var status = StatusExibido(s, relogio);
        return status is Models.StatusExibido.Encerrada or Models.StatusExibido.Inativa
            && Idade(s, relogio) > Constantes.IdadeSessaoAntiga;
    }

    /// <summary>Sessões visíveis, na ordem de urgência e, dentro do status, da mais recente para a mais antiga.</summary>
    public static IReadOnlyList<SessaoExibida> Montar(IEnumerable<Sessao> sessoes, bool mostrarAntigas, TimeProvider relogio)
    {
        var lista = new List<SessaoExibida>();
        foreach (var s in sessoes)
        {
            if (!mostrarAntigas && EhAntiga(s, relogio)) continue;
            lista.Add(new SessaoExibida(s, StatusExibido(s, relogio), TempoDecorrido(Idade(s, relogio))));
        }
        lista.Sort((a, b) =>
        {
            var c = a.Status.CompareTo(b.Status);
            if (c != 0) return c;
            c = b.Sessao.Atualizado.CompareTo(a.Sessao.Atualizado);
            return c != 0 ? c : string.CompareOrdinal(a.Sessao.SessionId, b.Sessao.SessionId);
        });
        return lista;
    }

    /// <summary>Contagens do cabeçalho. Recebe só as sessões visíveis; "sem status" não entra.</summary>
    public static ResumoCabecalho Resumo(IEnumerable<SessaoExibida> visiveis)
    {
        int atencao = 0, aguardando = 0, executando = 0;
        foreach (var v in visiveis)
        {
            switch (v.Status)
            {
                case Models.StatusExibido.PrecisaAtencao: atencao++; break;
                case Models.StatusExibido.Aguardando: aguardando++; break;
                case Models.StatusExibido.Executando: executando++; break;
            }
        }
        return new ResumoCabecalho(atencao, aguardando, executando);
    }

    public static string TempoDecorrido(TimeSpan idade)
    {
        if (idade < TimeSpan.FromMinutes(1)) return "agora";
        if (idade < TimeSpan.FromHours(1)) return $"{(int)idade.TotalMinutes} min";
        if (idade < TimeSpan.FromDays(1)) return $"{(int)idade.TotalHours} h";
        return $"{(int)idade.TotalDays} d";
    }
}

public sealed record SessaoExibida(Sessao Sessao, StatusExibido Status, string Tempo);

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
