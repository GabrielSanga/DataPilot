using System.Text;
using DataPilot.Core.Models;
using DataPilot.Core.Sessions;

namespace DataPilot.Core.Tests.Sessions;

public class MontadorComandoCaminhoTests
{
    private const string Id = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static IReadOnlyList<string> Args(string pasta, string? titulo = "Ajuste")
        => MontadorComando.Novo("CHG-142", titulo, new RepositorioCadastrado(pasta, []), Agente.Claude, Id, null).Argumentos;

    [Fact]
    public void Repositorio_da_raiz_continua_com_repo()
    {
        var args = Args("Faturamento");
        var i = args.ToList().IndexOf("--repo");
        Assert.Equal("Faturamento", args[i + 1]);
        Assert.DoesNotContain("--caminho", args);
    }

    [Fact]
    public void Caminho_completo_vai_em_Base64_sem_texto_livre_na_linha()
    {
        var args = Args(@"C:\Fontes\código & cia;v2");

        var i = args.ToList().IndexOf("--caminho");
        Assert.True(i > 0);
        Assert.Equal("/workspace/código & cia;v2", Encoding.UTF8.GetString(Convert.FromBase64String(args[i + 1])));
        Assert.DoesNotContain("--repo", args);
        Assert.DoesNotContain(args.SkipWhile(a => a != "docker"), a => a.IndexOfAny([' ', '&', ';', '"', '%', '^', '|']) >= 0);
    }

    [Fact]
    public void Titulo_mostra_so_o_nome_da_pasta_sem_ponto_e_virgula()
        => Assert.Equal("Ajuste · código & cia,v2",
            MontadorComando.Titulo("CHG-142", "Ajuste", Agente.Claude, @"C:\Users\Gabriel Sanga\código & cia;v2", null));

    [Fact]
    public void Caminho_invalido_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => Args(@"C:\Projetos\..\Windows"));
}
