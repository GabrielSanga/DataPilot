namespace DataPilot.Core;

/// <summary>
/// Um vínculo a aplicar: o pedido (só agente e identificador, vindos do arquivo de status) e o lançamento pendente
/// que o autoriza. Demanda, repositório e rótulo vêm do lançamento, que está no arquivo de dados.
/// </summary>
public sealed record VinculoPorLancamento(PedidoVinculo Pedido, string DemandaId, string Pasta, LancamentoPendente Lancamento);

/// <summary>
/// Correspondência entre sessões não registradas e lançamentos pendentes, como função pura. Compara o agente, o
/// último segmento da pasta de trabalho e a data de criação do arquivo de status com o lançamento; nunca acessa
/// a pasta indicada nem grava nada.
/// </summary>
public static class VinculoAutomatico
{
    /// <summary>
    /// Calcula e aplica os vínculos de um retrato da pasta de status. Cada vínculo é uma gravação própria, conferida
    /// dentro dela: se outra instância do painel já o aplicou, a gravação é recusada. Falhas são ignoradas, e a sessão
    /// continua em "Não vinculadas" até a leitura seguinte. Devolve quantos vínculos foram gravados.
    /// </summary>
    public static int Aplicar(DadosPainel dados, EstadoDados estado, IReadOnlyList<Sessao> sessoes, TimeProvider relogio)
    {
        if (!estado.Disponivel) return 0;
        var gravados = 0;
        foreach (var v in Calcular(estado.Demandas, sessoes, relogio.GetUtcNow()))
        {
            try
            {
                if (dados.VincularPorLancamento(v.Pedido, v.DemandaId, v.Pasta, v.Lancamento.Id, relogio.GetUtcNow()).Ok)
                    gravados++;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Ignorada: a próxima leitura tenta de novo.
            }
        }
        return gravados;
    }

    public static IReadOnlyList<VinculoPorLancamento> Calcular(
        IReadOnlyList<Demanda> demandas, IReadOnlyList<Sessao> sessoes, DateTimeOffset agora)
    {
        var registradas = new HashSet<string>(
            demandas.SelectMany(d => d.Repositorios).SelectMany(r => r.Sessoes).Select(s => s.SessionId),
            StringComparer.OrdinalIgnoreCase);

        // Lançamentos de demandas arquivadas ou vencidos não vinculam.
        var disponiveis = demandas
            .Where(d => !d.Arquivada)
            .SelectMany(d => d.Repositorios.SelectMany(r => r.Lancamentos.Select(l => (Demanda: d.Id, r.Pasta, Lancamento: l))))
            .Where(x => agora - x.Lancamento.CriadoEm <= DadosPainel.ValidadeLancamento)
            .ToList();
        if (disponiveis.Count == 0) return [];

        var candidatas = sessoes
            .Where(s => s.ArquivoCriadoEm is not null && !registradas.Contains(s.SessionId))
            .OrderBy(s => s.ArquivoCriadoEm)
            .ThenBy(s => s.SessionId, StringComparer.Ordinal);

        var resultado = new List<VinculoPorLancamento>();
        foreach (var sessao in candidatas)
        {
            var pedido = PedidoVinculo.Criar(sessao);
            if (pedido is null) continue;

            var escolhido = disponiveis
                .Where(x => x.Lancamento.Agente == sessao.Agente
                    && RegrasVinculo.PastaCoincide(sessao, x.Pasta)
                    && x.Lancamento.CriadoEm <= sessao.ArquivoCriadoEm)
                .OrderByDescending(x => x.Lancamento.CriadoEm)
                .ThenBy(x => x.Lancamento.Id, StringComparer.Ordinal)
                .Select(x => ((string Demanda, string Pasta, LancamentoPendente Lancamento)?)x)
                .FirstOrDefault();
            if (escolhido is null) continue;

            var (demanda, pasta, lancamento) = escolhido.Value;
            disponiveis.RemoveAll(x => x.Lancamento.Id == lancamento.Id);
            resultado.Add(new VinculoPorLancamento(pedido, demanda, pasta, lancamento));
        }
        return resultado;
    }
}
