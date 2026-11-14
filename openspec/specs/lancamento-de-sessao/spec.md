# lancamento-de-sessao Specification
## Purpose

Abre sessões novas e retoma sessões conhecidas do Claude Code e do Codex a partir do painel, em abas do Windows Terminal que rodam como o usuário isolado `ia`, usando apenas dados fornecidos pelo usuário principal.

## Requirements
### Requirement: Nova sessão
O painel SHALL oferecer a ação de abrir uma sessão nova, em que o usuário escolhe um repositório cadastrado, o agente e, opcionalmente, um rótulo. A ação SHALL abrir uma aba do Windows Terminal que inicia o agente nesse repositório como o usuário `ia`. Sem repositórios cadastrados, a ação SHALL orientar o usuário a cadastrar um.

#### Scenario: Nova sessão do Claude
- **WHEN** o usuário escolhe o repositório `Faturamento` e o agente Claude e confirma
- **THEN** abre uma aba do Windows Terminal com o Claude Code iniciado em `Faturamento`, rodando como `ia`

#### Scenario: Nova sessão do Codex
- **WHEN** o usuário escolhe o repositório `Faturamento` e o agente Codex e confirma
- **THEN** abre uma aba do Windows Terminal com o Codex iniciado em `Faturamento`, rodando como `ia`

#### Scenario: Nenhum repositório cadastrado
- **WHEN** o usuário aciona "Nova sessão" sem ter repositórios cadastrados
- **THEN** o painel informa que é preciso cadastrar um repositório e não abre nenhuma aba

### Requirement: Identificador gerado pelo painel
Para cada sessão nova do Claude, o painel SHALL gerar um GUID novo e iniciar o agente com esse identificador.

#### Scenario: Identificador da sessão
- **WHEN** o usuário abre uma sessão nova do Claude
- **THEN** o arquivo de status que surge para essa sessão tem como nome o GUID que o painel gerou e registrou

### Requirement: Retomada de sessão registrada
O painel SHALL retomar uma sessão registrada abrindo uma aba do Windows Terminal que retoma a conversa de mesmo identificador, no repositório em que a sessão foi registrada.

#### Scenario: Retomar sessão do Claude
- **WHEN** o usuário aciona "Retomar" numa sessão do Claude registrada
- **THEN** abre uma aba com a conversa anterior, com o histórico, no repositório registrado

### Requirement: Composição do comando
O painel SHALL iniciar `wt.exe` com os argumentos, nesta ordem e cada um separado: `-w`, `painel`, `nt`, `--title`, o título, `--suppressApplicationTitle`, `ssh`, `-t`, `ia@localhost`, `powershell`, `-NoProfile`, `-ExecutionPolicy`, `Bypass`, `-File`, `C:\Painel\abrir.ps1`, `-Agente`, o agente, `-Repo`, a pasta, `-Sessao` e o identificador quando houver, `-Modo` e o modo. Nenhum shell SHALL intermediar a chamada.

#### Scenario: Retomada do Claude
- **WHEN** o painel retoma a sessão `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10` do Claude em `Faturamento`
- **THEN** os argumentos terminam em `-Agente`, `claude`, `-Repo`, `Faturamento`, `-Sessao`, `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10`, `-Modo`, `retomar`

#### Scenario: Nova sessão do Codex
- **WHEN** o painel abre uma sessão nova do Codex em `Faturamento`
- **THEN** os argumentos terminam em `-Agente`, `codex`, `-Repo`, `Faturamento`, `-Modo`, `novo`, sem `-Sessao`

#### Scenario: Abas reunidas
- **WHEN** o painel abre duas sessões
- **THEN** as duas abas ficam na mesma janela do Windows Terminal, de nome `painel`

### Requirement: Somente dados do painel entram no comando
Os valores de agente, pasta e identificador usados no comando SHALL vir do arquivo de dados do painel ou da escolha do usuário na tela. Nenhum valor lido de um arquivo de status SHALL entrar em argumentos, no título ou na decisão de qual pasta usar.

#### Scenario: Pasta de trabalho divergente no arquivo de status
- **WHEN** uma sessão registrada em `Faturamento` tem, no arquivo de status, um `cwd` que aponta para outra pasta
- **THEN** a retomada usa `Faturamento` como `-Repo`

### Requirement: Validação imediatamente antes de lançar
Antes de iniciar o terminal, o painel SHALL validar de novo o agente (`claude` ou `codex`), a pasta (regra do cadastro) e o identificador (GUID canônico). Se algum valor for inválido, o painel SHALL NOT iniciar nenhum processo e SHALL informar o usuário.

#### Scenario: Valor inválido chega ao lançamento
- **WHEN** a pasta ou o identificador de uma sessão não passa na validação no momento de lançar
- **THEN** nenhum processo é iniciado e o painel mostra uma mensagem de erro

### Requirement: Título da aba e rótulo
O título da aba SHALL ser o nome do agente e o repositório, separados por " · ", seguido do rótulo quando houver. O rótulo SHALL ter no máximo 40 caracteres e SHALL NOT conter `;` nem caracteres de controle; um rótulo fora dessa regra SHALL ser recusado na tela.

#### Scenario: Sessão sem rótulo
- **WHEN** o usuário abre uma sessão do Claude em `Faturamento` sem rótulo
- **THEN** a aba tem o título "Claude · Faturamento"

#### Scenario: Sessão com rótulo
- **WHEN** o usuário abre uma sessão do Claude em `Faturamento` com o rótulo "implementação"
- **THEN** a aba tem o título "Claude · Faturamento · implementação"

#### Scenario: Rótulo com ponto e vírgula
- **WHEN** o usuário informa um rótulo contendo `;`
- **THEN** o painel recusa o rótulo e não abre a aba

### Requirement: Aviso de sessão possivelmente aberta
Ao retomar uma sessão cujo status exibido não seja "encerrada" nem "inativa", o painel SHALL avisar que ela pode já estar aberta no terminal e SHALL abrir a aba somente se o usuário confirmar.

#### Scenario: Sessão em andamento
- **WHEN** o usuário aciona "Retomar" numa sessão exibida como "aguardando você"
- **THEN** o painel pede confirmação e só abre a aba se o usuário confirmar

#### Scenario: Sessão encerrada
- **WHEN** o usuário aciona "Retomar" numa sessão exibida como "encerrada"
- **THEN** a aba abre sem pedido de confirmação

### Requirement: Falha ao iniciar o terminal
Se o Windows Terminal não puder ser iniciado, o painel SHALL mostrar uma mensagem que diga o que falhou e SHALL continuar funcionando.

#### Scenario: Windows Terminal ausente
- **WHEN** o usuário aciona "Nova sessão" numa máquina sem o Windows Terminal
- **THEN** o painel mostra a mensagem de erro, não fecha, e a lista de sessões continua atualizando
