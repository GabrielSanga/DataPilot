using DataPilot.Core.Models;
using DataPilot.Core.Monitoring;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Monitoring;

public class MonitorPastaTests
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(5);

    private sealed class Coletor
    {
        private RetratoPasta? _ultimo;
        public RetratoPasta? Ultimo => Volatile.Read(ref _ultimo);
        public void Receber(RetratoPasta r) => Volatile.Write(ref _ultimo, r);
    }

    private static MonitorPasta Criar(string pasta, bool aviso, Coletor c)
    {
        var m = new MonitorPasta(pasta, TimeSpan.FromMilliseconds(150), aviso, TimeSpan.FromMilliseconds(20));
        m.RetratoAtualizado += c.Receber;
        m.Iniciar();
        return m;
    }

    // Com o aviso desligado, só o temporizador pode produzir a mudança.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SessaoNova_Aparece(bool aviso)
    {
        using var p = new PastaTemporaria();
        var c = new Coletor();
        using var m = Criar(p.Caminho, aviso, c);
        Assert.True(Espera.Ate(() => c.Ultimo is { Disponivel: true }, Prazo));
        p.Escrever("s1.json", Fabrica.Json());
        Assert.True(Espera.Ate(() => c.Ultimo!.Sessoes.Count == 1, Prazo));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SessaoMudaDeStatus_Reflete(bool aviso)
    {
        using var p = new PastaTemporaria();
        var caminho = p.Escrever("s1.json", Fabrica.Json(status: "executando"));
        var c = new Coletor();
        using var m = Criar(p.Caminho, aviso, c);
        Assert.True(Espera.Ate(() => c.Ultimo?.Sessoes.Count == 1, Prazo));
        p.Escrever("s1.json", Fabrica.Json(status: "atencao"));
        File.SetLastWriteTimeUtc(caminho, DateTime.UtcNow.AddSeconds(10));
        Assert.True(Espera.Ate(() => c.Ultimo!.Sessoes.Count == 1 && c.Ultimo.Sessoes[0].Status == StatusGravado.Atencao, Prazo));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArquivoRemovido_SessaoSome(bool aviso)
    {
        using var p = new PastaTemporaria();
        var caminho = p.Escrever("s1.json", Fabrica.Json());
        var c = new Coletor();
        using var m = Criar(p.Caminho, aviso, c);
        Assert.True(Espera.Ate(() => c.Ultimo?.Sessoes.Count == 1, Prazo));
        File.Delete(caminho);
        Assert.True(Espera.Ate(() => c.Ultimo!.Sessoes.Count == 0, Prazo));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PastaInexistente_Indisponivel_EDepoisPassaAExistir(bool aviso)
    {
        using var p = new PastaTemporaria(criar: false);
        var c = new Coletor();
        using var m = Criar(p.Caminho, aviso, c);
        Assert.True(Espera.Ate(() => c.Ultimo is { Disponivel: false }, Prazo));

        Directory.CreateDirectory(p.Caminho);
        p.Escrever("s1.json", Fabrica.Json());
        Assert.True(Espera.Ate(() => c.Ultimo is { Disponivel: true } r && r.Sessoes.Count == 1, Prazo));
    }

    [Fact]
    public void PastaRemovidaERecriada_VoltaALer()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json());
        var c = new Coletor();
        using var m = Criar(p.Caminho, true, c);
        Assert.True(Espera.Ate(() => c.Ultimo?.Sessoes.Count == 1, Prazo));
        Directory.Delete(p.Caminho, true);
        Assert.True(Espera.Ate(() => c.Ultimo is { Disponivel: false }, Prazo));
        Directory.CreateDirectory(p.Caminho);
        p.Escrever("s2.json", Fabrica.Json(id: "s2"));
        Assert.True(Espera.Ate(() => c.Ultimo is { Disponivel: true } r && r.Sessoes.Count == 1 && r.Sessoes[0].SessionId == "s2", Prazo));
    }

    [Fact]
    public void Releituras_NaoSeSobrepoem()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json());
        int ativos = 0, maximo = 0, total = 0;
        using var m = new MonitorPasta(p.Caminho, TimeSpan.FromMilliseconds(10), false);
        m.RetratoAtualizado += _ =>
        {
            var a = Interlocked.Increment(ref ativos);
            int atual;
            while (a > (atual = Volatile.Read(ref maximo))) Interlocked.CompareExchange(ref maximo, a, atual);
            Thread.Sleep(50); // leitura mais lenta que o intervalo
            Interlocked.Decrement(ref ativos);
            Interlocked.Increment(ref total);
        };
        m.Iniciar();
        Assert.True(Espera.Ate(() => Volatile.Read(ref total) >= 3, Prazo));
        Assert.Equal(1, Volatile.Read(ref maximo));
    }

    [Fact]
    public void OuvinteComFalha_NaoDerrubaOMonitor()
    {
        using var p = new PastaTemporaria();
        var n = 0;
        using var m = new MonitorPasta(p.Caminho, TimeSpan.FromMilliseconds(50), false);
        m.RetratoAtualizado += _ => { Interlocked.Increment(ref n); throw new InvalidOperationException(); };
        m.Iniciar();
        Assert.True(Espera.Ate(() => Volatile.Read(ref n) >= 3, Prazo));
    }
}
