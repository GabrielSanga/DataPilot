# Proposal

## Why

Com a change `harness-em-container`, os agentes rodam num container Docker, que isola melhor do que o modo Windows: não tem rota para a rede da empresa e não vê o perfil nem as credenciais. Hoje o painel mantém dois caminhos em paralelo, cada um com scripts, testes, opções e documentação próprios. São eles o modo Windows pela conta principal e o modo Windows pela conta comum `ia` via SSH local. Manter os dois custa caro e deixa aberta uma forma menos segura de rodar os agentes. Esta change remove o modo Windows por completo e deixa o container como o único ambiente.

## What Changes

- **BREAKING:** o painel passa a abrir e retomar sessões só no container. As duas formas de abrir no Windows deixam de existir: direto na conta principal e pelo SSH local com o usuário escolhido.
- **Configurações:** saem "Executar os agentes em", "Abrir pelo SSH" e o usuário do SSH. A tela mantém a pasta de status, só para leitura, e o nome do container. Os campos `execucao`, `usarSsh` e `usuarioSsh` da seção `configuracoes` passam a ser ignorados na carga e somem na próxima gravação.
- **BREAKING (dados):** sessões registradas e lançamentos pendentes sem `execucao`, ou com `execucao` igual a `windows`, passam a ser desconsiderados na carga. As conversas deles estão no perfil do Windows e não podem ser retomadas no container. Os registros continuam no arquivo, sem aparecer no painel. O painel continua gravando `execucao` igual a `container` e passa a exigir esse valor.
- **Arquivos de status do Windows:** um arquivo com `cwd` que não comece com `/` passa a ser ignorado. Assim, uma sessão antiga do Windows não reaparece como sessão não registrada, com um "Retomar" que falharia.
- **Data de início:** passa a vir só do marcador `<session_id>.criado`. A data de criação do próprio arquivo deixa de servir de alternativa, porque só o hook do Windows a preservava.
- **Associação ao repositório:** o `cwd` passa a ser comparado só com o caminho no container. A comparação com o caminho do Windows (`C:\Fontes\...`) sai.
- **Vincular repositório:** a pasta escolhida precisa estar em `C:\Fontes` ou `C:\openspec\ERP`, os locais que o container enxerga. Uma pasta fora deles é recusada já na escolha, e não só ao abrir a sessão.
- **Scripts do Windows:** o painel deixa de instalar e atualizar o `abrir.ps1` e o `hook.ps1` em `C:\Painel`. Saem os scripts e a verificação deles, os exemplos de configuração dos hooks do Windows, o parâmetro `--pasta-scripts` e o aviso de falha na instalação.
- **Mensagens:** as orientações deixam de sugerir "escolha Windows nas configurações".
- **Documentação:** sai o guia de instalação do modo Windows (`docs/instalacao-etapa-1.md`). PRD, arquitetura, desenvolvimento, README e o guia do container perdem as referências ao modo Windows. O guia do container ganha uma seção opcional para desfazer a instalação antiga do Windows.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `scripts-do-painel`: a capability inteira é removida.
- `abertura-de-sessao`: saem a chamada pelo SSH, a chamada direta e a proteção de `C:\Painel`. As regras de abertura passam a descrever só o `abrir.sh` do container.
- `configuracoes`: saem o ambiente de execução e as opções de SSH. Ficam a pasta de status e o nome do container.
- `lancamento-de-sessao`: saem o comando do Windows e a escolha de ambiente por sessão. Nova sessão, retomada e locais passam a descrever só o container.
- `dados-do-painel`: o `execucao` igual a `container` passa a ser obrigatório nos registros, e o registro antigo ou do Windows é desconsiderado.
- `status-de-sessao`: saem as permissões da instalação do Windows e a preservação da data de criação pelo hook do Windows. O marcador `.criado` passa a ser a única fonte da data de início.
- `leitura-de-status`: o `cwd` passa a ser obrigatório e precisa começar com `/`. A data de início vem só do marcador.
- `demandas`: "Vincular repositório" aceita só pastas dentro dos locais do container.
- `lista-de-sessoes`: o exemplo da lista passa a usar uma pasta de trabalho do container, porque um `cwd` do Windows não gera mais sessão.
- `vinculo-de-sessao`: "Retomar sessão não registrada" passa a valer só para sessões do container. Isso é consequência do filtro de `cwd`, e o requisito não muda de texto. Por isso não há delta.

## Impact

- **Código (Core):**
  - `Models`: `Configuracoes`, `Execucao`/`Execucoes`, `Demanda` (`SessaoRegistrada`, `LancamentoPendente`), `Constantes`, `LocaisContainer`;
  - `ValidadorEntrada`;
  - `MontadorComando`, `DisparadorContainer`, `AberturaSessao`;
  - `DadosPainel` (configurações, sessões, lançamentos e a leitura do `execucao`);
  - `VinculoAutomatico`, `PedidoVinculo`, `ValidadorStatus`, `LeitorPasta`, `EscolhaDePasta`;
  - `ScriptsDoPainel`, que é removido, e os recursos embutidos no `.csproj`.
- **Código (WPF):** `App.xaml.cs` (instalação dos scripts e `--pasta-scripts`), `MainViewModel` (`AvisarScripts`) e `ConfiguracoesWindow`.
- **Arquivos removidos:** `scripts/abrir.ps1`, `scripts/hook.ps1`, `scripts/testes/verificar.ps1`, `scripts/exemplos/claude-settings.json`, `scripts/exemplos/codex-hooks.json`, `docs/instalacao-etapa-1.md`.
- **Arquivos ajustados:** `scripts/exemplos/gerar-sessoes-exemplo.ps1` (cwd no container e marcadores `.criado`). Nos comentários do `docker/painel/abrir.sh`, `hook.mjs` e `docker/testes/verificar.sh`, eles deixam de se referir aos scripts do Windows.
- **Testes:**
  - removidos: SSH, usuário do SSH e scripts do painel;
  - ajustados: montador, caminho, container, configurações, ambiente nos registros, leitor da pasta, validador de status, escolha de pasta e vínculo.
- **Dados:** o `demandas.json` não é migrado. As sessões e os lançamentos do Windows continuam no arquivo, invisíveis. As chaves antigas de configuração somem na próxima gravação.
- **Máquina do usuário:** o OpenSSH Server, o usuário `ia`, `C:\Painel` e as permissões especiais de `C:\ProgramData\Painel` continuam instalados. Desfazer isso é manual e opcional, como o guia vai orientar.
- **Fora do escopo:** a change `terminal-embutido`, que hoje prevê sessões do Windows no Windows Terminal, será ajustada antes da implementação dela.
