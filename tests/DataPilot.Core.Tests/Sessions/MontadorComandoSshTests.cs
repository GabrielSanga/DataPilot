using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

public class MontadorComandoSshTests
{
    private const string Id = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    private static readonly RepositorioCadastrado Repo = new("Faturamento", []);

    private static IReadOnlyList<string> Apos(Configuracoes c)
        => MontadorComando.Novo("CHG-142", "Ajuste", Repo, Agente.Claude, Id, null, c).Argumentos.Skip(6).Take(4).ToList();

    [Fact]
    public void Ssh_com_usuario_padrao() => Assert.Equal(["ssh", "-t", "ia@localhost", "powershell"], Apos(new(true, "ia")));

    [Fact]
    public void Ssh_com_outro_usuario() => Assert.Equal(["ssh", "-t", "agente-ia@localhost", "powershell"], Apos(new(true, "agente-ia")));

    [Fact]
    public void Sem_ssh_o_powershell_vem_logo_depois_do_titulo()
    {
        var args = MontadorComando.Novo("CHG-142", "Ajuste", Repo, Agente.Claude, Id, null, new(false, "ia")).Argumentos;
        Assert.Equal("powershell", args[6]);
        Assert.DoesNotContain("ssh", args);
        Assert.DoesNotContain("-t", args);
    }

    [Fact]
    public void Usuario_invalido_com_ssh_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => Apos(new(true, "-oProxyCommand=calc")));

    [Fact]
    public void Abertura_usa_as_configuracoes_gravadas()
    {
        using var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        dados.CriarDemanda("CHG-142", "Ajuste", null, Fabrica.Agora);
        dados.VincularRepositorio("CHG-142", "Faturamento");
        dados.SalvarConfiguracoes(new Configuracoes(false, "ia"));
        var disparador = new DisparadorFalso();
        var abertura = new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora));
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Codex, null).Ok);
        Assert.DoesNotContain("ssh", Assert.Single(disparador.Comandos).Argumentos);
    }
}
