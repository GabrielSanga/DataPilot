# Spec Delta

## Purpose

Permite ajustar, numa tela do próprio painel, a pasta de status das sessões de IA e a forma de abrir os agentes (pelo SSH local, com o usuário escolhido, ou direto), sem recompilar nem usar parâmetros de linha de comando.

## ADDED Requirements

### Requirement: Tela de configurações
O painel SHALL oferecer, na janela principal, a ação "Configurações", que abre um diálogo com a pasta de status das sessões de IA (com escolha de pasta do Windows), a opção "Abrir pelo SSH" e o usuário do SSH. O usuário do SSH SHALL ficar desabilitado quando "Abrir pelo SSH" estiver desligado. "Salvar" grava e fecha; "Cancelar" e Esc fecham sem gravar.

#### Scenario: Abrir as configurações
- **WHEN** o usuário aciona "Configurações"
- **THEN** o diálogo mostra a pasta de status, "Abrir pelo SSH" e o usuário do SSH com os valores em uso

#### Scenario: SSH desligado
- **WHEN** o usuário desliga "Abrir pelo SSH" no diálogo
- **THEN** o campo do usuário do SSH fica desabilitado

#### Scenario: Cancelar
- **WHEN** o usuário altera a pasta e aciona "Cancelar"
- **THEN** nada é gravado

### Requirement: Valores padrão
Sem configuração gravada, o painel SHALL usar a pasta `C:\ProgramData\Painel\sessoes`, "Abrir pelo SSH" ligado e o usuário `ia`.

#### Scenario: Primeira execução
- **WHEN** o arquivo de dados não tem configurações
- **THEN** o diálogo mostra `C:\ProgramData\Painel\sessoes`, "Abrir pelo SSH" ligado e o usuário `ia`

### Requirement: Validação das configurações
A pasta de status SHALL ser um caminho completo de unidade local (como `D:\Painel\sessoes`), sem caracteres inválidos. O usuário do SSH SHALL ter de 1 a 20 caracteres entre letras, dígitos, `_`, `.` e `-`, e SHALL NOT começar com `-` nem `.`. Um valor fora da regra SHALL ser recusado com o erro junto do campo, sem gravar nada.

#### Scenario: Usuário que vira opção do ssh
- **WHEN** o usuário informa `-oProxyCommand=calc` como usuário do SSH e salva
- **THEN** o erro aparece junto do campo e nada é gravado

#### Scenario: Pasta relativa
- **WHEN** o usuário informa `sessoes` como pasta de status e salva
- **THEN** o erro aparece junto do campo e nada é gravado

### Requirement: Configurações gravadas no arquivo de dados
As configurações SHALL ser gravadas na seção `configuracoes` do arquivo de dados do painel (`pastaStatus`, `usarSsh`, `usuarioSsh`), com a gravação segura dos trabalhos. Ao carregar, um campo ausente ou inválido SHALL valer o padrão. Com o arquivo de dados ilegível, o painel SHALL usar os padrões e o diálogo SHALL NOT gravar.

#### Scenario: Usuário inválido no arquivo
- **WHEN** o arquivo de dados tem `usuarioSsh` igual a `-oProxyCommand=calc`
- **THEN** o painel usa o usuário `ia`

#### Scenario: Arquivo de dados ilegível
- **WHEN** o arquivo de dados existe e não pode ser lido
- **THEN** o painel usa os valores padrão e o diálogo informa que não é possível salvar

### Requirement: Quando as configurações passam a valer
"Abrir pelo SSH" e o usuário do SSH SHALL valer a partir da próxima sessão aberta ou retomada. A pasta de status SHALL valer ao reabrir o painel, e o diálogo SHALL avisar isso ao salvar uma pasta diferente da que está em uso.

#### Scenario: Desligar o SSH
- **WHEN** o usuário desliga "Abrir pelo SSH", salva e aciona "Retomar" numa sessão
- **THEN** a aba chama o `abrir.ps1` direto, sem `ssh`

#### Scenario: Trocar a pasta de status
- **WHEN** o usuário salva outra pasta de status
- **THEN** o diálogo avisa que a pasta passa a valer ao reabrir o painel e a lista continua lendo a pasta atual
