using System.Text.Json.Nodes;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Persistence;

public sealed partial class DadosPainel
{
    /// <summary>Grava a seção <c>configuracoes</c> só com o container, preservando o resto do arquivo.</summary>
    public ResultadoOperacao SalvarConfiguracoes(Configuracoes configuracoes)
    {
        if (!ValidadorEntrada.ContainerValido(configuracoes.Container))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemContainer);

        return Alterar(raiz =>
        {
            raiz["configuracoes"] = new JsonObject { ["container"] = configuracoes.Container };
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Container ausente ou inválido vale o padrão.</summary>
    private static Configuracoes ExtrairConfiguracoes(JsonObject raiz)
    {
        var container = raiz["configuracoes"] is JsonObject c ? Texto(c, "container") : null;
        return ValidadorEntrada.ContainerValido(container) ? new Configuracoes(container!) : Configuracoes.Padrao;
    }
}
