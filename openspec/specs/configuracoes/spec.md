# configuracoes Specification

## Purpose
Permite ver, numa tela do próprio painel, a pasta de status das sessões de IA em uso e ajustar a forma de abrir os agentes (pelo SSH local, com o usuário escolhido, ou direto), sem recompilar.

## Requirements

### Requirement: Tela de configurações
O painel SHALL oferecer, na janela principal, a ação "Configurações", que abre um diálogo com a pasta de status das sessões de IA em uso, só para leitura; "Executar os agentes em", com as opções "Container Docker" e "Windows"; o nome do container; "Abrir pelo SSH" e o usuário do SSH. O nome do container SHALL ficar desabilitado quando "Windows" estiver escolhido. "Abrir pelo SSH" e o usuário do SSH SHALL continuar editáveis com "Container Docker" escolhido, porque valem para retomar as sessões abertas no Windows. O usuário do SSH SHALL ficar desabilitado quando "Abrir pelo SSH" estiver desligado. "Salvar" grava e fecha; "Cancelar" e Esc fecham sem gravar.

#### Scenario: Abrir as configurações
- **WHEN** o usuário aciona "Configurações"
- **THEN** o diálogo mostra a pasta de status em uso, o ambiente de execução, o nome do container, "Abrir pelo SSH" e o usuário do SSH com os valores em uso

#### Scenario: Pasta só para leitura
- **WHEN** o diálogo de configurações está aberto
- **THEN** a pasta de status pode ser lida e copiada, mas não alterada

#### Scenario: Windows escolhido
- **WHEN** o usuário escolhe "Windows" em "Executar os agentes em"
- **THEN** o campo do nome do container fica desabilitado

#### Scenario: SSH desligado
- **WHEN** o usuário desliga "Abrir pelo SSH" no diálogo
- **THEN** o campo do usuário do SSH fica desabilitado

#### Scenario: Cancelar
- **WHEN** o usuário altera o nome do container e aciona "Cancelar"
- **THEN** nada é gravado

### Requirement: Valores padrão
Sem configuração gravada, o painel SHALL usar "Container Docker" como ambiente de execução, `datapilot-agente` como nome do container, "Abrir pelo SSH" ligado e o usuário `ia`.

#### Scenario: Primeira execução
- **WHEN** o arquivo de dados não tem configurações
- **THEN** o diálogo mostra "Container Docker", o container `datapilot-agente`, "Abrir pelo SSH" ligado e o usuário `ia`

#### Scenario: Configurações de uma versão anterior
- **WHEN** o arquivo de dados tem `usarSsh` e `usuarioSsh` gravados e não tem `execucao`
- **THEN** o painel usa "Container Docker" e mantém os valores de SSH gravados

### Requirement: Validação do usuário do SSH
O usuário do SSH SHALL ter de 1 a 20 caracteres entre letras, dígitos, `_`, `.` e `-`, e SHALL NOT começar com `-` nem `.`. Um valor fora da regra SHALL ser recusado com o erro junto do campo, sem gravar nada.

#### Scenario: Usuário que vira opção do ssh
- **WHEN** o usuário informa `-oProxyCommand=calc` como usuário do SSH e salva
- **THEN** o erro aparece junto do campo e nada é gravado

#### Scenario: Usuário vazio com SSH ligado
- **WHEN** "Abrir pelo SSH" está ligado, o usuário do SSH está vazio e o usuário salva
- **THEN** o erro aparece junto do campo e nada é gravado

### Requirement: Configurações gravadas no arquivo de dados
As configurações SHALL ser gravadas na seção `configuracoes` do arquivo de dados do painel (`execucao`, com `container` ou `windows`; `container`; `usarSsh`; `usuarioSsh`), com a gravação segura dos trabalhos. Ao carregar, um campo ausente ou inválido SHALL valer o padrão. Com o arquivo de dados ilegível, o painel SHALL usar os padrões e o diálogo SHALL NOT gravar.

#### Scenario: Usuário inválido no arquivo
- **WHEN** o arquivo de dados tem `usuarioSsh` igual a `-oProxyCommand=calc`
- **THEN** o painel usa o usuário `ia`

#### Scenario: Nome de container inválido no arquivo
- **WHEN** o arquivo de dados tem `container` igual a `--privileged`
- **THEN** o painel usa o container `datapilot-agente`

#### Scenario: Ambiente desconhecido no arquivo
- **WHEN** o arquivo de dados tem `execucao` igual a `wsl`
- **THEN** o painel usa "Container Docker"

#### Scenario: Arquivo de dados ilegível
- **WHEN** o arquivo de dados existe e não pode ser lido
- **THEN** o painel usa os valores padrão e o diálogo informa que não é possível salvar

### Requirement: Quando as configurações passam a valer
O ambiente de execução e o nome do container SHALL valer a partir da próxima sessão nova ou lançamento aberto de novo, sem reabrir o painel. O nome do container, "Abrir pelo SSH" e o usuário do SSH SHALL valer também a partir da próxima retomada, sempre no ambiente em que a sessão foi registrada.

#### Scenario: Trocar para Windows
- **WHEN** o usuário escolhe "Windows", salva e abre uma sessão nova
- **THEN** a aba chama o `abrir.ps1`, pelo SSH ou direto conforme "Abrir pelo SSH", e não usa o container

#### Scenario: Retomar sessão do Windows com o container escolhido
- **WHEN** "Container Docker" está escolhido e o usuário retoma uma sessão registrada no Windows
- **THEN** a aba retoma no Windows, com as opções de SSH em uso

#### Scenario: Desligar o SSH
- **WHEN** o usuário desliga "Abrir pelo SSH", salva e aciona "Retomar" numa sessão registrada no Windows
- **THEN** a aba chama o `abrir.ps1` direto, sem `ssh`

### Requirement: Validação do nome do container
O nome do container SHALL ter de 1 a 63 caracteres entre letras, dígitos, `_`, `.` e `-`, e SHALL começar com letra ou dígito. Um valor fora da regra SHALL ser recusado com o erro junto do campo, sem gravar nada.

#### Scenario: Nome que vira opção do docker
- **WHEN** o usuário informa `--privileged` como nome do container e salva
- **THEN** o erro aparece junto do campo e nada é gravado

#### Scenario: Nome vazio
- **WHEN** o nome do container está vazio e o usuário salva
- **THEN** o erro aparece junto do campo e nada é gravado
