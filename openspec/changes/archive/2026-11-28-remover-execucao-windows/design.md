# Design

## Context

A motivação está em proposal.md. Esta change parte do estado deixado por `harness-em-container`, já arquivada:

- **Configuração:** `Configuracoes(Execucao, Container, UsarSsh, UsuarioSsh)`, em que `Execucao` é o enum `{ Container, Windows }`. O ambiente decide o ramo do `MontadorComando`: `docker exec … abrir.sh` ou `ssh`/`powershell … abrir.ps1`.
- **Registros:** `SessaoRegistrada` e `LancamentoPendente` têm `Execucao`, com padrão `Windows`. Na carga, `TentarLerExecucao` trata um registro sem o campo como `windows` e desconsidera um valor desconhecido. A gravação mexe só nos nós que conhece, então um registro desconsiderado continua no arquivo.
- **Disparo:** `ComandoTerminal.Container` é `null` no Windows, e o `DisparadorContainer` só verifica o Docker quando há container.
- **Scripts do Windows:** o `ScriptsDoPainel` instala o `abrir.ps1` e o `hook.ps1`, que vão embutidos no `.csproj`, em `C:\Painel` ao abrir o painel. Ele é chamado pelo `App.xaml.cs`, que lê `--pasta-scripts`.
- **Status:**
  - o `ValidadorStatus` aceita qualquer `cwd`, inclusive ausente;
  - o `LeitorPasta` usa a data do marcador `.criado` e, sem ele, a data de criação do arquivo;
  - o `PedidoVinculo.PastaCoincide` compara o `cwd` com o caminho do Windows e com o caminho no container.
- **Escolha de pasta:** o `EscolhaDePasta` aceita qualquer pasta local com caminho válido. Só o `AberturaSessao.NovaEmSessoes` recusa, e só no container, uma pasta fora de `LocaisContainer`.
- **Dados reais:** o `demandas.json` em uso tem cerca de 48 registros do Windows (sem `execucao`) e uns poucos do container.

## Goals / Non-Goals

**Goals:**

- Um único caminho de abertura, sem nenhum ramo, opção, script ou texto do modo Windows.
- Descartar os registros e os arquivos de status do Windows sem apagar nada do arquivo de dados nem da pasta de status.
- Manter a forma atual do código (record `Configuracoes`, `ComandoTerminal`, `IDisparador`) para o diff ficar pequeno e a change `terminal-embutido` continuar se encaixando.

**Non-Goals:**

- Migrar ou limpar o `demandas.json`.
- Apagar arquivos antigos da pasta de status.
- Desinstalar o OpenSSH Server, o usuário `ia` ou `C:\Painel`. O guia descreve como fazer, e cada usuário decide.
- Ajustar a change `terminal-embutido`.
- Trocar o Windows Terminal pelo terminal embutido.

## Decisions

### 1. O campo `execucao` continua no arquivo, e o enum sai do modelo

O painel continua gravando `"execucao": "container"` em toda sessão e todo lançamento, e a carga aceita só esse valor. O enum `Execucao`, o `Execucoes` e as propriedades `Execucao` de `SessaoRegistrada`, `LancamentoPendente` e `Configuracoes` saem. O texto `container` vira uma constante usada só pela persistência.

- **Por que manter o campo:** sem ele, um registro novo ficaria igual a um registro antigo do Windows. A única forma de distinguir seria tratar a ausência como container, e aí os 48 registros antigos voltariam ao painel com um "Retomar" que sempre falha.
- **Alternativa rejeitada: apagar os registros antigos na primeira gravação.** Seria destrutivo, sem ganho para quem usa, e iria contra a regra de preservar o que o painel não usa. Também impediria voltar à versão anterior para retomar uma conversa do Windows à mão.
- **Alternativa rejeitada: manter o enum com um só valor.** Seria código que não decide nada.

### 2. `Configuracoes` fica com um só campo

A forma passa a ser `record Configuracoes(string Container)`, com `Padrao` igual a `datapilot-agente`. Fica como record, em vez de virar uma string solta, porque já é o que passa pelo `MontadorComando`, pela `AberturaSessao` e pela tela. O `SalvarConfiguracoes` grava um objeto novo só com `container`, e por isso as chaves antigas somem sozinhas na próxima gravação. A extração lê só `container`.

### 3. Um único caminho de abertura

- **`MontadorComando`:** perde o ramo Windows, e o `NoContainer` vira o único `Montar`. O `Retomar` deixa de sobrescrever o ambiente com o do registro.
- **`ComandoTerminal.Container`:** passa a ser obrigatório. Isso fica no tipo, em vez de cada chamador lembrar.
- **`DisparadorContainer`:** verifica sempre e perde o texto "ou escolha Windows".
- **`AberturaSessao`:** deixa de repassar o ambiente nos quatro fluxos. Em `NovaEmSessoes`, a recusa de pasta fora dos locais vira incondicional. Ela continua necessária para repositórios que uma versão anterior vinculou fora dos locais.
- **`LocaisContainer.MensagemFora`:** passa a só listar os dois locais.

### 4. Arquivos de status do Windows filtrados na validação

O `ValidadorStatus` passa a exigir `cwd` como texto que começa com `/`. Assim, o arquivo do Windows não vira sessão e não chega a nenhuma regra seguinte (lista, "Mostrar antigas", vínculo, "Retomar").

- **Alternativa rejeitada: filtrar só na tela.** Deixaria a sessão existir para o vínculo automático e para as contagens.
- **Por que exigir o campo:** o `hook.mjs` sempre grava `cwd`, então um arquivo sem ele não veio do painel.

### 5. Data de início só pelo marcador

Sem o marcador `.criado`, o `LeitorPasta` deixa `ArquivoCriadoEm` nulo. Hoje ele usa a data de criação do arquivo. Com a data nula, o `VinculoAutomatico` ignora a sessão, como já faz.

- **Por que é seguro:** o `hook.mjs` cria o marcador antes do primeiro status e não grava o status se não conseguir criá-lo.
- **Por que tirar a alternativa:** a data de criação do arquivo não é confiável na montagem do container, e mantê-la convidaria a usá-la de novo.

### 6. Associação pelo caminho no container

O `PastaCoincide` perde a comparação direta do `cwd` com o caminho do Windows. Ficam dois casos:

- repositório cadastrado pelo nome: compara o último segmento do `cwd`;
- repositório com caminho completo: compara com `LocaisContainer.Traduzir`.

O `gerar-sessoes-exemplo.ps1` passa a gravar `cwd` em `/workspace/<pasta>` e a criar o marcador `.criado` de cada sessão. Sem isso, os exemplos deixariam de gerar sessões (decisão 4) e de ter data de início (decisão 5).

### 7. Recusa ao vincular

O `EscolhaDePasta.Validar` mantém o caso da pasta direta da raiz configurada, gravada pelo nome. Esse caso continua valendo com `--raiz-fontes` apontando para uma raiz de teste. Para o caminho completo, ele passa a exigir `LocaisContainer.Traduzir(completo) != null` e recusa com `MensagemFora`. A verificação de atalho e junção continua. O comentário que cita o `abrir.ps1` passa a citar o `abrir.sh`, que também recusa atalhos.

### 8. Remoção dos scripts do Windows

Saem as seguintes peças:

- `ScriptsDoPainel` e seus testes;
- os dois `EmbeddedResource` do `.csproj`;
- a chamada no `App.xaml.cs` e o `--pasta-scripts`;
- o `AvisarScripts` do `MainViewModel`;
- `scripts/abrir.ps1`, `scripts/hook.ps1` e `scripts/testes/verificar.ps1`;
- `scripts/exemplos/claude-settings.json` e `scripts/exemplos/codex-hooks.json`.

A pasta `scripts/` fica só com `exemplos/gerar-sessoes-exemplo.ps1`. Os comentários de `docker/painel/abrir.sh`, `hook.mjs` e `docker/testes/verificar.sh` passam a descrever as regras sem citar os scripts do Windows.

### 9. Tela de configurações

O `ConfiguracoesWindow` fica com a pasta de status, só para leitura, e o nome do container, sempre habilitado e com o erro junto do campo. Saem os dois `RadioButton`, o `CheckBox` do SSH e o campo de usuário.

### 10. Guia do container

- **Seção "Voltar para o Windows":** dá lugar a "Remover a instalação antiga do modo Windows (opcional)". O conteúdo:
  - desinstalar o OpenSSH Server;
  - remover o usuário `ia` e o perfil dele;
  - apagar `C:\Painel`;
  - apagar os hooks do perfil do `ia`.

  O guia avisa para não apagar `C:\ProgramData\Painel\sessoes`, que o container continua usando. A reversão do guia antigo apagava essa pasta inteira.
- **Parágrafo da permissão da pasta de status:** deixa de citar o guia removido.

## Risks / Trade-offs

- **[As sessões antigas somem dos cards]** → É a decisão do usuário. Os registros continuam no arquivo, e voltar à versão anterior os mostra de novo. A retomada no container falharia de qualquer forma.
- **[Repositórios vinculados fora de `C:\Fontes` e `C:\openspec\ERP` continuam aparecendo, mas não abrem]** → A mensagem informa os dois locais. O usuário remove o repositório e vincula de novo num dos locais. A tarefa de verificação confere o caso.
- **[Arquivo de status do container sem marcador, de um hook anterior à `harness-em-container`]** → Não existe, porque o hook do container sempre criou o marcador. No pior caso, a sessão aparece mas não é vinculada automaticamente, e o usuário pode vinculá-la pelo diálogo.
- **[Muitos testes usam `cwd` do Windows (`C:\Fontes\...`) nas fixtures]** → Elas precisam passar para `/workspace/...`. O ajuste é mecânico, mas espalhado. A tarefa de testes lista os arquivos.
- **[Quem ainda usa o modo Windows perde a forma de abrir]** → É uma mudança que quebra compatibilidade, de propósito. O guia do container explica a instalação.
- **[Configuração de raiz de teste (`--raiz-fontes`) diferente de `C:\Fontes`]** → Pastas diretas da raiz de teste continuam vinculando pelo nome. Um caminho completo fora de `C:\Fontes` e `C:\openspec\ERP` passa a ser recusado também em desenvolvimento, o que é coerente com o container.

## Migration Plan

1. Atualizar o painel. Nada é migrado: as configurações antigas valem só pelo `container`, e os registros do Windows ficam invisíveis.
2. Opcional: seguir a seção nova do guia do container para remover o OpenSSH Server, o usuário `ia` e `C:\Painel`.

**Volta atrás:** voltar à versão anterior do painel. Os registros do Windows reaparecem, porque nunca foram apagados. As configurações gravadas pela versão nova só têm `container`, então a anterior usa o container e os padrões de SSH. A retomada no Windows só funciona se a instalação do modo Windows ainda existir.
