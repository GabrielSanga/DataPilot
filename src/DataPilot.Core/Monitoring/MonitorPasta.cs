using DataPilot.Core.Models;

namespace DataPilot.Core.Monitoring;

/// <summary>
/// Produz retratos completos da pasta: por aviso do sistema de arquivos (com pequena espera para agrupar
/// eventos) e por releitura periódica. As releituras nunca se sobrepõem.
/// </summary>
public sealed class MonitorPasta : IDisposable
{
    private readonly LeitorPasta _leitor;
    private readonly TimeSpan _intervalo;
    private readonly bool _usarAviso;
    private readonly TimeSpan _espera;
    private readonly object _trava = new();
    private Timer? _periodico;
    private Timer? _agrupador;
    private FileSystemWatcher? _watcher;
    private bool _lendo, _pendente, _descartado;

    public MonitorPasta(string pasta, TimeSpan? intervalo = null, bool usarAviso = true, TimeSpan? espera = null)
    {
        _leitor = new LeitorPasta(pasta);
        _intervalo = intervalo ?? Constantes.IntervaloReleitura;
        _usarAviso = usarAviso;
        _espera = espera ?? TimeSpan.FromMilliseconds(200);
    }

    public string Pasta => _leitor.Pasta;

    /// <summary>Disparado em thread de segundo plano a cada leitura concluída.</summary>
    public event Action<RetratoPasta>? RetratoAtualizado;

    public void Iniciar()
    {
        lock (_trava)
        {
            if (_descartado || _periodico is not null) return;
            _agrupador = new Timer(_ => Reler(), null, Timeout.Infinite, Timeout.Infinite);
            _periodico = new Timer(_ => Reler(), null, TimeSpan.Zero, _intervalo);
        }
    }

    private void Reler()
    {
        lock (_trava)
        {
            if (_descartado) return;
            if (_lendo) { _pendente = true; return; }
            _lendo = true;
        }
        try
        {
            while (true)
            {
                var retrato = _leitor.Ler();
                GarantirAviso(retrato.Disponivel);
                try { RetratoAtualizado?.Invoke(retrato); }
                catch { /* um ouvinte com falha não pode derrubar o monitor */ }
                lock (_trava)
                {
                    if (_pendente && !_descartado) { _pendente = false; continue; }
                    _lendo = false;
                    return;
                }
            }
        }
        catch
        {
            lock (_trava) { _lendo = false; _pendente = false; }
        }
    }

    private void GarantirAviso(bool disponivel)
    {
        if (!_usarAviso) return;
        lock (_trava)
        {
            if (_descartado) return;
            if (!disponivel)
            {
                _watcher?.Dispose();
                _watcher = null;
                return;
            }
            if (_watcher is not null) return;
            try
            {
                var w = new FileSystemWatcher(_leitor.Pasta)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                    InternalBufferSize = 64 * 1024,
                };
                w.Created += (_, _) => Agendar();
                w.Changed += (_, _) => Agendar();
                w.Deleted += (_, _) => Agendar();
                w.Renamed += (_, _) => Agendar();
                w.Error += (_, _) => Agendar();
                w.EnableRaisingEvents = true;
                _watcher = w;
            }
            catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException)
            {
                _watcher = null; // o temporizador cobre
            }
        }
    }

    private void Agendar()
    {
        lock (_trava)
        {
            if (_descartado) return;
            _agrupador?.Change(_espera, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        lock (_trava)
        {
            _descartado = true;
            _periodico?.Dispose();
            _agrupador?.Dispose();
            _watcher?.Dispose();
        }
    }
}
