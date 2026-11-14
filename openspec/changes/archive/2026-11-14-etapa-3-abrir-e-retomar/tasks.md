# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem olhar a janela ou usar agentes reais na conta `ia`.

## 1. Ensaio do disparo

- [x] 1.1 Escrever um ensaio descartável que inicia `wt.exe` com `ProcessStartInfo.ArgumentList`, sem shell, usando `-w painel-ensaio nt --title "Ensaio · DataPilot" --suppressApplicationTitle` e um comando inofensivo; executá-lo uma vez (abre uma aba de terminal na área de trabalho) e verificar que o processo inicia e que a aba mostra o título com o separador correto. Se falhar, parar e rever a decisão 2 do design antes de seguir
- [x] 1.2 Registrar no `design.md` o resultado do ensaio e remover o código descartável; verificar que nada do ensaio ficou na solution

## 2. Validação e modelo dos dados do painel

- [x] 2.1 Implementar no Core a validação de pasta, identificador e rótulo num único tipo; cobrir com testes os casos das specs (`..`, só pontos, espaço, `\`, `/`, `;`, GUID com hifens fora do lugar, 36 hifens, rótulo com 41 caracteres, rótulo com `;`, rótulo com caractere de controle) e verificar que passam
- [x] 2.2 Criar os tipos de repositório cadastrado e de sessão registrada, separados do tipo de sessão lida da pasta de status; verificar que o projeto compila e que os tipos novos não têm nenhum campo vindo de arquivo de status

## 3. Arquivo de dados

- [x] 3.1 Implementar a leitura do `demandas.json` como árvore, com estado vazio para arquivo inexistente e desconsiderando repositórios e sessões inválidos; cobrir com testes os cenários de "Arquivo de dados do painel" e "Validação ao carregar" e verificar que passam
- [x] 3.2 Implementar cadastro e remoção de repositórios, com a demanda implícita, a recusa de duplicados e a gravação por temporário e troca; cobrir com testes os cenários de "Demanda implícita", "Cadastro de repositórios" e "Remoção de repositório" e verificar que passam
- [x] 3.3 Implementar o registro e o desfazer de sessão; cobrir com testes os cenários de "Registro das sessões abertas pelo painel" e verificar que passam
- [x] 3.4 Tratar arquivo ilegível (nenhuma gravação, estado de dados indisponíveis) e preservar campos desconhecidos; cobrir com testes os cenários de "Arquivo ilegível não é sobrescrito" e "Gravação segura", incluindo a conferência de que o conteúdo corrompido fica byte a byte igual, e verificar que passam

## 4. Montagem e disparo do comando

- [x] 4.1 Implementar o montador de comando sobre os tipos do arquivo de dados, com a validação repetida e o título da aba; cobrir com testes a lista exata de argumentos das quatro combinações de agente e modo, os três cenários de título e a recusa de cada valor inválido, e verificar que passam
- [x] 4.2 Implementar a interface de disparo e a implementação real com `ArgumentList` e sem shell, tratando a falha ao iniciar; cobrir com teste a falha de um executável inexistente e verificar que passa
- [x] 4.3 Implementar as funções puras de correspondência entre sessões da lista e sessões registradas (identificador e agente) e de decisão do aviso de sessão possivelmente aberta; cobrir com testes os cenários de "Ação de retomar na linha" e "Aviso de sessão possivelmente aberta" e verificar que passam
- [x] 4.4 Implementar a operação de nova sessão (gerar GUID, registrar, disparar, desfazer na falha; Codex sem registro) usando um disparador falso nos testes; cobrir os cenários de "Registro das sessões abertas pelo painel" e "Identificador gerado pelo painel" e verificar que passam

## 5. Interface

- [x] 5.1 Adicionar o diálogo "Repositórios" (listar, adicionar com mensagem de recusa, remover com confirmação quando houver sessões); verificar com `--dados` apontando para um arquivo temporário que cada ação reflete no arquivo
- [x] 5.2 Adicionar "Nova sessão" (repositório, agente, rótulo opcional, orientação quando não há repositórios) ligada à operação do Core; verificar com `--dados` e o disparador real que a aba abre e que a sessão do Claude fica registrada
- [x] 5.3 Adicionar o botão "Retomar" e a indicação de repositório cadastrado e rótulo nas linhas conhecidas, com o pedido de confirmação e as mensagens de erro; verificar com a pasta de exemplo e um arquivo de dados de exemplo que só as linhas registradas mostram o botão
- [x] 5.4 Adicionar o aviso de dados indisponíveis e desabilitar as ações nesse estado, e o parâmetro `--dados`; verificar iniciando o painel com um arquivo de dados corrompido
- [x] 5.5 Atualizar o `README.md` com as ações novas, o local do arquivo de dados, o parâmetro `--dados` e o roteiro de verificação manual; verificar que os comandos documentados funcionam como escritos

## 6. Verificação integrada

- [x] 6.1 **[manual]** Cadastrar um repositório que exista em `C:\Fontes`, abrir uma sessão nova do Claude pelo painel e verificar que a aba abre com o título esperado, que o arquivo de status tem o GUID registrado no `demandas.json` e que a linha mostra "Retomar"
- [x] 6.2 **[manual]** Encerrar essa sessão e acionar "Retomar"; verificar que a conversa certa abre, sem pedido de confirmação. Repetir com a sessão aberta e verificar o pedido de confirmação
- [x] 6.3 **[manual]** Abrir uma sessão nova do Codex pelo painel; verificar que a aba abre, que a sessão aparece na lista sem "Retomar" e que o `demandas.json` não mudou
- [x] 6.4 **[manual]** Numa sessão do Claude aberta pelo painel, executar `/clear`; verificar que a sessão nova aparece sem "Retomar" e a original continua com o botão
- [x] 6.5 **[manual]** Verificar que o `ia` não consegue ler nem alterar o arquivo de dados: `ssh ia@localhost powershell -NoProfile -Command Get-Content $env:APPDATA\Painel\demandas.json`, executado pelo usuário principal, deve ser negado

## Workflow follow-up

- Levar para a Etapa 4 o que o uso mostrar sobre sessões sem botão de retomar e sobre o cadastro de repositórios.
- Arquivar a change depois que as verificações manuais do grupo 6 estiverem concluídas.
