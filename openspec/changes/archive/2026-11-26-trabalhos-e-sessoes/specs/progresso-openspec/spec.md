# Spec Delta

## MODIFIED Requirements

### Requirement: Localização do tasks.md
Para cada repositório de uma demanda com change, própria ou herdada da demanda, o painel SHALL procurar `<raiz>\<pasta>\openspec\changes\<change>\tasks.md`, em que `<raiz>` é `C:\Fontes`, ou o valor de `--raiz-fontes` quando informado ao iniciar o painel. O caminho SHALL ser montado só com `pasta` e a change do arquivo de dados, validados.

#### Scenario: Change ativa
- **WHEN** o repositório `Faturamento` tem a change `ajuste-boleto` e existe `C:\Fontes\Faturamento\openspec\changes\ajuste-boleto\tasks.md`
- **THEN** o painel usa esse arquivo para o progresso do repositório `Faturamento`

#### Scenario: Demanda sem change
- **WHEN** o repositório não tem change, nem própria nem herdada
- **THEN** o painel não procura nenhum `tasks.md` para ele e não mostra progresso no card

#### Scenario: Repositório sem a change
- **WHEN** o repositório `Estoque` tem uma change cuja pasta não existe nele, ativa nem arquivada
- **THEN** o card `Estoque` indica que a change não foi encontrada

### Requirement: Exibição do progresso
Cada card de repositório com change SHALL mostrar o nome da change, uma barra e o progresso dela. Change arquivada SHALL ter a indicação "arquivada"; change não encontrada, "change não encontrada"; arquivo ilegível, "progresso indisponível"; arquivo sem tarefas, "sem tarefas". A área do trabalho SHALL NOT mostrar soma nem change.

#### Scenario: Change em dois repositórios
- **WHEN** `Faturamento` tem 4 de 6 tarefas concluídas na sua change e `Estoque` tem 3 de 6 na dele
- **THEN** o card `Faturamento` mostra 4 de 6, o card `Estoque` mostra 3 de 6, e o topo do trabalho não mostra soma

#### Scenario: Change não encontrada
- **WHEN** um repositório tem change e ela não existe nele, ativa nem arquivada
- **THEN** o card dele mostra "change não encontrada", sem barra

#### Scenario: Um repositório ilegível
- **WHEN** o `tasks.md` da change de `Estoque` é recusado
- **THEN** o card `Estoque` mostra "progresso indisponível" e o card `Faturamento` continua com o progresso dele

### Requirement: Atualização do progresso
O progresso dos repositórios do trabalho selecionado SHALL refletir uma alteração no `tasks.md` em até 5 segundos e SHALL ser atualizado ao selecionar outro trabalho ou trocar a change de um repositório, sem vigiar as pastas do usuário `ia` e sem mudar o item selecionado.

#### Scenario: Tarefa marcada pelo agente
- **WHEN** o agente marca uma tarefa no `tasks.md` com o trabalho selecionado
- **THEN** o card do repositório passa a mostrar uma tarefa concluída a mais em até 5 segundos

#### Scenario: Troca de demanda
- **WHEN** o usuário seleciona outro trabalho com repositórios que têm change
- **THEN** o progresso de cada repositório aparece sem esperar o ciclo seguinte
