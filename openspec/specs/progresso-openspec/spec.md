# progresso-openspec Specification
## Purpose

Mostra, na demanda selecionada, quantas tarefas da change do OpenSpec estão concluídas, lendo o `tasks.md` dos repositórios da demanda na pasta do usuário `ia` apenas para contar, sem seguir atalhos nem junções e sem que o conteúdo do arquivo vire ação ou texto exibido.

## Requirements

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

### Requirement: Change arquivada
Quando a change ativa não existir num repositório, o painel SHALL usar o `tasks.md` da pasta de `openspec\changes\archive` cujo nome seja `AAAA-MM-DD-<openspecChange>`, com data válida, escolhendo a de data mais recente, e SHALL indicar que a change está arquivada. A change ativa SHALL ter prioridade sobre a arquivada.

#### Scenario: Change arquivada
- **WHEN** não existe `openspec\changes\ajuste-boleto` e existe `openspec\changes\archive\2026-11-20-ajuste-boleto\tasks.md`
- **THEN** o painel usa esse arquivo e indica que a change está arquivada

#### Scenario: Ativa e arquivada ao mesmo tempo
- **WHEN** existem a pasta ativa e uma arquivada da mesma change
- **THEN** o painel usa a ativa, sem indicar arquivada

#### Scenario: Nome parecido
- **WHEN** existe só `openspec\changes\archive\2026-11-20-ajuste-boleto-2`
- **THEN** ela não é usada para a change `ajuste-boleto`

### Requirement: Contagem das tarefas
O painel SHALL contar como tarefas as linhas do `tasks.md` que casam com `^\s*-\s\[( |x|X)\]` e, entre elas, como concluídas as marcadas com `x` ou `X`. As demais linhas SHALL ser ignoradas.

#### Scenario: Tarefas marcadas e pendentes
- **WHEN** o `tasks.md` tem 7 linhas `- [x]` e 5 linhas `- [ ]`
- **THEN** o progresso é 7 de 12

#### Scenario: Arquivo sem tarefas
- **WHEN** o `tasks.md` não tem nenhuma linha de tarefa
- **THEN** o repositório aparece como "sem tarefas"

### Requirement: Leitura segura do tasks.md
O painel SHALL ler o `tasks.md` somente para contar, sem gravar, até 256 KB, sem bloquear quem escreve. SHALL recusar a leitura quando algum trecho do caminho a partir de `<raiz>\<pasta>`, ou o próprio arquivo, for atalho ou junção, ou quando o arquivo aberto não for o do caminho montado. Arquivo recusado, maior que o limite ou com UTF-8 inválido SHALL ser tratado como ilegível.

#### Scenario: Junção na pasta da change
- **WHEN** a pasta `openspec\changes\ajuste-boleto` é uma junção para outra pasta
- **THEN** o painel não lê o arquivo e o card indica "progresso indisponível"

#### Scenario: Pasta do repositório trocada por junção
- **WHEN** `C:\Fontes\Faturamento` é uma junção
- **THEN** o painel não lê nada dentro dela e o card indica "progresso indisponível"

#### Scenario: Arquivo grande demais
- **WHEN** o `tasks.md` tem mais de 256 KB
- **THEN** o painel não conta as tarefas e o card indica "progresso indisponível"

#### Scenario: Arquivo sendo gravado
- **WHEN** o agente está gravando o `tasks.md` no momento da leitura
- **THEN** o painel lê sem impedir a gravação, ou tenta de novo na atualização seguinte

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
