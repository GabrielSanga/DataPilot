# Spec Delta

## ADDED Requirements

### Requirement: Registro de lançamentos pendentes
O arquivo de dados SHALL guardar, em cada repositório de uma demanda, a lista `lancamentos` dos lançamentos pendentes, cada um com `id`, `agente`, `criadoEm` e, quando informado, `rotulo`. Um lançamento SHALL ser removido na mesma gravação em que vincula uma sessão. Na carga, lançamentos com `id` que não seja um GUID canônico, agente desconhecido, data inválida ou rótulo inválido SHALL ser desconsiderados. Lançamentos com mais de 24 horas SHALL poder ser removidos pelo painel ao gravar.

#### Scenario: Lançamento usado
- **WHEN** o painel vincula uma sessão por um lançamento pendente
- **THEN** a mesma gravação registra a sessão e remove o lançamento do repositório

#### Scenario: Lançamento inválido editado à mão
- **WHEN** o arquivo de dados contém um lançamento cujo `id` não é um GUID canônico
- **THEN** esse lançamento é desconsiderado e não vincula nenhuma sessão

#### Scenario: Arquivo sem lançamentos
- **WHEN** o arquivo de dados foi gravado por uma versão anterior e não tem `lancamentos`
- **THEN** ele é carregado normalmente

#### Scenario: Lançamento vencido removido
- **WHEN** o painel grava um lançamento novo num repositório que tem outro registrado há mais de 24 horas
- **THEN** o lançamento antigo é removido do arquivo

## MODIFIED Requirements

### Requirement: Registro das sessões abertas pelo painel
Ao abrir uma sessão nova do Claude, o painel SHALL registrar no repositório escolhido o agente, o identificador da sessão, o rótulo e a data de criação, antes de iniciar o terminal. Ao abrir uma sessão nova do Codex, cujo identificador ainda não existe, o painel SHALL registrar no repositório um lançamento pendente com o agente, o rótulo e a data, antes de iniciar o terminal. Se o terminal não puder ser iniciado, o registro SHALL ser desfeito.

#### Scenario: Sessão nova do Claude
- **WHEN** o usuário abre uma sessão nova do Claude no repositório `Faturamento`
- **THEN** o arquivo de dados passa a ter, nesse repositório, uma sessão com `agente` `claude` e o identificador gerado

#### Scenario: Terminal não inicia
- **WHEN** o painel registra a sessão e a abertura do terminal falha
- **THEN** a sessão registrada é removida do arquivo de dados

#### Scenario: Sessão nova do Codex
- **WHEN** o usuário abre uma sessão nova do Codex no repositório `Faturamento` da demanda `CHG-142`
- **THEN** nenhuma sessão é registrada no arquivo de dados, e o repositório `Faturamento` de `CHG-142` passa a ter um lançamento pendente com `agente` `codex`, o rótulo informado e a data

#### Scenario: Terminal do Codex não inicia
- **WHEN** o painel registra o lançamento e a abertura do terminal falha
- **THEN** o lançamento é removido do arquivo de dados

#### Scenario: Retomar não gera lançamento
- **WHEN** o usuário retoma uma sessão registrada, do Claude ou do Codex
- **THEN** nenhum lançamento é registrado

### Requirement: Dados de status nunca são gravados
O painel SHALL NOT copiar para o arquivo de dados nenhum valor lido de arquivos de status, com uma única exceção: no vínculo de uma sessão, acionado pelo usuário ou feito por correspondência com um lançamento pendente, o identificador da sessão, validado como GUID canônico, e o agente SHALL poder ser gravados.

#### Scenario: Sessão desconhecida aparece na pasta de status
- **WHEN** surge um arquivo de status de uma sessão que o painel não abriu, sem lançamento pendente que corresponda a ela, e o usuário não aciona o vínculo
- **THEN** o arquivo de dados permanece inalterado

#### Scenario: Vínculo acionado pelo usuário
- **WHEN** o usuário vincula uma sessão não vinculada
- **THEN** o arquivo de dados passa a ter o identificador e o agente dessa sessão, e nenhum outro valor do arquivo de status

#### Scenario: Vínculo por lançamento
- **WHEN** o painel vincula uma sessão por correspondência com um lançamento pendente
- **THEN** o arquivo de dados passa a ter o identificador e o agente dessa sessão, e nenhum outro valor do arquivo de status
