using DataPilot.Core.Models;

namespace DataPilot.Wpf.Services;

/// <summary>
/// Perguntas, avisos e diálogos ao usuário, atrás de uma interface para a lógica do view model não depender de janelas.
/// Nos diálogos de trabalho, repositório e vínculo, <c>aplicar</c> grava e devolve a mensagem de recusa (ou null); o
/// diálogo só fecha quando a gravação dá certo.
/// </summary>
public interface IInteracao
{
    /// <summary>
    /// Confirmação do próprio painel: <c>titulo</c> diz o que vai acontecer, <c>mensagem</c> explica e o botão de
    /// confirmação leva o nome da ação. "Cancelar" é o botão padrão, então Enter não confirma.
    /// </summary>
    bool Confirmar(string titulo, string mensagem, string acao);

    /// <summary>Criar (<c>tituloAtual</c> nulo) ou editar um trabalho; só o título.</summary>
    bool DialogoTrabalho(string? tituloAtual, Func<string, string?> aplicar);

    /// <summary>
    /// "Vincular repositório" (<c>pastaFixa</c> nulo: abre a escolha de pasta do Windows em <c>raiz</c>) ou "Trocar change"
    /// (pasta já conhecida). <c>aplicar</c> recebe o nome da pasta e a change escolhida (ou nenhuma).
    /// </summary>
    bool DialogoRepositorio(string raiz, string? pastaFixa, string? changeAtual, Func<string, string?, string?> aplicar);

    /// <summary>Diálogo de vínculo. <c>aplicar</c> devolve a mensagem de recusa; "" mantém o diálogo aberto sem mensagem (o usuário não confirmou o aviso).</summary>
    bool DialogoVincularSessao(string descricaoSessao, IReadOnlyList<OpcaoDemandaVinculo> demandas, Func<EscolhaVinculo, string?> aplicar);

    /// <summary>
    /// Diálogo de configurações. <c>semSalvar</c> não nulo é o motivo de não ser possível gravar (o botão fica
    /// desabilitado); <c>aplicar</c> grava e devolve a mensagem de recusa (ou null).
    /// </summary>
    bool DialogoConfiguracoes(string pastaStatus, Configuracoes atual, string? semSalvar, Func<Configuracoes, string?> aplicar);

    NovaSessaoEscolha? PedirNovaSessao();

    /// <summary>Escolha de pasta do Windows, começando em <c>raiz</c>. Nulo quando o usuário cancela.</summary>
    string? EscolherPasta(string raiz);

    /// <summary>Escolha do repositório entre as pastas diretas da raiz, pré-selecionando <c>sugerida</c>. Nulo quando cancela.</summary>
    string? EscolherRepositorio(IReadOnlyList<string> pastas, string? sugerida);
}

public sealed record EscolhaVinculo(string DemandaId, string Repositorio, string? Rotulo);

public sealed record NovaSessaoEscolha(Agente Agente, string? Rotulo);

/// <summary>Trabalho oferecido no diálogo de vínculo, com os repositórios dele e o que coincide com a pasta da sessão (se houver).</summary>
public sealed record OpcaoDemandaVinculo(string Id, string Texto, IReadOnlyList<string> Repositorios, string? RepositorioSugerido);
