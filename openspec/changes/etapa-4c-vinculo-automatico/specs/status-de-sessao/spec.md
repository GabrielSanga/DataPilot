# Spec Delta

## ADDED Requirements

### Requirement: Data de criação do arquivo preservada
O hook SHALL atualizar o arquivo de status de uma sessão sem alterar a data de criação do arquivo, que SHALL permanecer a da primeira gravação feita para a sessão.

#### Scenario: Eventos posteriores ao primeiro
- **WHEN** o hook grava `SessionStart` e, mais tarde, `UserPromptSubmit` e `Stop` da mesma sessão
- **THEN** a data de criação do arquivo de status continua sendo a da primeira gravação

#### Scenario: Primeiro evento da sessão
- **WHEN** o hook grava o primeiro evento de uma sessão que ainda não tem arquivo
- **THEN** a data de criação do arquivo é a do momento dessa gravação
