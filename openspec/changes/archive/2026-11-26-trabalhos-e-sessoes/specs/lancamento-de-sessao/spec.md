# Spec Delta

## ADDED Requirements

### Requirement: Diálogo de nova sessão
O diálogo de nova sessão SHALL pedir só o agente e, opcionalmente, o rótulo, sem mostrar o trabalho nem o repositório.

#### Scenario: Abrir o diálogo
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento` de um trabalho
- **THEN** o diálogo mostra a escolha do agente e o campo de rótulo, e nenhum texto com o trabalho ou o repositório

## MODIFIED Requirements

### Requirement: Nova sessão
O painel SHALL oferecer a ação de abrir uma sessão nova no card de cada repositório de uma demanda e na área "Sessões", em que o usuário escolhe o agente e, opcionalmente, um rótulo; em "Sessões", escolhe antes a pasta do repositório. A ação SHALL abrir uma aba do Windows Terminal que inicia o agente nesse repositório como o usuário `ia`. Uma sessão nova do Claude SHALL ser registrada nesse repositório dessa demanda, ou da `sem-demanda`.

#### Scenario: Nova sessão do Claude
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento` de um trabalho, escolhe o agente Claude e confirma
- **THEN** abre uma aba do Windows Terminal com o Claude Code iniciado em `Faturamento`, rodando como `ia`, e a sessão fica registrada em `Faturamento` dentro desse trabalho

#### Scenario: Nova sessão do Codex
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento`, escolhe o agente Codex e confirma
- **THEN** abre uma aba do Windows Terminal com o Codex iniciado em `Faturamento`, rodando como `ia`

#### Scenario: Nenhum repositório cadastrado
- **WHEN** o trabalho selecionado não tem repositórios vinculados
- **THEN** nenhuma ação de "Nova sessão" é oferecida no trabalho e o painel orienta a vincular um repositório

#### Scenario: Demanda arquivada
- **WHEN** o usuário visualiza um trabalho arquivado
- **THEN** os cards dele não oferecem "Nova sessão"

#### Scenario: Nova sessão em "Sessões"
- **WHEN** o usuário aciona "Nova sessão" em "Sessões", escolhe a pasta `Estoque` e o Claude
- **THEN** o repositório `Estoque` é vinculado à `sem-demanda`, se ainda não estiver, e a sessão abre e fica registrada nele

### Requirement: Título da aba e rótulo
O título da aba SHALL ser o título da demanda, com até 40 caracteres e cada `;` trocado por `,`, e o repositório, separados por " · ", seguido do rótulo quando houver. Para sessões da demanda `sem-demanda`, o nome do agente SHALL ocupar o lugar do título. O rótulo SHALL ter no máximo 40 caracteres e SHALL NOT conter `;` nem caracteres de controle; um rótulo fora dessa regra SHALL ser recusado na tela.

#### Scenario: Sessão sem rótulo
- **WHEN** o usuário abre uma sessão em `Faturamento`, no trabalho "Ajuste NF-e", sem rótulo
- **THEN** a aba tem o título "Ajuste NF-e · Faturamento"

#### Scenario: Sessão com rótulo
- **WHEN** o usuário abre uma sessão em `Faturamento`, no trabalho "Ajuste NF-e", com o rótulo "implementação"
- **THEN** a aba tem o título "Ajuste NF-e · Faturamento · implementação"

#### Scenario: Sessão da demanda "Sem demanda"
- **WHEN** o usuário retoma uma sessão do Claude registrada em `Faturamento`, em "Sessões"
- **THEN** a aba tem o título "Claude · Faturamento"

#### Scenario: Rótulo com ponto e vírgula
- **WHEN** o usuário informa um rótulo contendo `;`
- **THEN** o painel recusa o rótulo e não abre a aba

#### Scenario: Título do trabalho com ponto e vírgula
- **WHEN** o trabalho tem o título "Fase 1; ajustes"
- **THEN** a aba usa "Fase 1, ajustes" no lugar do título
