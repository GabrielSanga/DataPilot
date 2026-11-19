using DataPilot.Core.OpenSpec;

namespace DataPilot.Core.Tests.OpenSpec;

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
