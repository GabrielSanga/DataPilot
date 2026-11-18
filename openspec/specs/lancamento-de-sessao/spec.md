# lancamento-de-sessao Specification
## Purpose

Abre sessões novas e retoma sessões conhecidas do Claude Code e do Codex a partir do painel, em abas do Windows Terminal que rodam como o usuário isolado `ia`, usando apenas dados fornecidos pelo usuário principal.

## Requirements
### Requirement: Nova sessão
O painel SHALL oferecer, no card de cada repositório de uma demanda, a ação de abrir uma sessão nova, em que o usuário escolhe o agente e, opcionalmente, um rótulo. A ação SHALL abrir uma aba do Windows Terminal que inicia o agente nesse repositório como o usuário `ia`. Uma sessão nova do Claude SHALL ser registrada nesse repositório dessa demanda.

#### Scenario: Nova sessão do Claude
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento` da demanda `CHG-142`, escolhe o agente Claude e confirma
- **THEN** abre uma aba do Windows Terminal com o Claude Code iniciado em `Faturamento`, rodando como `ia`, e a sessão fica registrada em `Faturamento` dentro de `CHG-142`

#### Scenario: Nova sessão do Codex
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento`, escolhe o agente Codex e confirma
- **THEN** abre uma aba do Windows Terminal com o Codex iniciado em `Faturamento`, rodando como `ia`

#### Scenario: Nenhum repositório cadastrado
- **WHEN** a demanda selecionada não tem repositórios vinculados
- **THEN** nenhuma ação de "Nova sessão" é oferecida e o painel orienta a vincular um repositório

#### Scenario: Demanda arquivada
- **WHEN** o usuário visualiza uma demanda arquivada
- **THEN** os cards dela não oferecem "Nova sessão"

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
O título da aba SHALL ser o `id` da demanda e o repositório, separados por " · ", seguido do rótulo quando houver. Para sessões da demanda `sem-demanda`, o nome do agente SHALL ocupar o lugar do `id`. O rótulo SHALL ter no máximo 40 caracteres e SHALL NOT conter `;` nem caracteres de controle; um rótulo fora dessa regra SHALL ser recusado na tela.

#### Scenario: Sessão sem rótulo
- **WHEN** o usuário abre uma sessão em `Faturamento`, na demanda `CHG-142`, sem rótulo
- **THEN** a aba tem o título "CHG-142 · Faturamento"

#### Scenario: Sessão com rótulo
- **WHEN** o usuário abre uma sessão em `Faturamento`, na demanda `CHG-142`, com o rótulo "implementação"
- **THEN** a aba tem o título "CHG-142 · Faturamento · implementação"

#### Scenario: Sessão da demanda "Sem demanda"
- **WHEN** o usuário retoma uma sessão do Claude registrada em `Faturamento`, na demanda `sem-demanda`
- **THEN** a aba tem o título "Claude · Faturamento"

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

### Requirement: Abrir de novo um lançamento pendente
O painel SHALL permitir abrir de novo o agente de um lançamento pendente, pela ação "Abrir" da linha dele, lançando uma sessão nova desse agente no repositório do lançamento, com o rótulo dele e sem registrar outro lançamento. A data do lançamento SHALL passar a ser a dessa abertura. Se o terminal não puder ser iniciado, a data anterior SHALL ser mantida e a falha SHALL ser informada. O comando SHALL ser composto como o de uma sessão nova, só com dados do arquivo de dados.

#### Scenario: Aba fechada sem mensagem
- **WHEN** o usuário abre uma sessão nova do Codex pelo card, fecha a aba sem enviar mensagem e aciona "Abrir" na linha do lançamento
- **THEN** abre uma aba do Codex nesse repositório, com o mesmo título, e o card continua com uma só linha para esse lançamento, com o tempo decorrido recomeçando

#### Scenario: Primeira mensagem depois de abrir de novo
- **WHEN** o usuário envia a primeira mensagem na aba aberta por "Abrir"
- **THEN** a sessão é registrada no card com o rótulo do lançamento e a linha deixa de aparecer

#### Scenario: Terminal não inicia ao abrir de novo
- **WHEN** o usuário aciona "Abrir" e o terminal não pode ser iniciado
- **THEN** o painel informa a falha e o lançamento permanece no arquivo de dados com a data anterior

#### Scenario: Lançamento que não existe mais
- **WHEN** o usuário aciona "Abrir" num lançamento que outra janela do painel acabou de usar ou cancelar
- **THEN** nenhuma aba é aberta, o arquivo de dados não é alterado e a linha some na atualização seguinte

#### Scenario: Lançamento vencido
- **WHEN** o usuário aciona "Abrir" num lançamento que passou das 24 horas enquanto a tela estava aberta
- **THEN** nenhuma aba é aberta e o arquivo de dados não é alterado
