# Spec Delta

## ADDED Requirements

### Requirement: Data de criação do arquivo de status
O painel SHALL conhecer, para cada sessão lida, a data de criação do arquivo de status, obtida do sistema de arquivos, sem alterar o arquivo.

#### Scenario: Sessão com vários eventos
- **WHEN** o hook atualizou o arquivo de status de uma sessão várias vezes depois de criá-lo
- **THEN** a data de criação conhecida pelo painel é a da primeira gravação, e não a do último evento

#### Scenario: Sessão recém-criada
- **WHEN** surge o arquivo de status de uma sessão nova
- **THEN** a data de criação conhecida pelo painel é a do momento em que o arquivo surgiu

## MODIFIED Requirements

### Requirement: Dados de status servem apenas para exibição
O painel SHALL NOT usar nenhum valor lido de um arquivo de status para montar comandos, argumentos ou caminhos, nem para abrir, executar ou acessar o que esses valores indicam. Além da exibição, o painel SHALL poder comparar o agente, o nome da pasta de trabalho e a data de criação do arquivo com os lançamentos pendentes do arquivo de dados, e nada mais.

#### Scenario: Pasta de trabalho apontando para outro lugar
- **WHEN** um arquivo de status traz em `cwd` um caminho de rede, um executável ou um caminho inexistente
- **THEN** o painel apenas exibe o texto e não acessa esse caminho

#### Scenario: Comparação com lançamento pendente
- **WHEN** o painel verifica se uma sessão corresponde a um lançamento pendente
- **THEN** ele compara o texto do último segmento de `cwd` com o nome do repositório, sem acessar o caminho indicado
