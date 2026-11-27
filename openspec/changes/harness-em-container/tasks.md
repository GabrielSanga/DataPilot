# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem o Docker Desktop, login nos agentes ou a área de trabalho do Windows. Ao fim de cada grupo de código, `dotnet build` sem avisos e `dotnet test` passando.

## 1. Ambiente do container

- [ ] 1.1 Criar `docker/Dockerfile` (base `node:22-bookworm`, git, ripgrep, ca-certificates, Claude Code e Codex fixados por `ARG`, `/opt/painel` com dono `root`, usuário `node`), `docker/compose.yml` (serviços `agente` e `proxy`, redes `interna` com `internal: true` e `externa`, montagens e volumes da decisão 3, `cap_drop: [ALL]`, `no-new-privileges`, `TZ`, `RAIZ_FONTES`, `CLAUDE_CONFIG_DIR`, `DISABLE_AUTOUPDATER`), `docker/entrypoint.sh` e `docker/proxy/squid.conf` (lista mínima mais o TFS como exemplo comentado); **[manual]** subir com `docker compose up -d`, abrir o Claude e o Codex com `docker exec -it`, conferir no `access.log` do squid os domínios que eles usam e fechar a lista; verificar que `curl` para um servidor da empresa e para um site fora da lista falham, e que `/var/run/docker.sock` e o perfil do Windows não existem no container
- [x] 1.2 **[manual]** Verificar a preservação da data de criação (decisão 7): de dentro do container, gravar `/status/teste.json` por temporário e `rename`, esperar 20 s, regravar da mesma forma e conferir no Windows (`(Get-Item C:\ProgramData\Painel\sessoes\teste.json).CreationTime`) que a data é a da primeira gravação; se não for, atualizar esta change com o plano B (delta em `leitura-de-status`) antes do grupo 3

## 2. Scripts do container

- [x] 2.1 Criar `docker/painel/abrir.sh` com as regras do `abrir.ps1` (parâmetros com `--`, raiz `/workspace`, códigos 2, 3 e 4, segunda tentativa do Claude em menos de 30 s) e `docker/testes/verificar.sh` com os casos portados do `scripts/testes/verificar.ps1` (parâmetros inválidos, `--repo ..`, repositório inexistente, agente ausente, retomada com `claude` e `codex` falsos); rodar `verificar.sh` dentro do container e verificar que todos os casos passam
- [x] 2.2 Criar `docker/painel/hook.mjs` com o contrato de `status-de-sessao` (seis campos, mapa de eventos, filtro de `Notification`, não regredir, validação do `session_id`, temporário e `rename`, silêncio e código 0) e acrescentar ao `verificar.sh` os casos do hook (entrada inválida, evento desconhecido, identificador com `/`, pasta ausente, evento fora de ordem, prompt que não vai para o arquivo); verificar que passam
- [ ] 2.3 Criar `docker/painel/claude-managed-settings.json` (hooks para `/opt/painel/hook.mjs`, instalado em `/etc/claude-code/managed-settings.json` com dono `root`) e `docker/painel/codex-hooks.json` (mantido em `~/.codex/hooks.json` pelo `entrypoint.sh`); **[manual]** abrir uma sessão do Claude e uma do Codex (confiando nos hooks pelo `/hooks`) e conferir que os arquivos de status surgem em `C:\ProgramData\Painel\sessoes` com `cwd` em `/workspace/...`; tentar alterar a configuração gerenciada como `node` e conferir que é negado
- [x] 2.4 Plano B da decisão 7: o `hook.mjs` cria o marcador `<id>.criado` antes do primeiro status e não o regrava; acrescentar os casos ao `verificar.sh` e verificar que passam no container
- [x] 2.5 Dois locais (decisão 11): montar `C:\openspec\ERP` no `compose.yml`, aceitar `--caminho` no `abrir.sh`, acrescentar os casos ao `verificar.sh` e verificar que passam no container

## 3. Configurações e registros no Core

- [x] 3.1 Acrescentar `ContainerValido` a `ValidadorEntrada` (decisão 8), com testes de `datapilot-agente`, `agente_1`, `--privileged`, `.x`, vazio e 64 caracteres; verificar que passam
- [x] 3.2 Acrescentar `Execucao` e `Container` a `Configuracoes` e ler e gravar `execucao` e `container` em `DadosPainel` (ausente ou inválido vale o padrão; configuração antiga só com SSH vale `container`); cobrir com testes os cenários de "Valores padrão" e "Configurações gravadas no arquivo de dados" e verificar que passam
- [x] 3.3 Acrescentar `Execucao` a `SessaoRegistrada` e `LancamentoPendente` e ler e gravar `execucao` nos registros (ausente vale `windows`, desconhecido desconsidera o registro); fazer o `VinculoAutomatico` copiar o ambiente do lançamento; cobrir com testes os cenários de "Ambiente de execução nos registros" e de "Registro das sessões abertas pelo painel" e verificar que passam
- [x] 3.4 Plano B da decisão 7: o `LeitorPasta` usa a data de criação do marcador `<id>.criado`, quando existir; cobrir com testes os cenários de "Data de criação do arquivo de status" e verificar que passam

## 4. Abertura no container

- [x] 4.1 Fazer o `MontadorComando` montar o comando do container (decisão 9) e revalidar o nome do container; a `AberturaSessao` passa o ambiente certo em `Nova`, `Retomar`, `ReabrirLancamento` e `RetomarNaoRegistrada`; cobrir com testes os cenários de "Composição do comando no container", "Locais disponíveis no container" (incluindo a associação da sessão ao repositório), "Composição do comando", "Ambiente de execução da sessão" e "Validação imediatamente antes de lançar" e verificar que passam
- [x] 4.2 Criar o `DisparadorContainer` (verificação com `docker inspect`, `docker start` do container parado, tempo limite e mensagens de orientação) atrás de uma abstração de execução de processo que os testes substituem; cobrir com testes Docker ausente, container inexistente, container parado que inicia, container que não inicia e tempo limite, e verificar que o registro é desfeito nos casos de falha
- [ ] 4.3 Tornar a abertura assíncrona no `MainViewModel`, com o botão desabilitado durante a verificação; **[manual]** com o Docker Desktop fechado, acionar "Nova sessão" e conferir que a janela continua respondendo e a mensagem aparece

## 5. Tela

- [ ] 5.1 Acrescentar ao `ConfiguracoesWindow` "Executar os agentes em" (Container Docker / Windows) e o nome do container (desabilitado com Windows, erro junto do campo), mantendo os campos de SSH editáveis; **[manual]** abrir o painel com os dados de exemplo e conferir o diálogo, o campo desabilitado e o erro de nome inválido

## 6. Documentação

- [ ] 6.1 Criar `docs/instalacao-container.md` (Docker Desktop com WSL2, `docker compose up -d`, login dos agentes, confiança nos hooks do Codex, como conferir e ampliar a lista do proxy, como liberar o TFS com PAT somente leitura, como atualizar os CLIs e como voltar para "Windows"); verificar que os comandos citados funcionam como escritos
- [x] 6.2 Atualizar o PRD (o isolamento passa a ser pelo container; o usuário `ia` vira alternativa; RS-7), o README (requisitos com Docker Desktop) e `docs/arquitetura.md` (`docker/`, ambiente por sessão, `DisparadorContainer`); verificar que os caminhos e nomes citados existem

## 7. Verificação manual

- [ ] 7.1 **[manual]** Com "Container Docker" escolhido, abrir e retomar uma sessão do Claude e uma do Codex num trabalho; conferir status, vínculo automático do Codex e retomada com histórico
- [ ] 7.2 **[manual]** Com uma sessão registrada antes desta versão, retomar e conferir que abre no Windows; trocar para "Windows", abrir uma sessão nova e conferir que usa o SSH
- [ ] 7.3 **[manual]** Parar o container (`docker stop datapilot-agente`), acionar "Nova sessão" e conferir que o painel o inicia e abre a aba; remover o container e conferir a mensagem e que nada ficou registrado
- [ ] 7.4 **[manual]** Dentro de uma sessão no container, pedir ao agente que acesse um servidor da empresa e que leia um arquivo do perfil do Windows; conferir que as duas tentativas falham

## Workflow follow-up

- Implementar depois de `refatorar-estrutura`, `trabalhos-e-sessoes`, `configuracoes-do-painel` e `scripts-do-painel` estarem arquivadas.
- A change `terminal-embutido` depende desta.
- Arquivar esta change depois da verificação manual.
