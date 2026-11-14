# Spec Delta

## Purpose

Lê, com segurança e sem atraso perceptível, os arquivos de status que o usuário isolado `ia` grava para cada sessão de agente, entregando ao restante do painel apenas dados válidos e que servem somente para exibição.

## ADDED Requirements

### Requirement: Sessões lidas da pasta de status
O painel SHALL obter as sessões dos arquivos `<session_id>.json` da pasta de status, uma sessão por arquivo válido. Arquivos com outra extensão SHALL ser ignorados.

#### Scenario: Arquivo de status válido
- **WHEN** a pasta de status contém um arquivo válido gravado pelo hook
- **THEN** o painel passa a conhecer uma sessão com o agente, o identificador, a pasta de trabalho, o status e o instante de atualização desse arquivo

#### Scenario: Pasta vazia
- **WHEN** a pasta de status existe e não contém arquivos
- **THEN** o painel não conhece nenhuma sessão

#### Scenario: Arquivo temporário do hook
- **WHEN** a pasta de status contém um arquivo `.tmp`
- **THEN** esse arquivo não gera sessão

### Requirement: Validação do conteúdo
O painel SHALL aceitar um arquivo apenas se ele for um objeto JSON com `agente` igual a `claude` ou `codex`, `session_id` casando com `^[A-Za-z0-9_-]{1,100}$` e igual ao nome do arquivo, `status` entre `ativa`, `executando`, `aguardando`, `atencao` e `encerrada`, e `atualizado` como data e hora ISO 8601 com fuso. Qualquer outro arquivo SHALL ser ignorado. Campos adicionais SHALL ser desconsiderados.

#### Scenario: Conteúdo que não é JSON
- **WHEN** um arquivo `.json` da pasta contém texto que não é JSON válido
- **THEN** ele não gera sessão e as demais sessões continuam listadas

#### Scenario: Status desconhecido
- **WHEN** um arquivo traz em `status` um valor fora dos cinco previstos
- **THEN** ele não gera sessão

#### Scenario: Identificador diferente do nome do arquivo
- **WHEN** o `session_id` do conteúdo não é igual ao nome do arquivo sem a extensão
- **THEN** ele não gera sessão

#### Scenario: Campo obrigatório ausente
- **WHEN** falta `agente`, `session_id`, `status` ou `atualizado` no arquivo
- **THEN** ele não gera sessão

#### Scenario: Campo adicional
- **WHEN** um arquivo válido traz também um campo que o painel não conhece
- **THEN** a sessão é listada normalmente

### Requirement: Limite de tamanho por arquivo
O painel SHALL ignorar arquivos de status maiores que 16 KB, sem carregar o conteúdo excedente.

#### Scenario: Arquivo grande demais
- **WHEN** um arquivo `.json` da pasta tem mais de 16 KB
- **THEN** ele não gera sessão e o painel continua respondendo normalmente

### Requirement: Limite de quantidade de arquivos
O painel SHALL considerar no máximo os 500 arquivos de status modificados mais recentemente.

#### Scenario: Pasta com arquivos em excesso
- **WHEN** a pasta de status contém mais de 500 arquivos `.json`
- **THEN** apenas os 500 modificados mais recentemente são lidos e o painel continua respondendo normalmente

### Requirement: Tolerância a falhas de leitura
Uma falha ao ler um arquivo SHALL afetar apenas esse arquivo. Se a pasta de status não existir ou não puder ser lida, o painel SHALL informar que ela está indisponível, sem encerrar, e SHALL voltar a ler quando ela ficar disponível.

#### Scenario: Arquivo removido durante a leitura
- **WHEN** um arquivo é removido ou fica inacessível enquanto o painel lê a pasta
- **THEN** esse arquivo é desconsiderado e as demais sessões são listadas

#### Scenario: Pasta de status inexistente
- **WHEN** o painel inicia e a pasta de status não existe
- **THEN** o painel permanece aberto e indica que a pasta de status está indisponível

#### Scenario: Pasta passa a existir
- **WHEN** a pasta de status é criada com o painel já aberto
- **THEN** em até 5 segundos o painel passa a listar as sessões dela

### Requirement: Somente leitura e sem bloquear quem escreve
O painel SHALL NOT criar, alterar nem excluir nada na pasta de status, e SHALL ler os arquivos de modo que o hook consiga substituí-los durante a leitura.

#### Scenario: Hook atualiza um arquivo que está sendo lido
- **WHEN** o hook substitui um arquivo de status no momento em que o painel o lê
- **THEN** a substituição é concluída e o painel exibe o conteúdo anterior ou o novo, completos

#### Scenario: Uso prolongado
- **WHEN** o painel fica aberto durante um dia de trabalho
- **THEN** nenhum arquivo da pasta de status foi criado, alterado ou excluído por ele

### Requirement: Atualização em até 5 segundos
Toda criação, alteração ou remoção de arquivo de status SHALL refletir no painel em até 5 segundos, mesmo que o aviso de mudança do sistema de arquivos seja perdido.

#### Scenario: Sessão muda de status
- **WHEN** o hook grava um novo status para uma sessão já listada
- **THEN** o painel exibe o novo status em até 5 segundos

#### Scenario: Sessão nova
- **WHEN** o hook cria o arquivo de uma sessão que ainda não existia
- **THEN** a sessão aparece no painel em até 5 segundos

#### Scenario: Arquivo removido
- **WHEN** o arquivo de status de uma sessão é excluído
- **THEN** a sessão deixa de aparecer no painel em até 5 segundos

### Requirement: Dados de status servem apenas para exibição
O painel SHALL NOT usar nenhum valor lido de um arquivo de status para montar comandos, argumentos ou caminhos, nem para abrir, executar ou acessar o que esses valores indicam.

#### Scenario: Pasta de trabalho apontando para outro lugar
- **WHEN** um arquivo de status traz em `cwd` um caminho de rede, um executável ou um caminho inexistente
- **THEN** o painel apenas exibe o texto e não acessa esse caminho
