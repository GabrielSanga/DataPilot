# Spec Delta

## ADDED Requirements

### Requirement: Ação de retomar na linha
A linha de uma sessão SHALL oferecer a ação "Retomar" somente quando o identificador e o agente dessa sessão corresponderem a uma sessão registrada no arquivo de dados do painel. As demais linhas SHALL NOT oferecer a ação.

#### Scenario: Sessão aberta pelo painel
- **WHEN** a lista mostra uma sessão do Claude cujo identificador está registrado no arquivo de dados
- **THEN** a linha oferece "Retomar"

#### Scenario: Sessão do Codex
- **WHEN** a lista mostra uma sessão do Codex, que o painel não registra
- **THEN** a linha não oferece "Retomar"

#### Scenario: Sessão criada por /clear
- **WHEN** o usuário executa `/clear` numa sessão do Claude aberta pelo painel e surge uma sessão com outro identificador
- **THEN** a linha da sessão nova não oferece "Retomar", e a da sessão original continua oferecendo

#### Scenario: Arquivo de dados ilegível
- **WHEN** o arquivo de dados do painel não pode ser lido
- **THEN** nenhuma linha oferece "Retomar"

### Requirement: Repositório cadastrado na linha de sessão registrada
A linha de uma sessão registrada SHALL indicar o repositório em que ela foi registrada e o rótulo, quando houver, vindos do arquivo de dados do painel.

#### Scenario: Sessão registrada com rótulo
- **WHEN** a lista mostra uma sessão registrada em `Faturamento` com o rótulo "implementação"
- **THEN** a linha indica `Faturamento` e "implementação" como dados do painel

#### Scenario: Pasta de trabalho divergente
- **WHEN** uma sessão registrada em `Faturamento` tem, no arquivo de status, um `cwd` que aponta para outra pasta
- **THEN** a linha indica `Faturamento` como o repositório em que "Retomar" vai abrir
