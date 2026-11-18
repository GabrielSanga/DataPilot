using System.Diagnostics;
using System.Text;
using DataPilot.Core;

namespace DataPilot.Core.Tests;

public sealed class ContadorTarefasTests
{
    [Fact]
    public void Conta_marcadas_e_pendentes()
    {
        var texto = string.Join("\n", Enumerable.Repeat("- [x] a", 7).Concat(Enumerable.Repeat("- [ ] b", 5)));
        Assert.Equal(new ContagemTarefas(7, 12), ContadorTarefas.Contar(texto));
    }

    [Fact]
    public void Aceita_X_maiusculo_indentacao_e_fim_de_linha_do_Windows()
    {
        var texto = "- [X] a\r\n  - [x] b\r\n\t- [ ] c\r\n";
        Assert.Equal(new ContagemTarefas(2, 3), ContadorTarefas.Contar(texto));
    }

    [Fact]
    public void Ignora_linhas_parecidas_que_nao_casam()
    {
        var texto = string.Join("\n",
            "# Tasks", "- [] sem espaco", "- [xx] duas letras", "-[ ] sem espaco apos o traco",
            "* [ ] asterisco", "texto - [ ] no meio", "- [y] outra letra", "1. [ ] numerada", "- [ ]sem espaco depois");
        // A última casa: a regra só exige a caixa após "- ".
        Assert.Equal(new ContagemTarefas(0, 1), ContadorTarefas.Contar(texto));
    }

    [Fact]
    public void Arquivo_sem_tarefas()
        => Assert.Equal(new ContagemTarefas(0, 0), ContadorTarefas.Contar("# Tasks\n\nnada aqui\n"));
}

public sealed class LeitorTasksTests : IDisposable
{
    private readonly PastaTemporaria _raiz = new();

    public void Dispose() => _raiz.Dispose();

    private string Pasta(params string[] partes) => Path.Combine([_raiz.Caminho, .. partes]);

    private string Tasks(string repo, string conteudo, string change = "ajuste", bool arquivada = false, string data = "2026-11-20")
    {
        var dir = arquivada
            ? Pasta(repo, "openspec", "changes", "archive", $"{data}-{change}")
            : Pasta(repo, "openspec", "changes", change);
        Directory.CreateDirectory(dir);
        var arq = Path.Combine(dir, "tasks.md");
        File.WriteAllText(arq, conteudo, new UTF8Encoding(false));
        return arq;
    }

    private static string Linhas(int marcadas, int pendentes)
        => string.Join("\n", Enumerable.Repeat("- [x] a", marcadas).Concat(Enumerable.Repeat("- [ ] b", pendentes))) + "\n";

    private ProgressoRepositorio Ler(string repo, string change = "ajuste") => new LeitorTasks(_raiz.Caminho).Ler(repo, change);

    private static void Juncao(string link, string alvo)
    {
        var psi = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{alvo}\"")
        { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        Assert.True(Directory.Exists(link), "mklink /J falhou");
    }

    // ---------- localização ----------

    [Fact]
    public void Change_ativa()
    {
        Tasks("Faturamento", Linhas(2, 1));
        var r = Ler("Faturamento");
        Assert.Equal(new ProgressoRepositorio("Faturamento", EstadoRepositorio.Lido, false, 2, 3), r);
    }

    [Fact]
    public void Repositorio_sem_a_change_fica_sem_progresso()
    {
        Directory.CreateDirectory(Pasta("Estoque", "openspec", "changes"));
        Assert.Equal(EstadoRepositorio.NaoEncontrada, Ler("Estoque").Estado);
        Assert.Equal(EstadoRepositorio.NaoEncontrada, Ler("NaoExiste").Estado);
    }

    [Fact]
    public void Change_arquivada()
    {
        Tasks("F", Linhas(3, 0), arquivada: true);
        var r = Ler("F");
        Assert.Equal(new ProgressoRepositorio("F", EstadoRepositorio.Lido, true, 3, 3), r);
        Assert.Equal("3 de 3 · arquivada", r.Texto);
    }

    [Fact]
    public void Varias_arquivadas_vale_a_data_mais_recente()
    {
        Tasks("F", Linhas(1, 9), arquivada: true, data: "2026-10-01");
        Tasks("F", Linhas(5, 0), arquivada: true, data: "2026-11-20");
        Tasks("F", Linhas(2, 2), arquivada: true, data: "2026-11-02");
        Assert.Equal(5, Ler("F").Concluidas);
    }

    [Fact]
    public void Ativa_e_arquivada_ao_mesmo_tempo_vale_a_ativa()
    {
        Tasks("F", Linhas(1, 1));
        Tasks("F", Linhas(9, 0), arquivada: true);
        var r = Ler("F");
        Assert.False(r.Arquivada);
        Assert.Equal(new ProgressoRepositorio("F", EstadoRepositorio.Lido, false, 1, 2), r);
    }

    [Theory]
    [InlineData("2026-11-20-ajuste-2")]
    [InlineData("2026-11-20-outro-ajuste")]
    [InlineData("2026-13-40-ajuste")]
    [InlineData("26-11-20-ajuste")]
    [InlineData("2026-11-20_ajuste")]
    [InlineData("ajuste")]
    public void Nome_parecido_nao_e_usado(string pastaArquivada)
    {
        var dir = Pasta("F", "openspec", "changes", "archive", pastaArquivada);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "tasks.md"), Linhas(1, 1));
        Assert.Equal(EstadoRepositorio.NaoEncontrada, Ler("F").Estado);
    }

    [Fact]
    public void Entradas_invalidas_sao_recusadas_sem_montar_caminho()
    {
        Tasks("F", Linhas(1, 1));
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("..").Estado);
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler(@"F\..\F").Estado);
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F", "..").Estado);
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F", @"..\changes\ajuste").Estado);
    }

    [Fact]
    public void Change_ativa_sem_tasks_md_aparece_sem_tarefas()
    {
        Directory.CreateDirectory(Pasta("F", "openspec", "changes", "ajuste"));
        Assert.Equal(EstadoRepositorio.SemTarefas, Ler("F").Estado);
    }

    [Fact]
    public void Arquivo_sem_tarefas()
    {
        Tasks("F", "# Tasks\nnada\n");
        Assert.Equal("sem tarefas", Ler("F").Texto);
    }

    // ---------- leitura segura ----------

    [Fact]
    public void Juncao_na_pasta_da_change_e_recusada()
    {
        Directory.CreateDirectory(Pasta("F", "openspec", "changes"));
        Tasks("Outro", Linhas(9, 9), "ajuste");
        Juncao(Pasta("F", "openspec", "changes", "ajuste"), Pasta("Outro", "openspec", "changes", "ajuste"));
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F").Estado);
    }

    [Fact]
    public void Juncao_na_pasta_do_repositorio_e_recusada()
    {
        Tasks("Real", Linhas(1, 1));
        Juncao(Pasta("F"), Pasta("Real"));
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F").Estado);
    }

    [Fact]
    public void Juncao_no_lugar_do_tasks_md_e_recusada()
    {
        var dir = Pasta("F", "openspec", "changes", "ajuste");
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Pasta("Alvo"));
        Juncao(Path.Combine(dir, "tasks.md"), Pasta("Alvo"));
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F").Estado);
    }

    [Fact]
    public void Juncao_na_pasta_arquivada_e_recusada()
    {
        Tasks("Outro", Linhas(1, 1), arquivada: true);
        Directory.CreateDirectory(Pasta("F", "openspec", "changes"));
        Juncao(Pasta("F", "openspec", "changes", "archive"), Pasta("Outro", "openspec", "changes", "archive"));
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F").Estado);
    }

    [Fact]
    public void Arquivo_acima_do_limite_e_ilegivel()
    {
        Tasks("F", new string('x', LeitorTasks.TamanhoMaximo + 1));
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F").Estado);
    }

    [Fact]
    public void Arquivo_no_limite_e_lido()
    {
        var cabeca = "- [x] a\n";
        Tasks("F", cabeca + new string('x', LeitorTasks.TamanhoMaximo - cabeca.Length));
        Assert.Equal(new ProgressoRepositorio("F", EstadoRepositorio.Lido, false, 1, 1), Ler("F"));
    }

    [Fact]
    public void Utf8_invalido_e_ilegivel()
    {
        var arq = Tasks("F", "x");
        File.WriteAllBytes(arq, [(byte)'-', (byte)' ', (byte)'[', (byte)' ', (byte)']', 0xFF, 0xFE]);
        Assert.Equal(EstadoRepositorio.Ilegivel, Ler("F").Estado);
    }

    [Fact]
    public void Aceita_bom_utf8()
    {
        var arq = Tasks("F", "x");
        File.WriteAllBytes(arq, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("- [x] a\n")]);
        Assert.Equal(1, Ler("F").Total);
    }

    [Fact]
    public void Le_com_o_arquivo_aberto_para_escrita_por_outro_processo()
    {
        var arq = Tasks("F", Linhas(1, 1));
        using var escritor = new FileStream(arq, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        Assert.Equal(2, Ler("F").Total);
    }

    [Fact]
    public void Nao_grava_nada_no_arquivo()
    {
        var arq = Tasks("F", Linhas(1, 1));
        var antes = (File.GetLastWriteTimeUtc(arq), File.ReadAllText(arq));
        Ler("F");
        Assert.Equal(antes, (File.GetLastWriteTimeUtc(arq), File.ReadAllText(arq)));
    }

    // ---------- cache ----------

    [Fact]
    public void Releitura_acompanha_a_alteracao_do_arquivo()
    {
        var arq = Tasks("F", Linhas(1, 2));
        var leitor = new LeitorTasks(_raiz.Caminho);
        Assert.Equal(1, leitor.Ler("F", "ajuste").Concluidas);

        File.WriteAllText(arq, Linhas(2, 1));
        File.SetLastWriteTimeUtc(arq, DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(2, leitor.Ler("F", "ajuste").Concluidas);
    }

    [Fact]
    public void Cache_reaproveita_quando_data_e_tamanho_nao_mudam()
    {
        var arq = Tasks("F", Linhas(1, 2));
        var data = File.GetLastWriteTimeUtc(arq);
        var leitor = new LeitorTasks(_raiz.Caminho);
        Assert.Equal(1, leitor.Ler("F", "ajuste").Concluidas);

        // Mesmo tamanho e mesma data: o conteúdo não é lido de novo.
        File.WriteAllText(arq, Linhas(2, 1));
        File.SetLastWriteTimeUtc(arq, data);
        Assert.Equal(1, leitor.Ler("F", "ajuste").Concluidas);
    }

    [Fact]
    public void Cache_nao_esconde_uma_juncao_criada_depois()
    {
        Tasks("F", Linhas(1, 1));
        Tasks("Outro", Linhas(1, 1));
        var leitor = new LeitorTasks(_raiz.Caminho);
        Assert.Equal(EstadoRepositorio.Lido, leitor.Ler("F", "ajuste").Estado);

        Directory.Delete(Pasta("F", "openspec", "changes", "ajuste"), true);
        Juncao(Pasta("F", "openspec", "changes", "ajuste"), Pasta("Outro", "openspec", "changes", "ajuste"));
        Assert.Equal(EstadoRepositorio.Ilegivel, leitor.Ler("F", "ajuste").Estado);
    }

    // ---------- soma e exibição ----------

    [Fact]
    public void Change_em_dois_repositorios_soma()
    {
        Tasks("Faturamento", Linhas(4, 2));
        Tasks("Estoque", Linhas(3, 3));
        var p = ProgressoDemanda.Montar("ajuste", ["Faturamento", "Estoque"], new LeitorTasks(_raiz.Caminho));
        Assert.Equal("7 de 12 tarefas", p.Texto);
        Assert.Equal("4 de 6", p.Repositorios[0].Texto);
        Assert.Equal("3 de 6", p.Repositorios[1].Texto);
        Assert.Equal(7.0 / 12, p.Fracao, 6);
    }

    [Fact]
    public void Change_nao_encontrada()
    {
        Directory.CreateDirectory(Pasta("F"));
        var p = ProgressoDemanda.Montar("ajuste", ["F", "G"], new LeitorTasks(_raiz.Caminho));
        Assert.True(p.NaoEncontrada);
        Assert.Equal("change não encontrada nos repositórios", p.Texto);
        Assert.All(p.Repositorios, r => Assert.Equal("", r.Texto));
    }

    [Fact]
    public void Um_repositorio_ilegivel_fica_fora_da_soma()
    {
        Tasks("Faturamento", Linhas(4, 2));
        Tasks("Estoque", new string('x', LeitorTasks.TamanhoMaximo + 1));
        var p = ProgressoDemanda.Montar("ajuste", ["Faturamento", "Estoque"], new LeitorTasks(_raiz.Caminho));
        Assert.Equal("4 de 6 tarefas", p.Texto);
        Assert.Equal("progresso indisponível", p.Repositorios[1].Texto);
        Assert.False(p.NaoEncontrada);
    }

    [Fact]
    public void Todos_ilegiveis_mostram_progresso_indisponivel()
    {
        Tasks("F", new string('x', LeitorTasks.TamanhoMaximo + 1));
        var p = ProgressoDemanda.Montar("ajuste", ["F"], new LeitorTasks(_raiz.Caminho));
        Assert.Equal("progresso indisponível", p.Texto);
    }

    [Fact]
    public void Change_arquivada_na_soma_tem_a_indicacao()
    {
        Tasks("F", Linhas(3, 0), arquivada: true);
        var p = ProgressoDemanda.Montar("ajuste", ["F"], new LeitorTasks(_raiz.Caminho));
        Assert.Equal("3 de 3 tarefas · arquivada", p.Texto);
    }

    [Fact]
    public void Sem_tarefas()
    {
        Tasks("F", "# vazio");
        var p = ProgressoDemanda.Montar("ajuste", ["F"], new LeitorTasks(_raiz.Caminho));
        Assert.Equal("sem tarefas", p.Texto);
    }

    [Fact]
    public void Demanda_sem_change_nao_le_nada_nem_mostra_progresso()
    {
        Tasks("F", Linhas(1, 1));
        var p = ProgressoDemanda.Montar(null, ["F"], new LeitorTasks(_raiz.Caminho));
        Assert.Equal("", p.Texto);
        Assert.Empty(p.Repositorios);
    }

    [Fact]
    public void Demanda_sem_repositorios_nao_mostra_nada()
    {
        var p = ProgressoDemanda.Montar("ajuste", [], new LeitorTasks(_raiz.Caminho));
        Assert.Equal("", p.Texto);
        Assert.False(p.NaoEncontrada);
    }
}
