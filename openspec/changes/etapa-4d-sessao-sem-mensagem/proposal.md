# Proposal

## Why

Uma sessão que ainda não recebeu a primeira mensagem se comporta mal no painel, de um jeito diferente em cada agente:

- **Claude:** a sessão aparece no card assim que é aberta, mas, se for fechada antes da primeira mensagem, "Retomar" falha. O Claude só grava a conversa depois da primeira mensagem, e a retomada não encontra o que abrir.
- **Codex:** a sessão só aparece no card depois da primeira mensagem, porque o Codex só roda o hook de início nesse momento. Entre o clique e a primeira mensagem, o card não mostra nada, e um lançamento de uma aba fechada sem mensagem fica pendente por 24 horas sem que o usuário veja ou possa desfazer.

## What Changes

- O `abrir.ps1`, ao retomar uma sessão do Claude que ainda não tem conversa gravada, passa a iniciar uma sessão nova com o mesmo identificador, em vez de terminar com erro.
- O card de um repositório passa a mostrar o lançamento pendente do Codex assim que o usuário abre a sessão, como uma linha "aguardando a primeira mensagem", com o agente, o rótulo e o tempo decorrido.
- A linha do lançamento pendente é substituída pela sessão quando a primeira mensagem chega e o vínculo automático acontece.
- A linha oferece "Cancelar", que remove o lançamento pendente. Não oferece "Retomar", "Mover" nem "Remover".
- Lançamentos pendentes não entram na contagem de sessões nem no status das demandas.

O `abrir.ps1` muda e precisa ser copiado de novo para `C:\Painel`. O painel novo funciona com o script antigo e o script novo funciona com o painel antigo, então a ordem de instalação não importa. O `hook.ps1` e a configuração de hooks dos agentes não mudam.

Fora desta change: os status do Codex ao fechar a aba e ao retomar, que ficam como estão; vincular sozinho a sessão criada por `/clear`; progresso do OpenSpec, bandeja e notificações.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `abertura-de-sessao`: a retomada de uma sessão do Claude sem conversa gravada inicia a sessão com o mesmo identificador.
- `lista-de-sessoes`: o card mostra os lançamentos pendentes, fora das contagens e do status das demandas.
- `vinculo-de-sessao`: o usuário pode cancelar um lançamento pendente.

## Impact

- **Scripts:** `scripts/abrir.ps1` e `scripts/testes/verificar.ps1` mudam; o `abrir.ps1` precisa ser reinstalado em `C:\Painel`.
- **Código:** a visão agrupada do `DataPilot.Core` passa a levar os lançamentos pendentes de cada card; o `DataPilot.Wpf` ganha a linha do lançamento e a ação "Cancelar".
- **Dados:** nenhum campo novo. A linha mostra o que a etapa 4c já grava em `lancamentos`.
- **Segurança:** nada lido da pasta de status entra na linha do lançamento nem no cancelamento; os dois usam só o arquivo de dados. No `abrir.ps1`, a segunda tentativa usa o mesmo identificador já validado, sem nenhum parâmetro novo.
- **Dependências:** nenhum pacote novo. Esta change parte da etapa 4c, que ainda não foi arquivada.
