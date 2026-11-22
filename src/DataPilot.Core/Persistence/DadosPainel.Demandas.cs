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
    public ResultadoOperacao CriarDemanda(string titulo, DateTimeOffset criadaEm, Func<Guid>? gerarGuid = null) => Criar(titulo, criadaEm, gerarGuid ?? Guid.NewGuid, idFixo: null);

    internal ResultadoOperacao CriarDemandaComId(string id, string titulo, DateTimeOffset criadaEm)
    {
        if (!ValidadorEntrada.IdDemandaValido(id))
            return ResultadoOperacao.Falha("Identificador inválido.");
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha($"O identificador {RegrasDemanda.IdDemandaImplicita} é reservado.");
        return Criar(titulo, criadaEm, Guid.NewGuid, idFixo: id);
    }

    public ResultadoOperacao EditarDemanda(string id, string titulo)
    {
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha("\"Sessões\" não pode ser editada.");
        var invalido = ValidarTitulo(titulo);
        if (invalido is not null) return invalido;

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, id);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {id} não existe.");
            demanda["titulo"] = titulo;
            return ResultadoOperacao.Sucesso;
        });
    }

    public ResultadoOperacao ArquivarDemanda(string id)
    {
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha("\"Sessões\" não pode ser arquivada.");
        return DefinirArquivada(id, true);
    }

    public ResultadoOperacao DesarquivarDemanda(string id) => DefinirArquivada(id, false);


    private ResultadoOperacao Criar(string titulo, DateTimeOffset criadaEm, Func<Guid> guid, string? idFixo)
    {
        var invalido = ValidarTitulo(titulo);
        if (invalido is not null) return invalido;

        string? criado = null;
        var r = Alterar(raiz =>
        {
            var existentes = raiz["demandas"]!.AsArray().OfType<JsonObject>()
                .Select(d => Texto(d, "id")).OfType<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            existentes.Add(RegrasDemanda.IdDemandaImplicita);

            string? id = null;
            if (idFixo is not null)
            {
                if (existentes.Contains(idFixo))
                    return ResultadoOperacao.Falha($"Já existe um trabalho com o identificador {idFixo}.");
                id = idFixo;
            }
            for (var tentativa = 0; tentativa < 100 && id is null; tentativa++)
            {
                var candidato = "t" + guid().ToString("N")[..8];
                if (!existentes.Contains(candidato)) id = candidato;
            }
            if (id is null) return ResultadoOperacao.Falha("Não foi possível gerar um identificador para o trabalho.");

            var nova = new JsonObject { ["id"] = id, ["titulo"] = titulo };
            nova["criadaEm"] = criadaEm.ToString("o", CultureInfo.InvariantCulture);
            nova["arquivada"] = false;
            nova["repositorios"] = new JsonArray();
            raiz["demandas"]!.AsArray().Add(nova);
            criado = id;
            return ResultadoOperacao.Sucesso;
        });
        return r.Ok ? r with { Id = criado } : r;
    }

    private ResultadoOperacao DefinirArquivada(string id, bool arquivada)
        => Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, id);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {id} não existe.");
            demanda["arquivada"] = arquivada;
            return ResultadoOperacao.Sucesso;
        });

    private static ResultadoOperacao? ValidarTitulo(string titulo)
        => ValidadorEntrada.TituloValido(titulo)
            ? null
            : ResultadoOperacao.Falha(ValidadorEntrada.MensagemTitulo);

}
