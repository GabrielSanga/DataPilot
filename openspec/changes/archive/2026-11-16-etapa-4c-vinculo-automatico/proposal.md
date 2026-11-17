# Proposal

## Why

Uma sessão do Codex aberta pelo próprio painel, dentro de uma demanda, aparece em "Não vinculadas" e só entra na demanda depois de um vínculo manual. Para quem abriu a sessão a partir de um card, isso é um passo que o painel deveria resolver sozinho: ele sabe em que demanda e repositório o usuário clicou. O identificador da sessão do Codex só passa a existir depois, criado pelo próprio Codex, então o painel precisa de uma forma de reconhecer, entre as sessões que surgem na pasta de status, a que nasceu daquele clique.

## What Changes

- Ao abrir uma sessão nova do Codex pelo card, o painel registra no arquivo de dados um lançamento pendente naquele repositório, com o agente, o rótulo informado e a data.
- Quando surge uma sessão do Codex ainda não registrada, numa pasta com o nome do repositório e cujo arquivo de status foi criado depois do lançamento, o painel a registra naquela demanda e naquele repositório, com o rótulo do lançamento, sem outra ação do usuário.
- Cada lançamento vincula uma única sessão e deixa de existir ao ser usado. Um lançamento não usado vale por 24 horas.
- Havendo mais de um lançamento pendente para a mesma pasta, vale o mais recente entre os anteriores à criação da sessão.
- O painel passa a conhecer a data de criação do arquivo de status de cada sessão, lida do sistema de arquivos.
- O rótulo informado em "Nova sessão" do Codex, que hoje só aparece no título da aba, passa a ficar gravado na sessão.
- O vínculo manual em "Não vinculadas" continua, para sessões abertas fora do painel e para as que surgem depois da primeira no mesmo terminal, como a criada por `/clear` no Claude.
- **BREAKING** O painel passa a gravar no arquivo de dados sem uma ação do usuário sobre a sessão, restrito a lançamentos que o usuário iniciou.
- **BREAKING** A pasta de trabalho e a data de criação do arquivo de status deixam de servir só para exibição: passam a ser comparadas com os lançamentos pendentes. Continuam sem ser gravadas e sem compor comandos.

Os scripts instalados em `C:\Painel` não mudam de comportamento, e a configuração de hooks dos agentes não muda. O único arquivo de script alterado é o de verificação, que ganha um caso novo.

Fora desta change: vincular automaticamente sessões abertas fora do painel ou criadas depois da primeira no mesmo terminal, mostrar o lançamento pendente no card, progresso do OpenSpec, bandeja e notificações.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `vinculo-de-sessao`: vínculo automático por lançamento pendente, ao lado do vínculo manual.
- `dados-do-painel`: registro dos lançamentos pendentes no arquivo de dados e ampliação da exceção de gravação para o vínculo por lançamento.
- `leitura-de-status`: o painel conhece a data de criação do arquivo de status e pode compará-la, com o agente e o nome da pasta, aos lançamentos pendentes.
- `status-de-sessao`: a atualização do arquivo de status preserva a data de criação do arquivo, o que o hook já faz e passa a ser verificado.

## Impact

- **Scripts:** `scripts/hook.ps1` e `scripts/abrir.ps1` não mudam. `scripts/testes/verificar.ps1` ganha um caso para a data de criação.
- **Código:** data de criação na leitura da pasta de status, registro de lançamentos e função de correspondência no `DataPilot.Core`; a abertura de sessão nova do Codex passa a registrar o lançamento; o painel aplica os vínculos a cada leitura da pasta de status.
- **Dados:** repositórios ganham a lista `lancamentos`. Arquivos existentes continuam válidos.
- **Segurança:** o usuário `ia` pode forjar um arquivo de status que ocupe um lançamento pendente. O efeito é limitado a uma sessão por clique do usuário, no card em que ele clicou; o que cruza para os dados continua sendo só o identificador da sessão e o agente. A avaliação está no `design.md`.
- **Dependências:** nenhum pacote novo.
