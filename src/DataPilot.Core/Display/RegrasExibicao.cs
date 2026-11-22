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
        lista.Sort((a, b) => ComparaSessoes((a.Status, a.Sessao.Atualizado, a.Sessao.SessionId), (b.Status, b.Sessao.Atualizado, b.Sessao.SessionId)));
        return lista;
    }

    /// <summary>Ordem das sessões: status mais urgente, depois a data mais recente e, por fim, o id.</summary>
    public static int ComparaSessoes(
        (StatusExibido Status, DateTimeOffset Data, string Id) a, (StatusExibido Status, DateTimeOffset Data, string Id) b)
    {
        var c = a.Status.CompareTo(b.Status);
        if (c != 0) return c;
        c = b.Data.CompareTo(a.Data);
        return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
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

    /// <summary>"nenhuma sessão", "1 sessão" ou "3 sessões".</summary>
    public static string Quantidade(int n, string singular, string plural, string nenhum)
        => n switch { 0 => $"{nenhum} {singular}", 1 => $"1 {singular}", _ => $"{n} {plural}" };

    /// <summary>Avisa antes de retomar ou arquivar quando a sessão não está encerrada, inativa nem sem status.</summary>
    public static bool ExigeConfirmacao(StatusExibido status)
        => status is not (Models.StatusExibido.Encerrada or Models.StatusExibido.Inativa or Models.StatusExibido.SemStatus);

    public static string TempoDecorrido(TimeSpan idade)
    {
        if (idade < TimeSpan.FromMinutes(1)) return "agora";
        if (idade < TimeSpan.FromHours(1)) return $"{(int)idade.TotalMinutes} min";
        if (idade < TimeSpan.FromDays(1)) return $"{(int)idade.TotalHours} h";
        return $"{(int)idade.TotalDays} d";
    }
}

public sealed record SessaoExibida(Sessao Sessao, StatusExibido Status, string Tempo);
