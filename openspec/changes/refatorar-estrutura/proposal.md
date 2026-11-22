# Proposal

## Why

O painel cresceu etapa por etapa e a mesma regra passou a existir em vários lugares: a validade de um lançamento está escrita em quatro classes, a ordenação de sessões em três, a conversão do agente em texto em seis, e a leitura limitada de arquivo em duas. Arquivos têm nomes que não dizem o que contêm, o `MainViewModel` faz acesso a disco e validação que já existem no Core, e `Models` depende de `Persistence`. Quem chega ao projeto não sabe onde cada regra mora nem qual cópia é a certa, e uma correção feita numa cópia deixa as outras para trás.

## What Changes

- **Cada regra passa a ter um único lugar.** As cópias são removidas e os chamadores passam a usar essa versão única:
  - validade do lançamento;
  - conjunto de sessões excluídas e registradas;
  - ordenação de sessões;
  - nome do agente (no arquivo e na tela);
  - leitura limitada em UTF-8 estrito;
  - listagem de pastas sem atalhos;
  - mensagens de validação;
  - checagem de trabalho arquivado no JSON.
- **Apelidos e código morto saem:**
  - `RemoverSessao` (fica `DesfazerRegistro`);
  - `CancelarLancamento` duplicado de `DesfazerLancamento`;
  - `RegrasVinculo.PodeVincularSessao` + `PodeVincular`, que repetem `PedidoVinculo.Criar`;
  - `ValidadorEntrada.IdTrabalhoGeradoValido`, que não é usado;
  - `ProgressoDosRepositorios`, um laço que vira método do `LeitorTasks`;
  - `EscolhaPastaWindows`, cujas 5 linhas passam para `InteracaoJanelas`.
- **Arquivos com um assunto e o nome dele.** `NavegacaoDemandas.cs`, `RepositoriosNaRaiz.cs`, `ProgressoOpenspec.cs`, `Modelo.cs`/`DadosModelo.cs` e `Correspondencia.cs` são divididos ou renomeados para que o nome do arquivo seja o do tipo principal que ele contém.
- **Dependências na direção certa.** As constantes da demanda implícita e a validade do lançamento saem de `DadosPainel` e vão para `Models`, e `Models` deixa de depender de `Persistence`.
- **`MainViewModel` só liga tela e Core.** A listagem de pastas da raiz, a validação repetida no diálogo de vínculo e a montagem de uma `Sessao` artificial para vincular uma sessão registrada passam para o Core.
- **Itens da lista sem callback.** `DemandaItem`, `CardItem`, `SessaoItem` e `LancamentoItem` deixam de receber `Action` no construtor e fábricas `Func` no `Atualizar`. Cada comando passa a chamar o método do `MainViewModel` diretamente. `FiltroDemandas.Filtrar<T>` dá lugar a chamadas diretas a `Casa`. Os callbacks dos diálogos que gravam e os da infraestrutura continuam.
- **`AberturaSessao` com um só fluxo de "montar comando, registrar, disparar e desfazer se falhar"**, no lugar das quatro cópias.
- **`docs/arquitetura.md`**, uma página curta com o mapa das pastas, o fluxo de um ciclo de leitura e a correspondência "Demanda no código = Trabalho na tela".

Fora desta change:
- renomear `Demanda` para `Trabalho` no código, que pode virar um change mecânico separado;
- qualquer mudança de comportamento, de tela, dos arquivos `demandas.json` e de status, ou dos scripts;
- o change `etapa-5b-bandeja-e-avisos`;
- novas dependências.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

Nenhuma. Nenhum requisito muda, por isso o change usa `skip_specs: true`.

## Impact

- **Código:** `src/DataPilot.Core` (todas as pastas) e `src/DataPilot.Wpf` (`ViewModels`, `Services`). Tipos públicos são movidos ou removidos; o XAML só muda se um nome ligado a ele mudar.
- **Testes:** os 583 testes atuais continuam passando; os que usam um apelido removido passam a chamar o nome que fica; testes de código apagado são apagados junto ou movidos para a versão única.
- **Dados, scripts e segurança:** nada muda. As validações na fronteira, a recusa de atalhos e junções, a gravação atômica e a preservação de campos desconhecidos são mantidas exatamente como estão.
