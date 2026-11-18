# Spec Delta

## ADDED Requirements

### Requirement: Abrir de novo um lançamento pendente
O painel SHALL permitir abrir de novo o agente de um lançamento pendente, pela ação "Abrir" da linha dele, lançando uma sessão nova desse agente no repositório do lançamento, com o rótulo dele e sem registrar outro lançamento. A data do lançamento SHALL passar a ser a dessa abertura. Se o terminal não puder ser iniciado, a data anterior SHALL ser mantida e a falha SHALL ser informada. O comando SHALL ser composto como o de uma sessão nova, só com dados do arquivo de dados.

#### Scenario: Aba fechada sem mensagem
- **WHEN** o usuário abre uma sessão nova do Codex pelo card, fecha a aba sem enviar mensagem e aciona "Abrir" na linha do lançamento
- **THEN** abre uma aba do Codex nesse repositório, com o mesmo título, e o card continua com uma só linha para esse lançamento, com o tempo decorrido recomeçando

#### Scenario: Primeira mensagem depois de abrir de novo
- **WHEN** o usuário envia a primeira mensagem na aba aberta por "Abrir"
- **THEN** a sessão é registrada no card com o rótulo do lançamento e a linha deixa de aparecer

#### Scenario: Terminal não inicia ao abrir de novo
- **WHEN** o usuário aciona "Abrir" e o terminal não pode ser iniciado
- **THEN** o painel informa a falha e o lançamento permanece no arquivo de dados com a data anterior

#### Scenario: Lançamento que não existe mais
- **WHEN** o usuário aciona "Abrir" num lançamento que outra janela do painel acabou de usar ou cancelar
- **THEN** nenhuma aba é aberta, o arquivo de dados não é alterado e a linha some na atualização seguinte

#### Scenario: Lançamento vencido
- **WHEN** o usuário aciona "Abrir" num lançamento que passou das 24 horas enquanto a tela estava aberta
- **THEN** nenhuma aba é aberta e o arquivo de dados não é alterado
