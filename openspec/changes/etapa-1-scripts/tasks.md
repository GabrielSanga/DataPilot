# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a conta `ia`, elevação de administrador ou um agente interativo. As demais são feitas no repositório.

## 1. Conferência prévia como usuário ia

- [x] 1.1 **[manual]** Numa sessão `ssh ia@localhost`, rodar `claude --version`, `codex --version`, `claude --help` e `codex resume --help`; verificar que `--session-id`, `--resume` e `codex resume [SESSION_ID]` aparecem e anotar as versões em `docs/instalacao-etapa-1.md`
- [x] 1.2 **[manual]** Configurar temporariamente um hook de `SessionStart` no Codex que grave a entrada padrão num arquivo; verificar no arquivo se `session_id` é um GUID canônico de 36 caracteres. Se não for, parar e rever a spec `abertura-de-sessao` antes do grupo 4
- [ ] 1.3 **[manual]** Do mesmo modo, capturar no Claude um evento `Notification` de pedido de permissão; verificar no arquivo o nome do campo que traz o tipo (esperado: `notification_type`) e anotar no guia

## 2. Referência do projeto

- [x] 2.1 Adicionar `docs/PRD.md` com o PRD exportado em Markdown pelo usuário; verificar que o arquivo abre e contém as seções "Modelo de dados e contratos" e "Requisitos de segurança"

## 3. Hook de status

- [x] 3.1 Criar `scripts/testes/verificar.ps1` com a estrutura de casos (pasta temporária, execução de um script com entrada padrão, conferência de arquivo, saída e código de saída); verificar que roda em `powershell.exe` 5.1 e termina com código 0 sem casos cadastrados
- [x] 3.2 Implementar `scripts/hook.ps1` com leitura do evento, validação de `session_id`, mapa evento → status e parâmetros `-Agente` e `-PastaStatus`; adicionar em `verificar.ps1` um caso por linha do mapa e o de evento desconhecido, e verificar que todos passam
- [x] 3.3 Implementar o filtro de `Notification` por tipo; adicionar casos para `permission_prompt`, `elicitation_dialog` e `idle_prompt` depois de `Stop`, e verificar que passam
- [x] 3.4 Implementar a escrita atômica (temporário único por processo, UTF-8 sem BOM, renomear sobre o destino) e a proteção contra evento mais antigo; adicionar casos que conferem ausência de BOM, JSON completo após dois eventos disparados em paralelo e status preservado quando o arquivo existente é mais novo, e verificar que passam
- [x] 3.5 Adicionar casos de robustez: entrada que não é JSON, `session_id` ausente, `session_id` com `\`, `/` ou `.`, pasta de status inexistente e prompt com texto confidencial; verificar em todos código 0, saída padrão vazia e nenhum arquivo indevido
- [x] 3.6 Verificar que `scripts/hook.ps1` contém apenas caracteres ASCII, com um caso em `verificar.ps1` que falha se houver byte acima de 127
- [x] 3.7 Criar `scripts/exemplos/claude-settings.json` com os hooks de `SessionStart`, `UserPromptSubmit`, `PostToolUse` (assíncrono), `PermissionRequest`, `Notification` (matcher de permissão e pergunta), `Stop` e `SessionEnd` (com `timeout`); verificar que o arquivo é JSON válido com `ConvertFrom-Json`
- [x] 3.8 Criar `scripts/exemplos/codex-hooks.json` com os mesmos eventos, exceto `Notification`, usando `-Agente codex`; verificar que o arquivo é JSON válido com `ConvertFrom-Json`

## 4. Abertura de sessão

- [x] 4.1 Implementar `scripts/abrir.ps1` com a constante da raiz, as validações de agente, modo, repositório e sessão, e a exigência de `-Sessao` por combinação de agente e modo; adicionar em `verificar.ps1` casos para `-Repo ..`, `-Repo` só com pontos, `-Repo` com espaço, `;`, `&`, `|`, `\` e `/`, `-Sessao` com 36 hifens, agente desconhecido, `claude novo` sem sessão e `retomar` sem sessão, e verificar que todos terminam com código diferente de 0
- [x] 4.2 Implementar a resolução da pasta (diretório existente cujo pai é a raiz) e a mensagem de repositório não encontrado; adicionar caso com `-Repo` válido e inexistente e verificar código diferente de 0 e a mensagem
- [x] 4.3 Implementar o início e a retomada dos agentes nas quatro combinações de agente e modo, repassando o código de saída do agente; verificar por leitura que cada combinação usa as opções conferidas na tarefa 1.1
- [x] 4.4 Verificar que `scripts/abrir.ps1` contém apenas caracteres ASCII, com o mesmo caso de `verificar.ps1` usado no hook

## 5. Guia de instalação

- [x] 5.1 Escrever em `docs/instalacao-etapa-1.md` os passos de instalação na ordem do plano de migração do design, com os comandos de criação de pastas e de ACL usando SIDs, a cópia dos scripts, a mescla dos exemplos nos arquivos do `ia` e a confiança dos hooks no Codex; verificar que cada comando do guia está completo e pode ser colado como está
- [x] 5.2 Acrescentar ao guia o roteiro de verificação manual (um item por critério de aceite da Etapa 1 e pelas três questões em aberto) e a reversão; verificar que cada item diz o que fazer e o que deve ser observado

## 6. Instalação e verificação integrada

- [ ] 6.1 **[manual]** Como administrador, criar as pastas e aplicar as ACLs do guia; verificar com `icacls` que o `ia` tem modificação em `C:\ProgramData\Painel`, só leitura e execução em `C:\Painel`, e que o usuário principal sem elevação não consegue criar arquivo na pasta de status
- [ ] 6.2 **[manual]** Copiar os scripts para `C:\Painel` e rodar `verificar.ps1` como `ia`; verificar que todos os casos passam nessa conta
- [ ] 6.3 **[manual]** Mesclar os exemplos na configuração do `ia`, confiar nos hooks do Codex em `/hooks` e usar uma sessão de cada agente; verificar que o arquivo de status de cada sessão é criado e passa por `ativa`, `executando`, `atencao` (ao pedir permissão), de volta a `executando`, `aguardando` e `encerrada`, e que nada estranho aparece no contexto do agente
- [x] 6.4 **[manual]** Pelo usuário principal, executar a linha de SSH do guia para abrir uma sessão nova do Claude com um GUID escolhido e depois retomá-la, e o mesmo para o Codex; verificar que a sessão do Claude usa o GUID informado e que as retomadas abrem a conversa certa
- [x] 6.5 **[manual]** Responder e registrar no guia: se `/clear` no Claude muda o `session_id`; se fechar a aba do terminal dispara `SessionEnd`; e se uma sessão retomada mantém o mesmo arquivo de status

## Workflow follow-up

- Levar os resultados da tarefa 6.5 para o planejamento das Etapas 2 e 4.
- Arquivar a change depois que as verificações manuais do grupo 6 estiverem concluídas.
