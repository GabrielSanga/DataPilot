# configuracoes Specification

## Purpose
Permite ver, numa tela do próprio painel, a pasta de status das sessões de IA em uso e ajustar o nome do container em que os agentes rodam, sem recompilar.

## Requirements

### Requirement: Tela de configurações
O painel SHALL oferecer, na janela principal, a ação "Configurações", que abre um diálogo com a pasta de status das sessões de IA em uso, só para leitura, e o nome do container. O diálogo SHALL NOT oferecer escolha de ambiente de execução nem opções de SSH. "Salvar" grava e fecha; "Cancelar" e Esc fecham sem gravar.

#### Scenario: Abrir as configurações
- **WHEN** o usuário aciona "Configurações"
- **THEN** o diálogo mostra a pasta de status em uso e o nome do container em uso

#### Scenario: Pasta só para leitura
- **WHEN** o diálogo de configurações está aberto
- **THEN** a pasta de status pode ser lida e copiada, mas não alterada

#### Scenario: Windows escolhido
- **WHEN** o arquivo de dados tem `execucao` igual a `windows`, gravado por uma versão anterior, e o usuário aciona "Configurações"
- **THEN** o diálogo mostra só a pasta de status e o nome do container, com o nome do container editável

#### Scenario: SSH desligado
- **WHEN** o arquivo de dados tem `usarSsh` igual a `false`, gravado por uma versão anterior, e o usuário aciona "Configurações"
- **THEN** o diálogo não mostra "Abrir pelo SSH" nem o usuário do SSH

#### Scenario: Cancelar
- **WHEN** o usuário altera o nome do container e aciona "Cancelar"
- **THEN** nada é gravado

### Requirement: Valores padrão
Sem configuração gravada, o painel SHALL usar `datapilot-agente` como nome do container.

#### Scenario: Primeira execução
- **WHEN** o arquivo de dados não tem configurações
- **THEN** o diálogo mostra o container `datapilot-agente`

#### Scenario: Configurações de uma versão anterior
- **WHEN** o arquivo de dados tem `execucao`, `usarSsh` e `usuarioSsh` gravados na seção `configuracoes`
- **THEN** o painel desconsidera esses campos e usa só o `container` gravado

### Requirement: Configurações gravadas no arquivo de dados
As configurações SHALL ser gravadas na seção `configuracoes` do arquivo de dados do painel, só com o campo `container`, com a gravação segura dos trabalhos. Ao carregar, um `container` ausente ou inválido SHALL valer o padrão, e os campos de versões anteriores (`execucao`, `usarSsh` e `usuarioSsh`) SHALL ser desconsiderados e deixar de existir na próxima gravação. Com o arquivo de dados ilegível, o painel SHALL usar os padrões e o diálogo SHALL NOT gravar.

#### Scenario: Usuário inválido no arquivo
- **WHEN** o arquivo de dados tem `usuarioSsh` igual a `-oProxyCommand=calc`
- **THEN** o painel desconsidera o campo, e nada dele entra em comandos

#### Scenario: Nome de container inválido no arquivo
- **WHEN** o arquivo de dados tem `container` igual a `--privileged`
- **THEN** o painel usa o container `datapilot-agente`

#### Scenario: Ambiente desconhecido no arquivo
- **WHEN** o arquivo de dados tem `execucao` igual a `wsl` ou `windows`
- **THEN** o painel desconsidera o campo e as sessões novas abrem no container

#### Scenario: Campos antigos removidos ao salvar
- **WHEN** o arquivo de dados tem `usarSsh` e `usuarioSsh` na seção `configuracoes` e o usuário salva as configurações
- **THEN** a seção `configuracoes` passa a ter só o `container`

#### Scenario: Arquivo de dados ilegível
- **WHEN** o arquivo de dados existe e não pode ser lido
- **THEN** o painel usa os valores padrão e o diálogo informa que não é possível salvar

### Requirement: Validação do nome do container
O nome do container SHALL ter de 1 a 63 caracteres entre letras, dígitos, `_`, `.` e `-`, e SHALL começar com letra ou dígito. Um valor fora da regra SHALL ser recusado com o erro junto do campo, sem gravar nada.

#### Scenario: Nome que vira opção do docker
- **WHEN** o usuário informa `--privileged` como nome do container e salva
- **THEN** o erro aparece junto do campo e nada é gravado

#### Scenario: Nome vazio
- **WHEN** o nome do container está vazio e o usuário salva
- **THEN** o erro aparece junto do campo e nada é gravado

### Requirement: Quando o nome do container passa a valer
O nome do container SHALL valer a partir da próxima sessão nova, lançamento aberto de novo ou retomada, sem reabrir o painel.

#### Scenario: Trocar o nome do container
- **WHEN** o usuário troca o nome do container para `agente_1`, salva e abre uma sessão nova
- **THEN** a aba chama o `docker exec` no container `agente_1`

#### Scenario: Retomar depois de trocar o nome
- **WHEN** o usuário troca o nome do container, salva e aciona "Retomar" numa sessão registrada
- **THEN** a aba retoma a conversa no container com o nome novo
