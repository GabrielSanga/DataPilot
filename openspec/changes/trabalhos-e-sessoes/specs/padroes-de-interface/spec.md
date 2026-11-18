# Spec Delta

## ADDED Requirements

### Requirement: Nomes na interface
A interface SHALL chamar de "trabalho" o que as specs, o arquivo de dados e o código chamam de "demanda", e de "Sessões" a entrada que as specs chamam de "Não vinculadas" ou de demanda `sem-demanda`. Nenhum texto da interface SHALL usar "demanda" nem "Não vinculadas".

#### Scenario: Botão de criar
- **WHEN** o usuário olha a lista
- **THEN** o botão de criar se chama "Novo trabalho" e a entrada fixa se chama "Sessões"

#### Scenario: Mensagens e confirmações
- **WHEN** o painel mostra uma confirmação ou um erro sobre uma demanda
- **THEN** o texto usa "trabalho"

### Requirement: Formulários sem ajuda permanente
Os diálogos SHALL NOT mostrar texto de ajuda permanente sob os campos. Uma restrição de um campo SHALL aparecer só como erro junto dele, quando violada. Campos opcionais SHALL ser marcados como "(opcional)".

#### Scenario: Diálogo de novo trabalho
- **WHEN** o usuário abre o diálogo de novo trabalho
- **THEN** o diálogo mostra só o campo de título, sem texto de ajuda

#### Scenario: Restrição violada
- **WHEN** o usuário informa um rótulo com ";" e confirma
- **THEN** o erro aparece junto do campo de rótulo

## MODIFIED Requirements

### Requirement: Alto contraste
Com o alto contraste do Windows ligado, o painel SHALL usar as cores do sistema em textos, fundos, bordas e controles, e SHALL mostrar o nome do status de cada sessão em texto na linha.

#### Scenario: Alto contraste ligado
- **WHEN** o usuário liga o alto contraste do Windows com o painel aberto ou antes de abri-lo
- **THEN** a janela e os diálogos passam a usar as cores do sistema, e cada sessão passa a mostrar o nome do status em texto

### Requirement: Confirmação de remoção
A confirmação de uma ação que remove, exclui ou descarta algo SHALL dizer o que vai acontecer, SHALL ter o botão de confirmação nomeado pela ação e SHALL ter "Cancelar" como botão padrão, de modo que Enter não confirme.

#### Scenario: Remover registro de sessão
- **WHEN** o usuário aciona "Excluir" numa sessão
- **THEN** a confirmação explica que a conversa não é apagada e que a sessão some do painel, com os botões "Excluir sessão" e "Cancelar"

#### Scenario: Enter na confirmação
- **WHEN** a confirmação de exclusão está aberta e o usuário pressiona Enter
- **THEN** nada é excluído e a confirmação fecha
