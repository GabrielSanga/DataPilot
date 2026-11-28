# Arquitetura

Mapa curto do código. Para compilar e conferir, veja [desenvolvimento.md](desenvolvimento.md); para o produto, o [README](../README.md).

## Nomes: Demanda no código = Trabalho na tela

O código, o arquivo `demandas.json` e as specs dizem **Demanda**; a tela diz **Trabalho**. A entrada **Sessões** é a demanda fixa `sem-demanda` (`RegrasDemanda.IdDemandaImplicita`).

## Core (`src/DataPilot.Core`): regras e acesso a arquivos

| Pasta | O que tem |
|---|---|
| `Models` | Tipos do domínio sem dependência de outra pasta: `Sessao`, `Demanda` e os registros do arquivo de dados, `Constantes`, `Configuracoes` (nome do container), `LocaisContainer` (pastas do Windows vistas pelo container), `Agentes` (nome do agente), `ChaveSessao` e `RegrasDemanda` (restrições e validade do lançamento). |
| `Arquivos` | `LeituraSegura`: único lugar que lê arquivos e pastas em que o agente grava (sem atalhos, com limite, UTF-8 estrito). |
| `Validation` | `ValidadorEntrada`: regras e mensagens de pasta, rótulo, título, change e identificadores. |
| `Monitoring` | `MonitorPasta` e `LeitorPasta` leem a pasta de status; `ValidadorStatus` valida cada arquivo. |
| `Persistence` | `DadosPainel` (partial por assunto): lê e grava o `demandas.json` como árvore JSON, preservando campos desconhecidos. Só carrega sessões e lançamentos com `execucao` igual a `container`; os demais continuam no arquivo. |
| `Linking` | `VinculoAutomatico` liga sessões a lançamentos pendentes; `PedidoVinculo` e `RegrasVinculo` cuidam do vínculo manual. |
| `Display` | `VisaoAgrupada` monta a visão; `RegrasExibicao`, `ResumoCabecalho`, `FiltroDemandas` e `TextoExibicao` são regras de tela. |
| `OpenSpec` | Progresso das changes (`LocalizadorTasks`, `LeitorTasks`, `ContadorTarefas`) e escolha de pasta e de change. |
| `Sessions` | `AberturaSessao` abre e retoma sessões; `MontadorComando` monta o comando do `docker exec`; `DisparadorContainer` confere e inicia o container antes de o `DisparadorProcesso` (`Disparo.cs`) abrir o terminal. |

## WPF (`src/DataPilot.Wpf`): só liga tela e Core

| Pasta | O que tem |
|---|---|
| `ViewModels` | `MainViewModel` traduz ações da tela em chamadas ao Core; os `*Item` expõem a visão e chamam as ações do `MainViewModel` diretamente. Ficam como callback só os diálogos que gravam (`aplicar`) e a infraestrutura (eventos, `DadosPainel.Alterar`, `AberturaSessao.Executar`, `SincronizacaoColecao`). |
| `Views` | Janelas e diálogos (XAML). |
| `Services` | `IInteracao` e `InteracaoJanelas`: perguntas e diálogos atrás de uma interface. |
| `Helpers`, `Converters`, `Themes` | Sincronização de coleções, conversores e tema. |

## Container (`docker/`)

| Caminho | O que é |
|---|---|
| `Dockerfile`, `compose.yml`, `entrypoint.sh` | Imagem do agente (CLIs fixados por versão) e os serviços `agente` e `proxy`, com a rede interna e as montagens. |
| `proxy/squid.conf` | Destinos liberados. |
| `painel/abrir.sh`, `painel/hook.mjs` | Abertura e retomada das sessões e hook de status. O hook também cria o marcador `<id>.criado`, a única fonte da data de início da sessão, porque a montagem não preserva a data de criação do arquivo substituído. |
| `painel/claude-managed-settings.json`, `painel/codex-hooks.json` | Configuração dos hooks. |
| `testes/verificar.sh` | Verificação dos scripts, dentro do container. |

## Um ciclo de leitura

1. `MonitorPasta` relê a pasta de status (`LeitorPasta` + `ValidadorStatus`) e entrega um retrato ao `MainViewModel`.
2. O view model recarrega o `demandas.json` (`DadosPainel.Carregar`).
3. `VinculoAutomatico.Aplicar` liga sessões novas a lançamentos pendentes e grava cada vínculo.
4. `VisaoAgrupada.Montar` combina trabalhos, sessões registradas e sessões lidas, e `RegrasExibicao` calcula status e ordem.
5. Os itens de tela são atualizados a partir da visão.
