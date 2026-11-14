# Proposal

## Why

O painel já mostra o estado de cada sessão, mas para agir sobre ela o usuário ainda volta ao terminal e digita a linha de SSH à mão. A Etapa 3 fecha esse ciclo: abrir uma sessão nova ou voltar a uma conversa passa a ser um clique, numa aba do Windows Terminal já no repositório certo e rodando como o usuário `ia`. É a primeira vez que o painel executa algo, então é aqui que a regra central de segurança do PRD entra em vigor: comandos são montados só com dados que o próprio usuário principal forneceu.

## What Changes

- Novo arquivo de dados do painel, `%APPDATA%\Painel\demandas.json`, no formato do PRD, com uma única demanda implícita ("Sem demanda") nesta etapa.
- Cadastro de repositórios no painel: o usuário informa o nome da pasta, validado pela mesma regra do `abrir.ps1`.
- Botão "Nova sessão": escolhe repositório, agente e um rótulo opcional, e abre uma aba do Windows Terminal que chama o `abrir.ps1` pelo SSH local.
  - Claude: o painel gera o GUID da sessão e a registra no `demandas.json` antes de abrir.
  - Codex: a aba é aberta e nada é registrado, porque o identificador só aparece depois, pelo hook.
- Botão "Retomar" nas linhas da lista cujo identificador está registrado no `demandas.json`, com pedido de confirmação quando a sessão parece ainda estar aberta.
- Montagem do comando `wt` + `ssh` + `abrir.ps1` como lista de argumentos separados, sem shell, com validação de cada valor imediatamente antes do uso.

Limites conhecidos desta etapa, decididos na exploração:

- Sessões que o painel não abriu não podem ser retomadas por ele. Isso inclui toda sessão do Codex e a sessão nova que o Claude cria depois de um `/clear`. O vínculo dessas sessões fica para a Etapa 4.
- A lista continua vindo da pasta de status. Uma sessão registrada que nunca ganhou arquivo de status não aparece.

Fora desta change: criar, editar e arquivar demandas, vincular sessões, progresso do OpenSpec, bandeja e notificações.

## Capabilities

### New Capabilities

- `dados-do-painel`: o arquivo `demandas.json` do usuário principal — formato, cadastro de repositórios, registro das sessões abertas pelo painel, validação ao carregar e gravação segura.
- `lancamento-de-sessao`: abrir e retomar sessões a partir do painel — as ações oferecidas, a composição exata do comando, a validação e os avisos.

### Modified Capabilities

- `lista-de-sessoes`: a linha de uma sessão registrada passa a oferecer a ação de retomar e a indicar o repositório cadastrado.

## Impact

- **Código:** novos tipos no `DataPilot.Core` (validação de entradas, dados do painel, montagem e disparo do comando) e novas telas e botões no `DataPilot.Wpf`; novos testes no `DataPilot.Core.Tests`.
- **Máquina:** o painel passa a gravar em `%APPDATA%\Painel\` do usuário principal e a iniciar `wt.exe`. Depende do Windows Terminal instalado e do SSH local já configurado.
- **Scripts da Etapa 1:** sem alteração. O painel usa a linha de comando do `abrir.ps1` como contrato.
- **Segurança:** implementa RS-1 e RS-2 (nenhum valor de arquivo de status entra em comando; argumentos separados, sem shell), o lado do painel de RS-3 (validação de GUID e de pasta) e a parte do painel em RS-4 (só o usuário principal escreve o `demandas.json`).
- **Dependências:** nenhum pacote novo.
