namespace DataPilot.Core;

/// <summary>Operações de abrir e retomar sessões: registrar, disparar e desfazer o registro quando o disparo falha.</summary>
public sealed class AberturaSessao(DadosPainel dados, IDisparador disparador, TimeProvider relogio, Func<Guid>? gerarGuid = null)
{
    private readonly Func<Guid> _gerarGuid = gerarGuid ?? Guid.NewGuid;

    public ResultadoOperacao Nova(string pasta, Agente agente, string? rotulo)
    {
        rotulo = string.IsNullOrWhiteSpace(rotulo) ? null : rotulo.Trim();
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo))
            return ResultadoOperacao.Falha("Rótulo inválido: use até 40 caracteres, sem \";\" nem caracteres de controle.");

        var estado = dados.Carregar();
        if (!estado.Disponivel) return ResultadoOperacao.Falha($"Dados do painel indisponíveis: {dados.Caminho}");
        var repo = estado.Repositorios.FirstOrDefault(r => string.Equals(r.Pasta, pasta, StringComparison.OrdinalIgnoreCase));
        if (repo is null) return ResultadoOperacao.Falha("Repositório não cadastrado.");

        // O GUID só existe para o Claude; o do Codex aparece depois, pelo hook.
        var id = agente == Agente.Claude ? _gerarGuid().ToString("D") : null;

        ComandoTerminal comando;
        try { comando = MontadorComando.Novo(repo, agente, id, rotulo); }
        catch (EntradaInvalidaException e) { return ResultadoOperacao.Falha(e.Message); }

        // Registrar antes de abrir garante que toda sessão aberta possa ser retomada.
        if (id is not null)
        {
            var registro = dados.RegistrarSessao(repo.Pasta, agente, id, rotulo, relogio.GetUtcNow());
            if (!registro.Ok) return registro;
        }

        try
        {
            disparador.Iniciar(comando);
            return ResultadoOperacao.Sucesso;
        }
        catch (FalhaDisparoException e)
        {
            if (id is not null) dados.DesfazerRegistro(id);
            return ResultadoOperacao.Falha(e.Message);
        }
    }

    public ResultadoOperacao Retomar(SessaoRegistrada sessao)
    {
        ComandoTerminal comando;
        try { comando = MontadorComando.Retomar(sessao); }
        catch (EntradaInvalidaException e) { return ResultadoOperacao.Falha(e.Message); }

        try
        {
            disparador.Iniciar(comando);
            return ResultadoOperacao.Sucesso;
        }
        catch (FalhaDisparoException e)
        {
            return ResultadoOperacao.Falha(e.Message);
        }
    }
}
