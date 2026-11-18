# Spec Delta

## MODIFIED Requirements

### Requirement: Retomada de sessão
Com `-Modo retomar`, o script SHALL retomar a conversa identificada por `-Sessao` no agente indicado. `-Sessao` SHALL ser obrigatório nesse caso. Quando a retomada do Claude terminar com código diferente de 0 em menos de 30 segundos, o script SHALL avisar e iniciar o Claude como sessão nova com o mesmo identificador, e SHALL terminar com o código dessa segunda execução. A retomada do Codex SHALL NOT ter segunda tentativa.

#### Scenario: Retomar sessão do Claude
- **WHEN** o script é chamado com `-Agente claude -Sessao <guid> -Modo retomar`
- **THEN** o Claude Code abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomar sessão do Codex
- **WHEN** o script é chamado com `-Agente codex -Sessao <guid> -Modo retomar`
- **THEN** o Codex abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomada sem identificador
- **WHEN** o script é chamado com `-Modo retomar` e sem `-Sessao`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

#### Scenario: Sessão do Claude fechada antes da primeira mensagem
- **WHEN** o script é chamado com `-Agente claude -Sessao <guid> -Modo retomar` e o Claude não tem conversa gravada para `<guid>`
- **THEN** o script avisa que a conversa ainda não tem mensagens e o Claude Code inicia uma sessão nova com `<guid>` como identificador

#### Scenario: Retomada do Claude que falha depois de uso
- **WHEN** a retomada do Claude termina com código diferente de 0 depois de mais de 30 segundos
- **THEN** o script não inicia outra sessão e termina com o código do agente

#### Scenario: Retomada do Codex que falha
- **WHEN** a retomada do Codex termina com código diferente de 0
- **THEN** o script não inicia outra sessão e termina com o código do agente
