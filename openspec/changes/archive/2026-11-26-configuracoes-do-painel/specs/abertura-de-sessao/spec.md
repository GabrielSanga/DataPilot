## MODIFIED Requirements

### Requirement: Chamada pelo SSH local em uma única linha
O script SHALL funcionar quando invocado como `ssh -t <usuário>@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1` seguido dos parâmetros, e também quando invocado direto como `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1` seguido dos parâmetros, sem exigir aspas em nenhum valor de parâmetro. A validação dos parâmetros SHALL ser a mesma nos dois casos.

#### Scenario: Chamada manual pelo usuário principal
- **WHEN** o usuário principal executa a linha de SSH com `-Agente claude -Repo Faturamento -Sessao <guid> -Modo novo` num terminal
- **THEN** o agente abre nesse terminal, interativo, rodando como `ia`

#### Scenario: Chamada direta, sem SSH
- **WHEN** o usuário principal executa `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente codex -Repo Faturamento -Modo novo` num terminal
- **THEN** o agente abre nesse terminal, interativo, rodando como o usuário principal

#### Scenario: Parâmetro inválido na chamada direta
- **WHEN** o script é chamado direto com `-Repo ..\Windows`
- **THEN** o script recusa o parâmetro, não inicia agente e termina com código 2
