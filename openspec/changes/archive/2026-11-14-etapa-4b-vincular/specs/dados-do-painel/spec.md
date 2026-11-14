# Spec Delta

## MODIFIED Requirements

### Requirement: Dados de status nunca são gravados
O painel SHALL NOT copiar para o arquivo de dados nenhum valor lido de arquivos de status, com uma única exceção: no vínculo de uma sessão acionado pelo usuário, o identificador da sessão, validado como GUID canônico, e o agente SHALL poder ser gravados.

#### Scenario: Sessão desconhecida aparece na pasta de status
- **WHEN** surge um arquivo de status de uma sessão que o painel não abriu e o usuário não aciona o vínculo
- **THEN** o arquivo de dados permanece inalterado

#### Scenario: Vínculo acionado pelo usuário
- **WHEN** o usuário vincula uma sessão não vinculada
- **THEN** o arquivo de dados passa a ter o identificador e o agente dessa sessão, e nenhum outro valor do arquivo de status
