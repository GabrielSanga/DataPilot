using System.Text.Json.Nodes;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Persistence;

public sealed partial class DadosPainel
{
    /// <summary>Grava a seção <c>configuracoes</c>, preservando o resto do arquivo. O usuário é validado mesmo com o SSH desligado.</summary>
    public ResultadoOperacao SalvarConfiguracoes(Configuracoes configuracoes)
    {
        if (!ValidadorEntrada.UsuarioSshValido(configuracoes.UsuarioSsh))
            return ResultadoOperacao.Falha(ValidadorEntrada.MensagemUsuarioSsh);

        return Alterar(raiz =>
        {
            raiz["configuracoes"] = new JsonObject
            {
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
        var usarSsh = c["usarSsh"] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : padrao.UsarSsh;
        var usuario = Texto(c, "usuarioSsh");
        return new Configuracoes(usarSsh, ValidadorEntrada.UsuarioSshValido(usuario) ? usuario! : padrao.UsuarioSsh);
    }
}
