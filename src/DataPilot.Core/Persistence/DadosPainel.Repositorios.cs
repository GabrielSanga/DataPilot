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
    /// <summary>Vincula o repositório à demanda; <paramref name="openspecChange"/> é a change escolhida (ou nenhuma).</summary>
    public ResultadoOperacao VincularRepositorio(string demandaId, string pasta, string? openspecChange = null)
    {
        if (!ValidadorEntrada.RepositorioValido(pasta))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRepositorio);
        if (!string.IsNullOrEmpty(openspecChange) && !ValidadorEntrada.OpenspecChangeValido(openspecChange))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemChange);

        return Alterar(raiz =>
        {
            var demanda = EncontrarOuCriarImplicita(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (EncontrarRepositorio(demanda, pasta) is not null)
                return ResultadoOperacao.Falha($"O repositório {pasta} já está vinculado a este trabalho.");

            var novo = NovoRepositorio(pasta);
            if (!string.IsNullOrEmpty(openspecChange)) novo["openspecChange"] = openspecChange;
            Repositorios(demanda).Add(novo);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>
    /// Grava a change do repositório (ou a remove, com <c>null</c>). Só o repositório é gravado: o campo antigo da
    /// demanda é preservado, mas o repositório passa a ter a própria change (vazia, quando "Nenhuma").
    /// </summary>
    public ResultadoOperacao DefinirChange(string demandaId, string pasta, string? openspecChange)
    {
        if (!string.IsNullOrEmpty(openspecChange) && !ValidadorEntrada.OpenspecChangeValido(openspecChange))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemChange);

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");

            if (string.IsNullOrEmpty(openspecChange))
            {
                repo.Remove("openspecChange");
                // "Nenhuma" precisa vencer a change herdada da demanda: o texto vazio no repositório vale como "nenhuma".
                if (demanda["openspecChange"] is not null) repo["openspecChange"] = "";
            }
            else repo["openspecChange"] = openspecChange;
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Remove o repositório e as sessões registradas nele, só nessa demanda. A confirmação é de quem chama.</summary>
    public ResultadoOperacao RemoverRepositorio(string demandaId, string pasta)
    {
        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (demanda["repositorios"] is not JsonArray repos)
                return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");

            var removido = false;
            for (var i = repos.Count - 1; i >= 0; i--)
            {
                if (repos[i] is JsonObject r && PastaIgual(r, pasta))
                {
                    repos.RemoveAt(i);
                    removido = true;
                }
            }
            return removido ? ResultadoOperacao.Sucesso
                : ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");
        });
    }

}
