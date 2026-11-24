## MODIFIED Requirements

### Requirement: Composição do comando
O painel SHALL iniciar `wt.exe` com os argumentos, nesta ordem e cada um separado: `-w`, `painel`, `nt`, `--title`, o título, `--suppressApplicationTitle`; com "Abrir pelo SSH" ligado, `ssh`, `-t` e `<usuário>@localhost`; depois `powershell`, `-NoProfile`, `-ExecutionPolicy`, `Bypass`, `-File`, `C:\Painel\abrir.ps1`, `-Agente`, o agente, `-Repo`, a pasta, `-Sessao` e o identificador quando houver, `-Modo` e o modo. O usuário SHALL ser validado de novo antes de entrar no comando. Nenhum shell SHALL intermediar a chamada.

#### Scenario: Retomada do Claude
- **WHEN** o painel retoma a sessão `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10` do Claude em `Faturamento`
- **THEN** os argumentos terminam em `-Agente`, `claude`, `-Repo`, `Faturamento`, `-Sessao`, `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10`, `-Modo`, `retomar`

#### Scenario: Nova sessão do Codex
- **WHEN** o painel abre uma sessão nova do Codex em `Faturamento`
- **THEN** os argumentos terminam em `-Agente`, `codex`, `-Repo`, `Faturamento`, `-Modo`, `novo`, sem `-Sessao`

#### Scenario: Abas reunidas
- **WHEN** o painel abre duas sessões
- **THEN** as duas abas ficam na mesma janela do Windows Terminal, de nome `painel`

#### Scenario: Pelo SSH com o usuário padrão
- **WHEN** o painel abre uma sessão com "Abrir pelo SSH" ligado e o usuário `ia`
- **THEN** depois de `--suppressApplicationTitle` vêm `ssh`, `-t`, `ia@localhost` e `powershell`

#### Scenario: Pelo SSH com outro usuário
- **WHEN** o painel abre uma sessão com "Abrir pelo SSH" ligado e o usuário `agente-ia`
- **THEN** o destino do `ssh` é `agente-ia@localhost`

#### Scenario: Sem SSH
- **WHEN** o painel abre uma sessão com "Abrir pelo SSH" desligado
- **THEN** depois de `--suppressApplicationTitle` vem `powershell`, sem `ssh`, `-t` nem destino
