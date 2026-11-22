using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Sessions;

/// <summary>Operações de abrir e retomar sessões: registrar, disparar e desfazer o registro quando o disparo falha.</summary>
public sealed class AberturaSessao(DadosPainel dados, IDisparador disparador, TimeProvider relogio, Func<Guid>? gerarGuid = null)
{
    private readonly Func<Guid> _gerarGuid = gerarGuid ?? Guid.NewGuid;

    public ResultadoOperacao Nova(string demandaId, string pasta, Agente agente, string? rotulo)
    {
        rotulo = string.IsNullOrWhiteSpace(rotulo) ? null : rotulo.Trim();
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRotulo);

        var estado = dados.Carregar();
        if (!estado.Disponivel) return ResultadoOperacao.Falha($"Dados do painel indisponíveis: {dados.Caminho}");
        var demanda = estado.Demandas.FirstOrDefault(d => string.Equals(d.Id, demandaId, StringComparison.OrdinalIgnoreCase));
        if (demanda is null) return ResultadoOperacao.Falha("Trabalho não encontrado.");
        if (demanda.Arquivada) return ResultadoOperacao.Falha("O trabalho está arquivado.");
        var repo = demanda.Repositorios.FirstOrDefault(r => string.Equals(r.Pasta, pasta, StringComparison.OrdinalIgnoreCase));
        if (repo is null) return ResultadoOperacao.Falha("Repositório não vinculado a este trabalho.");

        // O GUID da sessão só existe para o Claude; o do Codex aparece depois, pelo hook. Para o Codex, o painel
        // registra um lançamento pendente, que vincula a sessão quando ela surgir na pasta de status.
        var claude = agente == Agente.Claude;
        var id = _gerarGuid().ToString("D");

        // Registrar antes de abrir garante que toda sessão aberta possa ser retomada.
        return Executar(
            () => MontadorComando.Novo(demanda.Id, demanda.Titulo, repo, agente, claude ? id : null, rotulo),
            () => claude
                ? dados.RegistrarSessao(demanda.Id, repo.Pasta, agente, id, rotulo, relogio.GetUtcNow())
                : dados.RegistrarLancamento(demanda.Id, repo.Pasta, agente, id, rotulo, relogio.GetUtcNow()),
            () => { _ = claude ? dados.DesfazerRegistro(id) : dados.DesfazerLancamento(id); });
    }

    /// <summary>
    /// Fluxo comum: montar o comando (erro vira falha), gravar, disparar e, se o disparo falhar, desfazer a gravação.
    /// </summary>
    private ResultadoOperacao Executar(Func<ComandoTerminal> montar, Func<ResultadoOperacao> gravar, Action desfazer)
    {
        ComandoTerminal comando;
        try { comando = montar(); }
        catch (EntradaInvalidaException e) { return ResultadoOperacao.Falha(e.Message); }

        var gravacao = gravar();
        if (!gravacao.Ok) return gravacao;

        try
        {
            disparador.Iniciar(comando);
            return ResultadoOperacao.Sucesso;
        }
        catch (FalhaDisparoException e)
        {
            desfazer();
            return ResultadoOperacao.Falha(e.Message);
        }
    }
    /// <summary>
    /// Abre de novo o agente de um lançamento pendente (aba fechada antes da primeira mensagem). Como ainda não existe
    /// conversa, é o mesmo comando de uma sessão nova, no repositório e com o rótulo do lançamento. Reaproveita o
    /// lançamento: a data passa a ser a deste clique e volta à anterior se o terminal não abrir.
    /// </summary>
    public ResultadoOperacao ReabrirLancamento(string lancamentoId)
    {
        var estado = dados.Carregar();
        if (!estado.Disponivel) return ResultadoOperacao.Falha($"Dados do painel indisponíveis: {dados.Caminho}");

        var agora = relogio.GetUtcNow();
        var achado = estado.Demandas
            .SelectMany(d => d.Repositorios.SelectMany(r => r.Lancamentos.Select(l => (Demanda: d, Repo: r, Lancamento: l))))
            .Where(x => string.Equals(x.Lancamento.Id, lancamentoId, StringComparison.OrdinalIgnoreCase)
                && RegrasDemanda.LancamentoValido(x.Demanda, x.Lancamento, agora))
            .Select(x => ((Demanda Demanda, RepositorioCadastrado Repo, LancamentoPendente Lancamento)?)x)
            .FirstOrDefault();
        if (achado is null) return ResultadoOperacao.Falha("Este lançamento não existe mais.");
        var (demanda, repo, lancamento) = achado.Value;

        return Executar(
            () => MontadorComando.Novo(demanda.Id, demanda.Titulo, repo, lancamento.Agente, null, lancamento.Rotulo),
            () => dados.RenovarLancamento(lancamento.Id, agora),
            () => dados.RenovarLancamento(lancamento.Id, lancamento.CriadoEm));
    }

    /// <summary>
    /// "Nova sessão" em "Sessões": vincula a pasta à <c>sem-demanda</c>, se ainda não estiver, e segue o fluxo de
    /// sempre (Claude registrado na hora, Codex com lançamento pendente).
    /// </summary>
    public ResultadoOperacao NovaEmSessoes(string pasta, Agente agente, string? rotulo)
    {
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemPasta);
        var estado = dados.Carregar();
        if (!estado.Disponivel) return ResultadoOperacao.Falha($"Dados do painel indisponíveis: {dados.Caminho}");

        var implicita = estado.Demandas.FirstOrDefault(d => RegrasDemanda.EhImplicita(d.Id));
        var jaVinculado = implicita?.Repositorios.Any(r => string.Equals(r.Pasta, pasta, StringComparison.OrdinalIgnoreCase)) == true;
        if (!jaVinculado)
        {
            var vinculo = dados.VincularRepositorio(RegrasDemanda.IdDemandaImplicita, pasta);
            if (!vinculo.Ok) return vinculo;
        }
        return Nova(RegrasDemanda.IdDemandaImplicita, pasta, agente, rotulo);
    }

    /// <summary>
    /// "Retomar" numa sessão que o painel não registrou: registra-a em "Sessões", no repositório escolhido, e abre a
    /// conversa. Se o terminal não abre, o registro é desfeito.
    /// </summary>
    public ResultadoOperacao RetomarNaoRegistrada(PedidoVinculo pedido, string pasta)
    {
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemPasta);
        var agora = relogio.GetUtcNow();
        var sessao = new SessaoRegistrada(pedido.Agente, pedido.SessionId, pasta, null, agora,
            RegrasDemanda.IdDemandaImplicita, RegrasDemanda.TituloDemandaImplicita);

        return Executar(
            () => MontadorComando.Retomar(sessao),
            () => dados.RegistrarRetomada(pedido, pasta, agora),
            () => dados.DesfazerRegistro(pedido.SessionId));
    }

    public ResultadoOperacao Retomar(SessaoRegistrada sessao)
        => Executar(() => MontadorComando.Retomar(sessao), () => ResultadoOperacao.Sucesso, () => { });
}
