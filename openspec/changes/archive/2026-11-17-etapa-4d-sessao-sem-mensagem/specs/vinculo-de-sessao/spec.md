# Spec Delta

## ADDED Requirements

### Requirement: Cancelar lançamento pendente
O painel SHALL permitir cancelar um lançamento pendente pela linha dele no card, removendo-o do arquivo de dados sem pedir confirmação e sem fechar a aba aberta. Uma sessão que surja depois do cancelamento SHALL NOT ser vinculada por esse lançamento. Cancelar um lançamento que não existe mais SHALL NOT alterar o arquivo de dados.

#### Scenario: Aba fechada sem mensagem
- **WHEN** o usuário abre uma sessão nova do Codex, fecha a aba sem enviar mensagem e aciona "Cancelar" na linha do lançamento
- **THEN** a linha deixa de aparecer e o lançamento não consta mais no arquivo de dados

#### Scenario: Cancelar com a aba aberta
- **WHEN** o usuário cancela o lançamento e depois envia a primeira mensagem na aba que continuou aberta
- **THEN** a sessão aparece em "Não vinculadas" e pode ser vinculada manualmente

#### Scenario: Outro lançamento no mesmo card
- **WHEN** o card tem dois lançamentos pendentes e o usuário cancela um deles
- **THEN** o outro continua pendente e aparecendo no card

#### Scenario: Lançamento já usado por outra janela
- **WHEN** o usuário aciona "Cancelar" num lançamento que outra janela do painel acabou de usar para vincular uma sessão
- **THEN** o arquivo de dados não é alterado, a sessão vinculada permanece e a linha some na atualização seguinte
