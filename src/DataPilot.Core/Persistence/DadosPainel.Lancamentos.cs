using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Persistence;

public sealed partial class DadosPainel
{
    /// <summary>
    /// Registra o lançamento pendente de uma sessão nova do Codex no repositório do card. O identificador do
    /// lançamento é gerado por quem chama. Lançamentos do repositório com mais de <see cref="RegrasDemanda.ValidadeLancamento"/>
    /// são removidos na mesma gravação.
    /// </summary>
    public ResultadoOperacao RegistrarLancamento(string demandaId, string pasta, Agente agente, string lancamentoId,
        string? rotulo, DateTimeOffset criadoEm, Execucao execucao = Execucao.Windows)
    {
        if (!ValidadorEntrada.RepositorioValido(pasta)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRepositorio);
        if (!ValidadorEntrada.IdentificadorValido(lancamentoId)) return ResultadoOperacao.Falha("Identificador de lançamento inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRotulo);

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");
            if (LancamentosComId(raiz, lancamentoId).Any()) return ResultadoOperacao.Falha("Este lançamento já está registrado.");

            var lista = repo["lancamentos"] as JsonArray;
            if (lista is null) repo["lancamentos"] = lista = new JsonArray();
            for (var i = lista.Count - 1; i >= 0; i--)
            {
                if (lista[i] is JsonObject l && LancamentoVencido(l, criadoEm)) lista.RemoveAt(i);
            }

            var novo = new JsonObject
            {
                ["id"] = lancamentoId,
                ["agente"] = Agentes.NoArquivo(agente),
                ["execucao"] = Execucoes.NoArquivo(execucao),
            };
            if (rotulo is not null) novo["rotulo"] = rotulo;
            novo["criadoEm"] = criadoEm.ToString("o", CultureInfo.InvariantCulture);
            lista.Add(novo);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>
    /// Troca a data de um lançamento pendente, quando o usuário abre o agente de novo pela linha dele: a validade
    /// recomeça e só uma sessão criada depois disso corresponde a ele. Se o lançamento não existir mais, nada é gravado.
    /// </summary>
    public ResultadoOperacao RenovarLancamento(string lancamentoId, DateTimeOffset criadoEm)
    {
        return Alterar(raiz =>
        {
            var lancamento = LancamentosComId(raiz, lancamentoId).FirstOrDefault();
            if (lancamento is null) return ResultadoOperacao.Falha("O lançamento não existe mais.");
            lancamento["criadoEm"] = criadoEm.ToString("o", CultureInfo.InvariantCulture);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Desfaz o registro do lançamento quando o terminal não abre.</summary>
    public ResultadoOperacao DesfazerLancamento(string lancamentoId)
    {
        return Alterar(raiz =>
        {
            var removido = false;
            foreach (var repo in Todos(raiz))
            {
                if (repo["lancamentos"] is not JsonArray lista) continue;
                for (var i = lista.Count - 1; i >= 0; i--)
                {
                    if (lista[i] is JsonObject l && LancamentoIgual(l, lancamentoId))
                    {
                        lista.RemoveAt(i);
                        removido = true;
                    }
                }
            }
            return removido ? ResultadoOperacao.Sucesso : ResultadoOperacao.Falha("Lançamento não registrado.");
        });
    }

    /// <summary>
    /// Aplica, numa única gravação, o vínculo de uma sessão a um lançamento pendente: confere que o lançamento ainda
    /// existe (com o mesmo agente) e que o identificador não está registrado, registra a sessão com o rótulo do
    /// lançamento e <c>vinculadaEm</c>, e remove o lançamento. Demanda arquivada não recebe vínculo.
    /// </summary>
    public ResultadoOperacao VincularPorLancamento(PedidoVinculo pedido, string demandaId, string pasta,
        string lancamentoId, DateTimeOffset vinculadaEm)
    {
        if (!ValidadorEntrada.RepositorioValido(pasta)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRepositorio);
        if (!ValidadorEntrada.IdentificadorValido(pedido.SessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (EstaArquivada(demanda))
                return ResultadoOperacao.Falha($"O trabalho {demandaId} está arquivado.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");

            if (repo["lancamentos"] is not JsonArray lista
                || lista.OfType<JsonObject>().FirstOrDefault(l => LancamentoIgual(l, lancamentoId)) is not { } lancamento)
                return ResultadoOperacao.Falha("O lançamento não existe mais.");
            if (Texto(lancamento, "agente") != Agentes.NoArquivo(pedido.Agente))
                return ResultadoOperacao.Falha("O agente da sessão não é o do lançamento.");
            if (SessoesComId(raiz, pedido.SessionId).Any())
                return ResultadoOperacao.Falha("Esta sessão já está registrada.");

            if (!TentarLerExecucao(lancamento, out var execucao))
                return ResultadoOperacao.Falha("Ambiente de execução do lançamento inválido.");

            var rotulo = Texto(lancamento, "rotulo");
            if (!ValidadorEntrada.RotuloValido(rotulo)) rotulo = null;

            AdicionarSessao(repo, pedido.Agente, pedido.SessionId, rotulo, vinculadaEm, vinculadaEm, execucao);
            lista.Remove(lancamento);
            return ResultadoOperacao.Sucesso;
        });
    }

    private static bool LancamentoVencido(JsonObject lancamento, DateTimeOffset agora)
        => DateTimeOffset.TryParse(Texto(lancamento, "criadoEm"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var criado)
           && agora - criado > RegrasDemanda.ValidadeLancamento;

    private static bool LancamentoIgual(JsonObject lancamento, string id)
        => string.Equals(Texto(lancamento, "id"), id, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<JsonObject> Todos(JsonObject raiz)
        => raiz["demandas"]!.AsArray().OfType<JsonObject>()
            .Select(d => d["repositorios"] as JsonArray).OfType<JsonArray>()
            .SelectMany(rs => rs.OfType<JsonObject>());

    private static IEnumerable<JsonObject> LancamentosComId(JsonObject raiz, string id)
        => Todos(raiz).Select(r => r["lancamentos"] as JsonArray).OfType<JsonArray>()
            .SelectMany(ls => ls.OfType<JsonObject>())
            .Where(l => LancamentoIgual(l, id));
}
