using System.Text.Json.Nodes;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Persistence;

public sealed partial class DadosPainel
{
    /// <summary>Grava a seção <c>configuracoes</c>, preservando o resto do arquivo. O container e o usuário são validados mesmo sem uso.</summary>
    public ResultadoOperacao SalvarConfiguracoes(Configuracoes configuracoes)
    {
        if (!ValidadorEntrada.ContainerValido(configuracoes.Container))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemContainer);
        if (!ValidadorEntrada.UsuarioSshValido(configuracoes.UsuarioSsh))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemUsuarioSsh);

        return Alterar(raiz =>
        {
            raiz["configuracoes"] = new JsonObject
            {
                ["execucao"] = Execucoes.NoArquivo(configuracoes.Execucao),
                ["container"] = configuracoes.Container,
                ["usarSsh"] = configuracoes.UsarSsh,
                ["usuarioSsh"] = configuracoes.UsuarioSsh,
            };
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Campo ausente ou inválido vale o padrão.</summary>
    private static Configuracoes ExtrairConfiguracoes(JsonObject raiz)
    {
        var padrao = Configuracoes.Padrao;
        if (raiz["configuracoes"] is not JsonObject c) return padrao;
        var execucao = Execucoes.TentarLer(Texto(c, "execucao"), out var e) ? e : padrao.Execucao;
        var container = Texto(c, "container");
        var usarSsh = c["usarSsh"] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : padrao.UsarSsh;
        var usuario = Texto(c, "usuarioSsh");
        return new Configuracoes(execucao, ValidadorEntrada.ContainerValido(container) ? container! : padrao.Container,
            usarSsh, ValidadorEntrada.UsuarioSshValido(usuario) ? usuario! : padrao.UsuarioSsh);
    }
}
