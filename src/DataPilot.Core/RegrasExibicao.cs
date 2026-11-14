namespace DataPilot.Core;

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
            StatusGravado.Encerrada => Core.StatusExibido.Encerrada,
            StatusGravado.Ativa => idade > Constantes.InatividadeAtiva ? Core.StatusExibido.Inativa : Core.StatusExibido.Ativa,
            StatusGravado.Executando => idade > Constantes.InatividadeAtiva ? Core.StatusExibido.Inativa : Core.StatusExibido.Executando,
            StatusGravado.Aguardando => idade > Constantes.InatividadeEspera ? Core.StatusExibido.Inativa : Core.StatusExibido.Aguardando,
            StatusGravado.Atencao => idade > Constantes.InatividadeEspera ? Core.StatusExibido.Inativa : Core.StatusExibido.PrecisaAtencao,
            _ => Core.StatusExibido.Inativa,
        };
    }

    public static string Texto(StatusExibido status) => status switch
    {
        Core.StatusExibido.PrecisaAtencao => "precisa de atenção",
        Core.StatusExibido.Aguardando => "aguardando você",
        Core.StatusExibido.Executando => "executando",
        Core.StatusExibido.Ativa => "ativa",
        Core.StatusExibido.Inativa => "inativa",
        _ => "encerrada",
    };

    /// <summary>Encerrada ou inativa há mais de 3 dias.</summary>
    public static bool EhAntiga(Sessao s, TimeProvider relogio)
    {
        var status = StatusExibido(s, relogio);
        return status is Core.StatusExibido.Encerrada or Core.StatusExibido.Inativa
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

    /// <summary>Resumo do cabeçalho, omitindo contagens zero. Recebe só as sessões visíveis.</summary>
    public static string Resumo(IEnumerable<SessaoExibida> visiveis)
    {
        int atencao = 0, aguardando = 0, executando = 0;
        foreach (var v in visiveis)
        {
            switch (v.Status)
            {
                case Core.StatusExibido.PrecisaAtencao: atencao++; break;
                case Core.StatusExibido.Aguardando: aguardando++; break;
                case Core.StatusExibido.Executando: executando++; break;
            }
        }
        var partes = new List<string>();
        if (atencao > 0) partes.Add($"{atencao} precisa de atenção");
        if (aguardando > 0) partes.Add($"{aguardando} aguardando você");
        if (executando > 0) partes.Add($"{executando} executando");
        return partes.Count == 0 ? "Nada precisa de você" : string.Join(" · ", partes);
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
