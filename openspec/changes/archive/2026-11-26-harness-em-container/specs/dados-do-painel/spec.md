## ADDED Requirements

### Requirement: Ambiente de execução nos registros
Cada sessão registrada e cada lançamento pendente SHALL guardar o ambiente em que foi aberto, no campo `execucao`, com `container` ou `windows`. Na carga, um registro sem `execucao` SHALL valer como `windows`, e um registro com valor desconhecido SHALL ser desconsiderado. O vínculo de uma sessão por lançamento pendente SHALL passar à sessão o `execucao` do lançamento.

#### Scenario: Sessão registrada por uma versão anterior
- **WHEN** o arquivo de dados tem uma sessão sem `execucao`
- **THEN** ela é carregada com o ambiente `windows`

#### Scenario: Ambiente desconhecido editado à mão
- **WHEN** o arquivo de dados tem uma sessão com `execucao` igual a `wsl`
- **THEN** essa sessão é desconsiderada na carga e nada dela é usado em comandos

#### Scenario: Lançamento de uma versão anterior
- **WHEN** o arquivo de dados tem um lançamento sem `execucao`
- **THEN** ele é carregado com o ambiente `windows`

#### Scenario: Vínculo por lançamento do container
- **WHEN** o painel vincula uma sessão por um lançamento pendente com `execucao` `container`
- **THEN** a sessão registrada fica com `execucao` `container`

## MODIFIED Requirements

### Requirement: Registro das sessões abertas pelo painel
Ao abrir uma sessão nova do Claude, o painel SHALL registrar no repositório escolhido o agente, o identificador da sessão, o rótulo, a data de criação e o ambiente de execução, antes de iniciar o terminal. Ao abrir uma sessão nova do Codex, cujo identificador ainda não existe, o painel SHALL registrar no repositório um lançamento pendente com o agente, o rótulo, a data e o ambiente de execução, antes de iniciar o terminal. Se o terminal não puder ser iniciado, o registro SHALL ser desfeito.

#### Scenario: Sessão nova do Claude
- **WHEN** o usuário abre uma sessão nova do Claude no repositório `Faturamento`, com "Container Docker" escolhido
- **THEN** o arquivo de dados passa a ter, nesse repositório, uma sessão com `agente` `claude`, o identificador gerado e `execucao` `container`

#### Scenario: Terminal não inicia
- **WHEN** o painel registra a sessão e a abertura do terminal falha
- **THEN** a sessão registrada é removida do arquivo de dados

#### Scenario: Container indisponível
- **WHEN** o painel registra a sessão e o ambiente do container está indisponível
- **THEN** a sessão registrada é removida do arquivo de dados

#### Scenario: Sessão nova do Codex
- **WHEN** o usuário abre uma sessão nova do Codex no repositório `Faturamento` da demanda `CHG-142`
- **THEN** nenhuma sessão é registrada no arquivo de dados, e o repositório `Faturamento` de `CHG-142` passa a ter um lançamento pendente com `agente` `codex`, o rótulo informado, a data e o ambiente de execução em uso

#### Scenario: Terminal do Codex não inicia
- **WHEN** o painel registra o lançamento e a abertura do terminal falha
- **THEN** o lançamento é removido do arquivo de dados

#### Scenario: Retomar não gera lançamento
- **WHEN** o usuário retoma uma sessão registrada, do Claude ou do Codex
- **THEN** nenhum lançamento é registrado
