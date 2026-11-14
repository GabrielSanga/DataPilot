using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DataPilot.Core;

namespace DataPilot.Wpf;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MonitorPasta _monitor;
    private readonly TimeProvider _relogio;
    private readonly Dispatcher _dispatcher;
    private readonly DadosPainel _dados;
    private readonly AberturaSessao _abertura;
    private readonly IInteracao _interacao;
    private RetratoPasta? _retrato;
    private EstadoDados _estadoDados = EstadoDados.Vazio;
    private readonly DemandaItem _naoVinculadas;

    /// <summary>Lista da esquerda: demandas na ordem de urgência e, no fim, "Não vinculadas".</summary>
    public ObservableCollection<DemandaItem> Demandas { get; } = [];

    [ObservableProperty] private DemandaItem? _demandaSelecionada;
    [ObservableProperty] private string _resumo = "Carregando…";
    [ObservableProperty] private bool _pastaIndisponivel;
    [ObservableProperty] private string _aviso = "";
    [ObservableProperty] private bool _mostrarAntigas;
    [ObservableProperty] private bool _mostrarArquivadas;
    [ObservableProperty] private bool _semDemandas;
    [ObservableProperty] private bool _dadosDisponiveis = true;
    [ObservableProperty] private string _avisoDados = "";

    public MainViewModel(string pasta, TimeProvider relogio, Dispatcher dispatcher,
        DadosPainel dados, AberturaSessao abertura, IInteracao interacao)
    {
        _relogio = relogio;
        _dispatcher = dispatcher;
        _dados = dados;
        _abertura = abertura;
        _interacao = interacao;
        _naoVinculadas = NovoDemandaItem(DemandaItem.ChaveNaoVinculadas, true);
        _monitor = new MonitorPasta(pasta);
        Aviso = $"Pasta de status indisponível: {TextoExibicao.Limpar(pasta)}";
        _monitor.RetratoAtualizado += r => _dispatcher.BeginInvoke(() => Receber(r));
        RecarregarDados();
    }

    public void Iniciar() => _monitor.Iniciar();

    partial void OnMostrarAntigasChanged(bool value) => Aplicar();
    partial void OnMostrarArquivadasChanged(bool value) => Aplicar();

    // ---------- fábricas dos itens (ligam as ações da tela às operações) ----------

    private DemandaItem NovoDemandaItem(string chave, bool naoVinculadas)
        => new(chave, naoVinculadas, EditarDemanda, ArquivarDemanda, DesarquivarDemanda, VincularRepositorio);

    private CardItem NovoCard(string pasta) => new(pasta, NovaSessao, RemoverRepositorio);

    private SessaoItem NovaSessaoItem(string sessionId) => new(sessionId, Retomar, MoverSessao, RemoverSessao, VincularSessao);

    // ---------- dados do painel ----------

    private void RecarregarDados()
    {
        _estadoDados = _dados.Carregar();
        DadosDisponiveis = _estadoDados.Disponivel;
        AvisoDados = _estadoDados.Disponivel ? ""
            : $"Dados do painel indisponíveis: {TextoExibicao.Limpar(_dados.Caminho)} ({TextoExibicao.Limpar(_estadoDados.Erro ?? "")}). "
              + "O arquivo não foi alterado; criar e alterar demandas, abrir e retomar estão desabilitados.";
    }

    private void RecarregarEAplicar()
    {
        RecarregarDados();
        Aplicar();
    }

    private void SelecionarPorChave(string chave)
        => DemandaSelecionada = Demandas.FirstOrDefault(d => d.Chave == chave);

    private static string Chave(string demandaId) => demandaId.ToLowerInvariant();

    // ---------- demandas ----------

    public void NovaDemanda()
    {
        if (!DadosDisponiveis) return;
        string? criada = null;
        _interacao.DialogoDemanda(null, d =>
        {
            var r = _dados.CriarDemanda(d.Id, d.Titulo, d.OpenspecChange, _relogio.GetUtcNow());
            if (!r.Ok) return r.Mensagem;
            criada = d.Id;
            return null;
        });
        if (criada is null) return;
        RecarregarEAplicar();
        SelecionarPorChave(Chave(criada));
    }

    private void EditarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeEditar(demanda)) return;
        string? novoId = null;
        _interacao.DialogoDemanda(demanda, d =>
        {
            var r = _dados.EditarDemanda(demanda.Id, d.Id, d.Titulo, d.OpenspecChange);
            if (!r.Ok) return r.Mensagem;
            novoId = d.Id;
            return null;
        });
        if (novoId is null) return;
        RecarregarEAplicar();
        SelecionarPorChave(Chave(novoId)); // o id pode ter mudado, e com ele a chave do item
    }

    private void ArquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        if (!RegrasDemanda.PodeArquivar(demanda))
        {
            _interacao.Erro(RegrasDemanda.EhImplicita(demanda.Id)
                ? "A demanda \"Sem demanda\" não pode ser arquivada." : "A demanda já está arquivada.");
            return;
        }
        if (item.TemSessaoEmAndamento
            && !_interacao.Confirmar("Esta demanda tem sessões em andamento. Arquivar mesmo assim?"))
            return;
        var r = _dados.ArquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível arquivar a demanda.");
    }

    private void DesarquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        var r = _dados.DesarquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível desarquivar a demanda.");
    }

    // ---------- repositórios ----------

    private void VincularRepositorio(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeVincularRepositorio(demanda)) return;

        // Sugere as pastas já usadas em outras demandas e ainda não vinculadas a esta.
        var jaNaDemanda = demanda.Repositorios.Select(r => r.Pasta).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sugestoes = _estadoDados.Demandas
            .SelectMany(d => d.Repositorios.Select(r => r.Pasta))
            .Where(p => !jaNaDemanda.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vinculou = _interacao.DialogoVincularRepositorio(item.Cabecalho, sugestoes, pasta =>
        {
            var r = _dados.VincularRepositorio(demanda.Id, pasta.Trim());
            return r.Ok ? null : r.Mensagem;
        });
        if (vinculou) RecarregarEAplicar();
    }

    private void RemoverRepositorio(CardItem card)
    {
        if (!DadosDisponiveis) return;
        if (card.Sessoes.Count > 0)
        {
            var n = card.Sessoes.Count;
            var msg = $"O repositório {card.Pasta} tem {n} {(n == 1 ? "sessão registrada que deixará" : "sessões registradas que deixarão")} de poder ser retomada{(n == 1 ? "" : "s")} nesta demanda. Remover mesmo assim?";
            if (!_interacao.Confirmar(msg)) return;
        }
        var r = _dados.RemoverRepositorio(card.DemandaId, card.Pasta);
        RecarregarEAplicar();
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível remover o repositório.");
    }

    // ---------- abrir, retomar, mover e remover sessões ----------

    private void NovaSessao(CardItem card)
    {
        if (!DadosDisponiveis) return;
        var demanda = DemandaSelecionada?.Demanda;
        if (demanda is null || !RegrasDemanda.PodeAbrirNovaSessao(demanda)) return;

        var escolha = _interacao.PedirNovaSessao(DemandaSelecionada!.Cabecalho, card.Pasta);
        if (escolha is null) return;

        var r = _abertura.Nova(card.DemandaId, card.Pasta, escolha.Agente, escolha.Rotulo);
        RecarregarEAplicar();
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    private void Retomar(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Registrada is not { } registrada) return;

        if (Correspondencia.ExigeConfirmacao(item.Status)
            && !_interacao.Confirmar($"Esta sessão aparece como \"{item.StatusTexto}\" e pode já estar aberta no terminal. Abrir outra aba mesmo assim?"))
            return;

        var r = _abertura.Retomar(registrada);
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível retomar a sessão.");
    }

    private void MoverSessao(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Registrada is not { } registrada) return;

        var destinos = _estadoDados.Demandas
            .Where(d => !d.Arquivada && !string.Equals(d.Id, registrada.DemandaId, StringComparison.OrdinalIgnoreCase))
            .Select(d => new DestinoDemanda(d.Id, $"{TextoExibicao.Limpar(d.Id)} · {TextoExibicao.Limpar(d.Titulo)}"))
            .ToList();
        if (destinos.Count == 0)
        {
            _interacao.Informar("Não há outra demanda para receber a sessão. Crie uma demanda primeiro.");
            return;
        }

        var destino = _interacao.EscolherDemandaDestino(destinos);
        if (destino is null) return;

        var r = _dados.MoverSessao(registrada.SessionId, destino);
        RecarregarEAplicar();
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível mover a sessão.");
    }

    private void RemoverSessao(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Registrada is not { } registrada) return;
        if (!_interacao.Confirmar("Remover o registro desta sessão? A conversa não é apagada, mas a sessão deixará de poder ser retomada pelo painel."))
            return;

        var r = _dados.RemoverSessao(registrada.SessionId);
        RecarregarEAplicar();
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível remover o registro da sessão.");
    }

    // ---------- vínculo de sessões não vinculadas ----------

    /// <summary>"Vincular" na entrada "Não vinculadas": diálogo com demanda, repositório e rótulo.</summary>
    private void VincularSessao(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Lida is not { } lida) return;

        var pedido = PedidoVinculo.Criar(lida);
        if (pedido is null)
        {
            _interacao.Erro(RegrasVinculo.PodeVincularSessao(lida).Motivo ?? "Esta sessão não pode ser vinculada.");
            return;
        }

        var opcoes = _estadoDados.Demandas
            .Where(d => !d.Arquivada)
            .Select(d => new OpcaoDemandaVinculo(
                d.Id,
                $"{TextoExibicao.Limpar(d.Id)} · {TextoExibicao.Limpar(d.Titulo)}",
                d.Repositorios.Select(r => r.Pasta).ToList(),
                d.Repositorios.Select(r => r.Pasta).FirstOrDefault(p => RegrasVinculo.PastaCoincide(lida, p))))
            .ToList();
        if (opcoes.Count == 0)
        {
            _interacao.Informar("Não há demanda para receber a sessão. Crie uma demanda primeiro.");
            return;
        }

        var descricao = $"{(lida.Agente == Agente.Claude ? "Claude" : "Codex")} · pasta {TextoExibicao.NomePasta(lida.Cwd)} · sessão {TextoExibicao.Limpar(lida.SessionId)}";
        var vinculou = _interacao.DialogoVincularSessao(descricao, opcoes, escolha =>
        {
            if (!ValidadorEntrada.PastaValida(escolha.Repositorio))
                return "Repositório inválido: use apenas letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.";
            if (escolha.Rotulo is not null && !ValidadorEntrada.RotuloValido(escolha.Rotulo))
                return "Rótulo inválido: use até 40 caracteres, sem \";\" nem caracteres de controle.";

            if (RegrasVinculo.AvisarPastaDivergente(lida, escolha.Repositorio)
                && !_interacao.Confirmar($"A pasta de trabalho desta sessão termina em \"{TextoExibicao.NomePasta(lida.Cwd)}\", mas o repositório escolhido é \"{escolha.Repositorio}\". "
                    + "Ao retomar, o agente pode não encontrar a conversa. Vincular mesmo assim?"))
                return ""; // não confirmou: o diálogo continua aberto

            var r = _dados.VincularSessao(pedido, escolha.DemandaId, escolha.Repositorio, escolha.Rotulo, _relogio.GetUtcNow());
            return r.Ok ? null : r.Mensagem ?? "Não foi possível vincular a sessão.";
        });
        if (vinculou) RecarregarEAplicar();
    }

    // ---------- tela ----------

    private void Receber(RetratoPasta retrato)
    {
        _retrato = retrato;
        RecarregarDados();
        Aplicar();
    }

    private void Aplicar()
    {
        if (_retrato is null) return;
        PastaIndisponivel = !_retrato.Disponivel;

        var visao = VisaoAgrupada.Montar(_estadoDados.Demandas, _retrato.Sessoes, MostrarArquivadas, MostrarAntigas, _relogio);
        Resumo = _retrato.Disponivel ? visao.ResumoCabecalho : "";
        SemDemandas = visao.Demandas.Count == 0;

        // Tudo é atualizado no lugar, por chave: a demanda selecionada e a rolagem não se perdem
        // quando um status muda ou quando a ordem das demandas muda.
        var linhas = visao.Demandas.Select(d => (Chave: Chave(d.Demanda.Id), Dado: (DemandaNaVisao?)d)).ToList();
        linhas.Add((DemandaItem.ChaveNaoVinculadas, null));

        SincronizacaoColecao.Sincronizar(Demandas, linhas,
            d => d.Chave, l => l.Chave,
            l => l.Dado is null ? _naoVinculadas : NovoDemandaItem(l.Chave, false),
            (item, l) =>
            {
                if (l.Dado is { } d) item.Atualizar(d, NovoCard, NovaSessaoItem);
                else item.AtualizarNaoVinculadas(visao.NaoVinculadas, NovaSessaoItem);
            });
    }

    public void Dispose() => _monitor.Dispose();
}
