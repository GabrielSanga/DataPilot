# Design

## Context

São dois projetos: `DataPilot.Core` (regras puras e acesso a arquivos, coberto por 583 testes xUnit) e `DataPilot.Wpf` (MVVM com CommunityToolkit). O Core já está dividido em pastas por assunto (`Models`, `Persistence`, `Monitoring`, `Sessions`, `Linking`, `Display`, `OpenSpec`, `Validation`). Essa divisão é boa e fica. O problema está dentro das pastas: há regras copiadas, arquivos com vários assuntos e partes da tela fazendo trabalho do Core. A motivação está em proposal.md.

Restrições: o comportamento observável não muda (as specs ficam como estão); os arquivos `demandas.json` e de status continuam no mesmo formato; toda validação de fronteira e toda defesa contra atalhos e junções continua existindo, só que em um lugar.

## Goals / Non-Goals

**Goals:**
- Uma regra, um lugar. Os chamadores usam essa versão e as cópias são apagadas.
- Cada arquivo leva o nome do tipo principal que contém.
- O `MainViewModel` só traduz ações de tela em chamadas ao Core e o resultado em propriedades.
- Os testes continuam passando a cada grupo de tarefas, e não só no fim.

**Non-Goals:**
- Renomear `Demanda` para `Trabalho` no código.
- Novas camadas ou padrões: nada de repositório, injeção de dependência, mediator ou interfaces novas.
- Reescrever `DadosPainel` como serialização tipada (a árvore JSON é o que preserva campos desconhecidos).
- Mudar o XAML, além de acompanhar um nome que mudou.

## Decisions

### 1. Manter as pastas do Core e adicionar uma só: `Arquivos`

Hoje três lugares fazem a mesma coisa:
- **Saber se um caminho é atalho ou junção:** `LocalizadorTasks.Tipo`, que `ChangesDoRepositorio` usa atravessando a classe.
- **Ler um arquivo com limite de bytes, sem BOM e em UTF-8 estrito:** `LeitorPasta.LerEValidar` e `LeitorTasks.LerEContar`.
- **Listar subpastas que não são atalho:** `ChangesDoRepositorio`, `LocalizadorTasks`, `MainViewModel.PastasDaRaiz`.

Tudo isso vai para `Core/Arquivos/LeituraSegura.cs`, uma classe estática com três funções: `Tipo`, `LerTexto` e `Subpastas`. Ela é o único lugar que conhece as regras de leitura de pastas que pertencem ao usuário `ia`.

*Alternativa descartada:* deixar como está e só renomear. Isso não resolve, porque as cópias de leitura com limite já divergem em detalhes (uma confere o arquivo aberto, a outra não). Conferir o arquivo aberto continua sendo uma etapa do `LeitorTasks`, chamada depois de `LerTexto`.

### 2. `Models` não depende de nenhuma outra pasta

As seguintes regras passam a morar em `Models`:
- `IdDemandaImplicita`, `TituloDemandaImplicita` e `ValidadeLancamento` (hoje em `DadosPainel`), que vão para `RegrasDemanda`;
- `RegrasDemanda.LancamentoValido(demanda, lancamento, agora)`, a regra única de "não arquivado e com até 24 h", usada pela visão, pelo vínculo automático e pela reabertura (a limpeza de vencidos em `DadosPainel` usa a mesma constante);
- `Agentes`, com `NoArquivo(agente)` (`"claude"`), `TentarLer(texto, out agente)` e `Nome(agente)` (`"Claude"`). Substitui `ValidadorEntrada.NomeAgente/TentarAgente`, o `switch` de `ValidadorStatus` e as quatro expressões `== Claude ? "Claude" : "Codex"`;
- `ChaveSessao.De(agente, id)`, a chave `(agente, id em minúsculas)`. É usada por `VisaoAgrupada` e por `VinculoAutomatico`, que hoje constroem esses conjuntos cada um à sua maneira.

Arquivos: `Modelo.cs` vira `Sessao.cs` (Sessao e enums), `Constantes.cs` e `Agentes.cs`; `DadosModelo.cs` vira `Demanda.cs` (records do arquivo de dados) e `RegrasDemanda.cs`.

### 3. Uma ordenação de sessões e um texto de quantidade

`RegrasExibicao.ComparaSessoes(status, data, id)` é a regra "status mais urgente, depois o mais recente, depois o id". `RegrasExibicao.Montar`, `VisaoAgrupada` e `DemandaItem.AtualizarSessoes` passam a usá-la. `RegrasExibicao.Quantidade(n, "sessão", "sessões", "nenhuma")` monta os plurais hoje repetidos em `VisaoAgrupada.TextoResumo` e em `DemandaItem`. `Correspondencia.ExigeConfirmacao`, que é uma regra de tela de uma linha, vai para `RegrasExibicao`, e o arquivo `Correspondencia.cs` é apagado.

### 4. O vínculo trabalha com a pasta de trabalho, não com uma `Sessao` inteira

Hoje `RegrasVinculo.PastaCoincide` recebe uma `Sessao`. Para vincular uma sessão já registrada, que não tem arquivo de status, o `MainViewModel` fabrica uma `Sessao` falsa. A regra passa a receber o texto da pasta de trabalho (`cwd`). `PedidoVinculo` ganha um segundo `Criar(SessaoRegistrada)`, que é seguro porque esse dado vem do arquivo de dados já validado. Com isso a `Sessao` artificial desaparece. `RegrasVinculo.PodeVincularSessao` e o record `PodeVincular` são apagados: a tela usa `PedidoVinculo.Criar(...) is null` e a constante `MotivoIdentificadorInvalido`. `VinculoSessao.cs` é renomeado para `PedidoVinculo.cs`.

### 5. `DadosPainel` continua uma classe, dividida em arquivos `partial`

O arquivo de 800 linhas já tem seções (leitura, demandas, repositórios, sessões, lançamentos, apoio). Cada seção vira um arquivo `DadosPainel.<Seção>.cs` na mesma pasta, e a API pública não muda. As outras mudanças em `DadosPainel` são:
- os apelidos `RemoverSessao` e `CancelarLancamento` são apagados (ficam `DesfazerRegistro` e `DesfazerLancamento`);
- as três checagens `demanda["arquivada"] is JsonValue ...` viram `EstaArquivada(no)`;
- os dois laços de remover uma sessão por id (`ExcluirSessao` e `DesfazerRegistro`) viram `RetirarSessoes(raiz, id)`.

*Alternativa descartada:* separar em `RepositorioDemandas`, `RepositorioSessoes` etc. Isso multiplicaria a leitura, a gravação atômica e a preservação de campos por várias classes, ou exigiria uma classe base. O `partial` dá a navegação por assunto sem mudar nenhuma chamada.

### 6. Mensagens de validação ao lado da regra

`ValidadorEntrada` ganha `MensagemPasta`, `MensagemRotulo`, `MensagemTitulo` e `MensagemChange`. Hoje o mesmo texto está copiado em `DadosPainel`, `AberturaSessao`, `MontadorComando`, `EscolhaDePasta` e `MainViewModel`. Onde duas versões diferem ("Repositório inválido." e "Repositório inválido: use apenas letras…"), fica o texto mais completo.

### 7. `AberturaSessao` com um fluxo só

As quatro operações (`Nova`, `ReabrirLancamento`, `RetomarNaoRegistrada`, `Retomar`) repetem o mesmo fluxo:
1. montar o comando (erro vira falha);
2. gravar;
3. disparar;
4. se o disparo falhar, desfazer a gravação.

Esse fluxo vira um método privado `Executar(montar, gravar, desfazer)`. Cada operação passa a conter só o que é dela: encontrar o trabalho e o repositório, gerar o id e escolher entre registro e lançamento.

### 8. WPF: o view model só liga tela e Core

- `PastasDaRaiz` sai do VM e vai para `EscolhaDePasta.PastasDaRaiz(raiz)`, que usa `LeituraSegura.Subpastas`.
- `VincularSessao` deixa de revalidar pasta e rótulo, porque `DadosPainel.VincularSessao` já valida e devolve a mensagem.
- `AbrirLancamento` e `CancelarLancamento` usam um único `LancamentoExiste(id)`.
- `ProgressoDosRepositorios.Montar` vira `LeitorTasks.LerRepositorios`.
- `EscolhaPastaWindows` (5 linhas) passa para dentro de `InteracaoJanelas.EscolherPasta`.
- Os três records de `Wpf/Models` só existem como parâmetros de `IInteracao`. Eles passam para o próprio `IInteracao.cs`, e a pasta é apagada.

### 9. Arquivos com um assunto

Divisões e renomeações de arquivo, sem mudança de código:
- `NavegacaoDemandas.cs` vira `FiltroDemandas.cs` e `ProximaSessao.cs`;
- `ResumoCabecalho` sai de `RegrasExibicao.cs` para o próprio arquivo;
- `ProgressoOpenspec.cs` vira `ContadorTarefas.cs`, `LocalizadorTasks.cs` e `LeitorTasks.cs` (com `ProgressoRepositorio`);
- `RepositoriosNaRaiz.cs` vira `EscolhaDePasta.cs` e `ChangesDoRepositorio.cs`.

As pastas de testes acompanham as pastas do Core.

### 10. Itens da lista chamam o `MainViewModel` em vez de receber callbacks

Hoje os itens recebem 13 `Action<T>` no construtor, e as fábricas que os criam (3 `Func<string, …>`) atravessam três níveis de `Atualizar`. O "ir para a definição" de um comando leva a um parâmetro, e não ao método que roda. Cada item passa a receber o `MainViewModel` e chama o método diretamente (`_painel.Retomar(this)`). Assim, os filhos são criados com `new …(chave, _painel)` e as fábricas somem.

*Alternativa descartada:* comandos no `MainViewModel` com `CommandParameter="{Binding}"`. Os menus são `ContextMenu` compartilhados, fora da árvore visual: `RelativeSource AncestorType=Window` não os alcança, e exigiria um *binding proxy* ou `PlacementTarget.Tag`, ou seja, mais código do que o que sai.

Ficam como callback, de propósito:
- **os três diálogos que gravam (`aplicar`):** o diálogo continua aberto e mostra o erro junto do campo quando a gravação falha;
- **os eventos da plataforma** (`RetratoAtualizado`, `FileSystemWatcher`, `Loaded`);
- **`DadosPainel.Alterar`:** o único lugar da gravação atômica;
- **`AberturaSessao.Executar`:** o fluxo único de abertura;
- **`SincronizacaoColecao`:** um algoritmo genérico;
- **o `Func<Guid>` dos testes.**

`FiltroDemandas.Filtrar<T>` só existia para o view model testar um item de cada vez. Ele sai, e o view model chama `Casa`.

## Risks / Trade-offs

- **Itens dependem do `MainViewModel` (acoplamento na mesma camada).** Aceito: não há testes de view model, e o acoplamento já existia, escondido nos delegates. Se algum dia houver testes de view model, extrair a interface nesse momento.

- **Uma cópia "igual" que na verdade diverge e é tratada como idêntica.** Mitigação: antes de unificar, comparar as cópias linha a linha. Quando a diferença é intencional (conferir o arquivo aberto, rótulo inválido que invalida o lançamento), ela fica explícita no chamador. Os 583 testes rodam ao fim de cada grupo.
- **O vínculo automático passa a usar a chave `(agente, id)`, como a visão, em vez de só `id`.** Uma sessão com o mesmo id em outro agente passaria a ser candidata, mas `DadosPainel.VincularPorLancamento` recusa id já registrado na mesma gravação. O resultado final é o mesmo, e há um teste para cobrir esse caso.
- **Texto de erro em um caso hoje inalcançável** (repositório inválido no diálogo de vínculo, que só lista pastas já validadas) passa a aparecer depois do aviso de pasta divergente, e não antes. Aceito.
- **Os testes chamam tipos e métodos que vão sumir.** Mitigação: em cada grupo, os testes do código movido são movidos junto, e os de apelidos passam a chamar o nome que fica. Nenhum cenário de comportamento é apagado; só se apagam testes de código que deixou de existir (por exemplo, `IdTrabalhoGeradoValido`, que já não tinha teste).
- **Um diff grande em arquivos movidos dificulta a revisão.** Mitigação: um commit por grupo de tarefas. Em cada grupo, os movimentos de arquivo puros vão num commit separado das mudanças de código.

## Migration Plan

Nenhuma migração de dados. Para reverter, basta descartar o branch `refatorar-estrutura`: nenhum arquivo do usuário muda de formato.
