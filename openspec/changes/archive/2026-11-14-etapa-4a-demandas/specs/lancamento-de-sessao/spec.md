# Spec Delta

## MODIFIED Requirements

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
