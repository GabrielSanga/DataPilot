# padroes-de-interface Specification
## Purpose

Define as regras comuns a toda a interface do painel, na janela principal e nos diálogos: o que se faz pelo teclado, como o foco aparece, como o painel se comporta com o alto contraste do Windows, como diálogos, confirmações e erros funcionam e como os textos são escritos.

## Requirements
### Requirement: Teclado e foco visível
Toda ação do painel SHALL poder ser acionada pelo teclado, e o controle com foco SHALL ter um indicador visível. O painel SHALL oferecer os atalhos Ctrl+N para nova demanda, Ctrl+F para o filtro de demandas e F8 para ir à próxima sessão que precisa do usuário.

#### Scenario: Navegação só com o teclado
- **WHEN** o usuário percorre a janela com Tab
- **THEN** cada controle focado mostra o indicador de foco, e os botões são acionados com Enter ou Espaço

#### Scenario: Nova demanda pelo atalho
- **WHEN** o usuário pressiona Ctrl+N com a janela ativa
- **THEN** abre o diálogo de nova demanda

### Requirement: Alto contraste
Com o alto contraste do Windows ligado, o painel SHALL usar as cores do sistema em textos, fundos, bordas e controles, mantendo o nome de cada status em texto.

#### Scenario: Alto contraste ligado
- **WHEN** o usuário liga o alto contraste do Windows com o painel aberto ou antes de abri-lo
- **THEN** a janela e os diálogos passam a usar as cores do sistema, e cada sessão continua mostrando o nome do status

### Requirement: Diálogos de formulário
Nos diálogos do painel, o botão principal SHALL ter o nome da ação, Enter SHALL acionar o botão principal e Esc SHALL cancelar. Um erro de validação SHALL aparecer junto do campo, sem fechar o diálogo e sem apagar o que foi digitado.

#### Scenario: Rótulo inválido
- **WHEN** o usuário digita um rótulo com ";" no diálogo de nova sessão e pressiona Enter
- **THEN** o diálogo continua aberto, com o texto digitado e a explicação do erro abaixo do campo

#### Scenario: Cancelar com Esc
- **WHEN** um diálogo está aberto e o usuário pressiona Esc
- **THEN** o diálogo fecha sem alterar nada

### Requirement: Confirmação de remoção
A confirmação de uma ação que remove ou descarta algo SHALL dizer o que vai acontecer, SHALL ter o botão de confirmação nomeado pela ação e SHALL ter "Cancelar" como botão padrão, de modo que Enter não confirme a remoção.

#### Scenario: Remover registro de sessão
- **WHEN** o usuário escolhe "Remover registro" no menu de uma sessão
- **THEN** a confirmação explica que a conversa não é apagada e que a sessão deixa de poder ser retomada pelo painel, com os botões "Remover registro" e "Cancelar"

#### Scenario: Enter na confirmação
- **WHEN** a confirmação de remoção está aberta e o usuário pressiona Enter
- **THEN** nada é removido e a confirmação fecha

### Requirement: Erros dentro da janela
Um erro de operação, como o terminal que não abre ou uma gravação que falha, SHALL aparecer numa faixa dentro da janela, com o que aconteceu e o que fazer, e um botão para fechá-la, sem bloquear o uso da janela.

#### Scenario: Terminal não abre
- **WHEN** o usuário aciona "Retomar" e o terminal não pode ser iniciado
- **THEN** uma faixa de erro aparece na janela com a causa e a orientação, e o usuário continua podendo usar a lista e os outros botões

#### Scenario: Fechar a faixa
- **WHEN** o usuário aciona o botão de fechar da faixa de erro
- **THEN** a faixa some

### Requirement: Texto da interface
Os textos do painel SHALL usar português com inicial maiúscula só no começo da frase, plural de acordo com a quantidade e contagens separadas por vírgula. Estados vazios SHALL dizer o que o usuário pode fazer.

#### Scenario: Contagens no plural
- **WHEN** há duas sessões em "precisa de atenção"
- **THEN** o painel escreve "2 precisam de atenção", e "1 precisa de atenção" quando há uma

#### Scenario: Demanda sem sessões
- **WHEN** uma demanda tem dois repositórios e nenhuma sessão
- **THEN** o item da lista mostra "2 repositórios, nenhuma sessão"

#### Scenario: Repositório sem sessões
- **WHEN** um repositório da demanda selecionada não tem sessões
- **THEN** o painel orienta a usar "Nova sessão" para abrir o Claude ou o Codex nele
