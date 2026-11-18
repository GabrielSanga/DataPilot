# Spec Delta

## Purpose

Mostra, na demanda selecionada, quantas tarefas da change do OpenSpec estão concluídas, lendo o `tasks.md` dos repositórios da demanda na pasta do usuário `ia` apenas para contar, sem seguir atalhos nem junções e sem que o conteúdo do arquivo vire ação ou texto exibido.

## ADDED Requirements

### Requirement: Localização do tasks.md
Para cada repositório de uma demanda com `openspecChange`, o painel SHALL procurar `<raiz>\<pasta>\openspec\changes\<openspecChange>\tasks.md`, em que `<raiz>` é `C:\Fontes`, ou o valor de `--raiz-fontes` quando informado ao iniciar o painel. O caminho SHALL ser montado só com `pasta` e `openspecChange` do arquivo de dados, validados.

#### Scenario: Change ativa
- **WHEN** a demanda `CHG-142` tem `openspecChange` `ajuste-boleto` e existe `C:\Fontes\Faturamento\openspec\changes\ajuste-boleto\tasks.md`
- **THEN** o painel usa esse arquivo para o progresso do repositório `Faturamento`

#### Scenario: Demanda sem change
- **WHEN** a demanda não tem `openspecChange`
- **THEN** o painel não procura nenhum `tasks.md` e não mostra progresso

#### Scenario: Repositório sem a change
- **WHEN** a pasta da change não existe no repositório `Estoque` da demanda
- **THEN** o repositório `Estoque` fica sem progresso, sem indicação de erro

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
A demanda selecionada SHALL mostrar uma barra com a soma das tarefas concluídas e do total dos repositórios em que a change foi encontrada e lida, e cada card desses repositórios SHALL mostrar o progresso dele. Change arquivada SHALL ter a indicação "arquivada". Sem a change em nenhum repositório, SHALL mostrar "change não encontrada nos repositórios". Repositório ilegível SHALL ficar fora da soma, com "progresso indisponível".

#### Scenario: Change em dois repositórios
- **WHEN** `Faturamento` tem 4 de 6 tarefas concluídas e `Estoque` tem 3 de 6
- **THEN** a barra da demanda mostra 7 de 12 tarefas, o card `Faturamento` mostra 4 de 6 e o card `Estoque` mostra 3 de 6

#### Scenario: Change não encontrada
- **WHEN** a demanda tem `openspecChange` e nenhum repositório tem a change, ativa ou arquivada
- **THEN** a área da demanda mostra "change não encontrada nos repositórios" e nenhum card mostra progresso

#### Scenario: Um repositório ilegível
- **WHEN** `Faturamento` tem 4 de 6 e o `tasks.md` de `Estoque` é recusado
- **THEN** a barra mostra 4 de 6 e o card `Estoque` mostra "progresso indisponível"

### Requirement: Atualização do progresso
O progresso da demanda selecionada SHALL refletir uma alteração no `tasks.md` em até 5 segundos e SHALL ser atualizado ao selecionar outra demanda, sem vigiar as pastas do usuário `ia` e sem mudar a demanda selecionada.

#### Scenario: Tarefa marcada pelo agente
- **WHEN** o agente marca uma tarefa no `tasks.md` com a demanda selecionada
- **THEN** a barra e o card passam a mostrar uma tarefa concluída a mais em até 5 segundos

#### Scenario: Troca de demanda
- **WHEN** o usuário seleciona outra demanda com `openspecChange`
- **THEN** o progresso dessa demanda aparece sem esperar o ciclo seguinte
