# Proposal

## Why

O isolamento atual, que roda os agentes como o usuário `ia` pelo SSH local, tira do alcance deles as credenciais de quem usa o painel (Credential Manager, tickets Kerberos, perfil do Windows). Mas não tira a rede: o `ia` alcança os servidores da empresa como qualquer outro usuário da máquina. Basta uma connection string esquecida num `appsettings.json` em `C:\Fontes`, ou um serviço interno sem autenticação, para o agente agir num ambiente real. O Firewall do Windows não filtra a saída por usuário de forma prática, então essa lacuna não fecha com o modelo atual.

Rodar os agentes num container Docker fecha a lacuna. O container não tem rota direta para nenhuma rede, e a única saída é um proxy que libera só os destinos de uma lista. Ele também não enxerga o perfil nem as credenciais do Windows, e de quebra dispensa o OpenSSH Server, o usuário `ia` e as permissões especiais das pastas.

## What Changes

- **Novo ambiente de execução "Container Docker", padrão para sessões novas.** O ambiente fica versionado na pasta `docker/` do repositório:
  - uma imagem com Claude Code, Codex e git;
  - um compose com dois serviços: o agente, numa rede `internal`, e um proxy com lista de destinos liberados.
- **O container só vê o necessário:**
  - a raiz dos fontes (`C:\Fontes` → `/workspace`), com qualquer pasta filha;
  - a pasta `C:\openspec\ERP` (→ `/openspec/ERP`);
  - a pasta de status (`C:\ProgramData\Painel\sessoes` → `/status`);
  - volumes próprios para o login dos agentes.
  
  Não vê o perfil do Windows, credenciais nem o socket do Docker. Roda com usuário sem privilégios e sem capabilities.
- **Scripts do painel dentro da imagem.** Um `abrir.sh` com as mesmas regras do `abrir.ps1` e um hook com o mesmo contrato do `hook.ps1` ficam em `/opt/painel`, sem permissão de escrita para o agente. Os hooks do Claude e do Codex já vêm configurados na imagem.
- **Abertura pelo Windows Terminal, como hoje.** A aba passa a rodar `docker exec` no container em vez de `ssh`. O terminal dentro do painel fica para a change `terminal-embutido`.
- **Ambiente verificado antes de abrir.** Se o Docker não responde ou o container não existe, o painel não abre a aba, desfaz o registro e explica o que fazer. Se o container existe mas está parado, o painel o inicia.
- **Configurações:**
  - nova opção "Executar os agentes em", com os valores "Container Docker" (padrão) e "Windows";
  - novo campo com o nome do container (`datapilot-agente` por padrão), validado como os demais valores que entram em comandos;
  - "Abrir pelo SSH" e o usuário do SSH continuam valendo para o modo Windows.
- **Cada sessão lembra onde foi aberta.** O registro da sessão e o lançamento pendente guardam o ambiente (`container` ou `windows`). A retomada usa sempre o ambiente do registro, porque uma conversa criada no container não existe no Windows e vice-versa. Registros antigos, sem o campo, valem como `windows`.
- **BREAKING (comportamento padrão):** sessões novas passam a abrir no container. Quem ainda não instalou o ambiente vê a mensagem de ambiente indisponível e pode instalá-lo ou escolher "Windows" nas configurações. As sessões já registradas continuam retomando no Windows.

## Capabilities

### New Capabilities

- `ambiente-container`: o ambiente Docker dos agentes (imagem, rede restrita, montagens, usuário sem privilégios, scripts e hooks protegidos, login persistente).

### Modified Capabilities

- `configuracoes`: ambiente de execução e nome do container.
- `lancamento-de-sessao`: escolha do ambiente por sessão, comando com `docker exec` e verificação do container.
- `abertura-de-sessao`: script de abertura dentro do container, com as mesmas regras.
- `status-de-sessao`: a mesma pasta de status, vista pelo container como `/status`, e as permissões em cada ambiente.
- `dados-do-painel`: o ambiente guardado no registro da sessão e no lançamento pendente.
- `leitura-de-status`: a data de início da sessão vem do marcador `.criado`, porque a montagem do container não preserva a data de criação do arquivo substituído.

## Impact

- **Código:**
  - `Models`: `Configuracoes` (ambiente e nome do container), `SessaoRegistrada` e `LancamentoPendente` (ambiente);
  - `Constantes`;
  - `ValidadorEntrada`: nome do container;
  - `DadosPainel`: ler e gravar os campos novos;
  - `MontadorComando`: comando do container;
  - `DisparadorProcesso`: verificação do container antes de abrir;
  - `AberturaSessao`: ambiente por sessão;
  - `VinculoAutomatico`: o ambiente do lançamento passa para a sessão;
  - `ConfiguracoesWindow`.
- **Arquivos novos:** `docker/Dockerfile`, `docker/compose.yml`, `docker/proxy/squid.conf`, `docker/painel/abrir.sh`, `docker/painel/hook.mjs`, a configuração dos hooks e os testes dos scripts em `docker/testes/`.
- **Testes:** montador (container e Windows), validação do nome do container, leitura e gravação do ambiente, migração de registros sem o campo, verificação do container com um disparador falso e testes dos scripts do container.
- **Documentação:**
  - guia novo `docs/instalacao-container.md`;
  - PRD: o isolamento passa a ser pelo container, e o usuário `ia` fica como alternativa;
  - README e `docs/arquitetura.md`.
- **Dados:** o `demandas.json` ganha os campos opcionais `execucao` (nas configurações, nas sessões e nos lançamentos) e `container` (nas configurações). Arquivos sem esses campos continuam válidos.
- **Fora do escopo:**
  - terminal dentro do painel (change `terminal-embutido`);
  - proteção do `.git` contra escrita;
  - acesso ao TFS e a outros servidores internos. A lista do proxy traz o TFS como exemplo comentado, para liberar com um PAT somente leitura.
- **Ordem:** implementar depois de `refatorar-estrutura`, `trabalhos-e-sessoes`, `configuracoes-do-painel` e `scripts-do-painel` estarem arquivadas, porque esta change modifica requisitos que elas criam ou alteram.
