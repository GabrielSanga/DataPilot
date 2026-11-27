# Spec Delta

## ADDED Requirements

### Requirement: Criar trabalho pelo título
O painel SHALL permitir criar uma demanda informando só o `titulo`, de 1 a 80 caracteres, sem caracteres de controle. O `id` SHALL ser gerado pelo painel, no formato `t` seguido de 8 dígitos hexadecimais, único sem diferenciar maiúsculas, e SHALL NOT ser exibido nem pedido ao usuário.

#### Scenario: Trabalho criado
- **WHEN** o usuário cria um trabalho com o título "Ajuste NF-e"
- **THEN** a demanda passa a constar no arquivo de dados com um `id` gerado, o título e a data de criação, e aparece na lista com o título

#### Scenario: Título vazio
- **WHEN** o usuário tenta criar um trabalho sem título
- **THEN** a criação é recusada com o erro junto do campo e o arquivo de dados não é alterado

#### Scenario: Identificador não aparece
- **WHEN** o usuário cria ou visualiza um trabalho
- **THEN** nenhum campo nem texto da tela mostra o `id` da demanda

### Requirement: Editar o título do trabalho
O painel SHALL permitir alterar só o `titulo` de uma demanda, com a regra da criação, mantendo o `id`, os repositórios e as sessões.

#### Scenario: Alterar o título
- **WHEN** o usuário altera o título de um trabalho
- **THEN** o novo título aparece na lista e no arquivo de dados, e o `id`, os repositórios e as sessões continuam os mesmos

### Requirement: Vincular repositório pela pasta
"Vincular repositório" SHALL abrir a escolha de pasta do Windows na raiz dos fontes e SHALL aceitar só uma pasta filha direta da raiz cujo nome siga a regra de pasta, gravando apenas o nome. Outra pasta SHALL ser recusada com um erro, permitindo escolher de novo.

#### Scenario: Pasta da raiz
- **WHEN** o usuário escolhe `C:\Fontes\Faturamento`
- **THEN** o repositório `Faturamento` é vinculado ao trabalho

#### Scenario: Pasta fora da raiz
- **WHEN** o usuário escolhe `C:\Projetos\Faturamento` ou `C:\Fontes\Faturamento\src`
- **THEN** o painel mostra que só pastas diretas de `C:\Fontes` podem ser usadas e não vincula nada

#### Scenario: Escolha cancelada
- **WHEN** o usuário cancela a escolha de pasta
- **THEN** nada é vinculado

### Requirement: Change do repositório
Ao vincular um repositório, e pela ação "Trocar change" do menu dele, o painel SHALL oferecer como opções as changes ativas do repositório e "Nenhuma", e SHALL gravar no repositório a change escolhida, validada. As opções SHALL incluir só pastas comuns de `openspec\changes`, sem `archive`, com nome válido, até 200, em ordem alfabética.

#### Scenario: Repositório com changes
- **WHEN** o usuário escolhe a pasta `Faturamento`, que tem as changes `ajuste-boleto` e `nova-api`
- **THEN** o diálogo lista `ajuste-boleto`, `nova-api` e "Nenhuma", e a escolhida fica gravada no repositório

#### Scenario: Repositório sem OpenSpec
- **WHEN** a pasta escolhida não tem `openspec\changes`
- **THEN** a única opção é "Nenhuma" e o repositório é vinculado sem change

#### Scenario: Pasta de change que é uma junção
- **WHEN** uma entrada de `openspec\changes` é uma junção para outra pasta
- **THEN** ela não aparece entre as opções

#### Scenario: Trocar a change
- **WHEN** o usuário escolhe "Trocar change" no menu do repositório e seleciona outra change
- **THEN** o repositório passa a mostrar o progresso da nova change

### Requirement: Excluir sessão
Toda sessão exibida SHALL ter a ação "Excluir". Após confirmação, o painel SHALL remover o registro da sessão, se houver, e acrescentá-la às sessões excluídas, de modo que ela não apareça mais no painel. A exclusão SHALL NOT alterar arquivos do agente nem da pasta de status.

#### Scenario: Sessão de um trabalho
- **WHEN** o usuário exclui uma sessão registrada em `Faturamento` de um trabalho e confirma
- **THEN** a sessão deixa de aparecer no trabalho e não aparece em "Sessões"

#### Scenario: Sessão que o painel não abriu
- **WHEN** o usuário exclui uma sessão que aparece em "Sessões" sem registro e confirma
- **THEN** ela deixa de aparecer, mesmo com o arquivo de status ainda existindo

#### Scenario: Exclusão cancelada
- **WHEN** o usuário cancela a confirmação
- **THEN** o arquivo de dados não é alterado

## MODIFIED Requirements

### Requirement: Demanda fixa "Sem demanda"
A demanda de `id` `sem-demanda` SHALL ser apresentada como a entrada "Sessões", sempre presente no fim da lista, e SHALL NOT aparecer entre os trabalhos nem poder ser editada ou arquivada. Sessões abertas em "Sessões" SHALL ser registradas nela.

#### Scenario: Sessões abertas antes das demandas
- **WHEN** o arquivo de dados tem repositórios e sessões na demanda `sem-demanda`
- **THEN** essas sessões aparecem na entrada "Sessões", e a demanda não aparece entre os trabalhos

#### Scenario: Tentativa de arquivar
- **WHEN** o usuário seleciona "Sessões"
- **THEN** as ações de editar e arquivar não são oferecidas

## REMOVED Requirements

### Requirement: Criar demanda
**Reason**: o identificador passa a ser gerado e escondido, e a change sai da demanda.
**Migration**: substituído por "Criar trabalho pelo título"; a change é escolhida por repositório em "Change do repositório".

### Requirement: Editar demanda
**Reason**: só o título pode ser alterado.
**Migration**: substituído por "Editar o título do trabalho"; a change é trocada pelo menu do repositório.

### Requirement: Vincular repositório a uma demanda
**Reason**: o repositório passa a ser escolhido na pasta, e não digitado com sugestões.
**Migration**: substituído por "Vincular repositório pela pasta".

### Requirement: Mover sessão para outra demanda
**Reason**: sessões de um trabalho não são mais movidas; o único movimento é de "Sessões" para um trabalho, feito pelo vínculo.
**Migration**: em "Sessões", usar "Vincular a um trabalho"; num trabalho, excluir a sessão e abrir outra onde for preciso.

### Requirement: Remover o registro de uma sessão
**Reason**: remover o registro devolvia a sessão à lista de não vinculadas; o usuário quer tirá-la do painel.
**Migration**: substituído por "Excluir sessão".
