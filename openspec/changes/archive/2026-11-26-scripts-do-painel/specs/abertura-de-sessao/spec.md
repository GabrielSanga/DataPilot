## MODIFIED Requirements

### Requirement: Scripts protegidos contra alteração pelo usuário ia
`C:\Painel` SHALL permitir que o usuário `ia` leia e execute os scripts, mas não crie, altere nem exclua arquivos nessa pasta. Só o sistema, os administradores e o usuário principal SHALL poder gravar nela, este último para que o painel mantenha os scripts atualizados.

#### Scenario: Usuário ia tenta alterar um script
- **WHEN** um processo do usuário `ia` tenta modificar `C:\Painel\abrir.ps1` ou `C:\Painel\hook.ps1`
- **THEN** a operação é negada

#### Scenario: Usuário ia executa um script
- **WHEN** um agente rodando como `ia` dispara o hook configurado em `C:\Painel\hook.ps1`
- **THEN** o script é executado normalmente

#### Scenario: Usuário principal atualiza um script
- **WHEN** o painel, rodando como o usuário principal e sem elevação, substitui `C:\Painel\abrir.ps1`
- **THEN** a gravação é bem-sucedida
