# ambiente-container Specification

## Purpose
Define o ambiente Docker em que o Claude Code e o Codex rodam: o que ele alcança na rede, o que ele vê da máquina e o que fica protegido do próprio agente, para que nenhuma sessão chegue aos servidores da empresa nem às credenciais de quem usa o painel.

## Requirements

### Requirement: Ambiente versionado e iniciado por um comando
O repositório SHALL conter, na pasta `docker`, tudo o que é preciso para construir e subir o ambiente: a imagem do agente com Claude Code, Codex, git e os scripts do painel, e um compose com o serviço do agente e o serviço do proxy. O ambiente SHALL subir com `docker compose up -d` nessa pasta, e o container do agente SHALL ter o nome `datapilot-agente`.

#### Scenario: Primeira instalação
- **WHEN** o usuário executa `docker compose up -d` na pasta `docker` com o Docker Desktop em execução
- **THEN** os containers do agente e do proxy ficam em execução, e o do agente se chama `datapilot-agente`

#### Scenario: Agentes disponíveis
- **WHEN** o usuário executa `docker exec datapilot-agente claude --version` e `docker exec datapilot-agente codex --version`
- **THEN** os dois comandos mostram a versão instalada na imagem

### Requirement: Rede restrita a destinos liberados
O container do agente SHALL NOT ter rota direta para nenhuma rede. Todo acesso de saída SHALL passar pelo proxy, que SHALL permitir só os destinos da lista em `docker/proxy/squid.conf` e recusar os demais. A lista padrão SHALL conter só os destinos que o Claude Code e o Codex precisam para funcionar.

#### Scenario: Servidor da empresa
- **WHEN** um processo dentro do container tenta conectar a um servidor da rede da empresa, pelo proxy ou direto
- **THEN** a conexão é recusada

#### Scenario: API dos agentes
- **WHEN** o Claude Code ou o Codex faz uma requisição à API do seu fornecedor
- **THEN** a requisição passa pelo proxy e é atendida

#### Scenario: Destino fora da lista
- **WHEN** um processo dentro do container pede ao proxy um site que não está na lista
- **THEN** o proxy recusa a requisição e registra a recusa no seu log

### Requirement: Montagens mínimas
O container do agente SHALL ver, da máquina, só a raiz dos fontes em `/workspace` (leitura e escrita), a pasta `C:\openspec\ERP` em `/openspec/ERP` (leitura e escrita), a pasta de status em `/status` (escrita) e volumes próprios do Docker para a configuração e o login dos agentes. O container SHALL NOT ter acesso ao perfil do Windows de nenhum usuário, ao Credential Manager, a chaves SSH do Windows nem ao socket do Docker.

#### Scenario: Repositório visível
- **WHEN** existe a pasta `C:\Fontes\Faturamento`
- **THEN** dentro do container ela aparece como `/workspace/Faturamento`, com o mesmo conteúdo

#### Scenario: Pasta do OpenSpec visível
- **WHEN** existe a pasta `C:\openspec\ERP\Faturamento`
- **THEN** dentro do container ela aparece como `/openspec/ERP/Faturamento`, com o mesmo conteúdo

#### Scenario: Perfil do Windows
- **WHEN** um processo dentro do container procura arquivos do perfil do usuário principal, como `.git-credentials` ou a pasta `.ssh`
- **THEN** nenhum desses arquivos existe dentro do container

#### Scenario: Socket do Docker
- **WHEN** um processo dentro do container procura `/var/run/docker.sock`
- **THEN** o arquivo não existe

### Requirement: Agente sem privilégios no container
Os agentes SHALL rodar no container com um usuário que não é `root`, sem capabilities do Linux e sem poder ganhar privilégios (`no-new-privileges`).

#### Scenario: Tentativa de virar root
- **WHEN** um processo do agente tenta executar `sudo` ou `su`
- **THEN** a tentativa falha

### Requirement: Scripts do painel protegidos dentro da imagem
O script de abertura e o hook de status SHALL ficar em `/opt/painel`, dentro da imagem, pertencendo a `root` e só com leitura e execução para o usuário dos agentes. Uma versão nova dos scripts SHALL chegar ao container só pela reconstrução da imagem.

#### Scenario: Agente tenta alterar o hook
- **WHEN** um processo do agente tenta modificar ou excluir um arquivo em `/opt/painel`
- **THEN** a operação é negada

#### Scenario: Versão nova dos scripts
- **WHEN** um script em `docker/painel` muda e o usuário executa `docker compose up -d --build`
- **THEN** o container passa a usar a versão nova

### Requirement: Hooks configurados pelo ambiente
Os hooks de status do Claude Code e do Codex SHALL vir configurados no ambiente, apontando para o hook em `/opt/painel`, sem passo manual além da confirmação de confiança que o Codex exige. A configuração do Claude Code SHALL ficar num local que o usuário dos agentes não pode alterar. O hook do container SHALL cumprir o mesmo contrato do arquivo de status, do mapa de eventos, da escrita atômica, da validação do identificador e do silêncio definidos em `status-de-sessao`.

#### Scenario: Sessão nova no container
- **WHEN** uma sessão do Claude Code é aberta no container
- **THEN** surge em `C:\ProgramData\Painel\sessoes` o arquivo de status dessa sessão, com `status` igual a `ativa`

#### Scenario: Agente tenta desligar os hooks do Claude
- **WHEN** um processo do agente tenta alterar a configuração de hooks do Claude Code definida pelo ambiente
- **THEN** a operação é negada e os hooks continuam valendo

#### Scenario: Primeira sessão do Codex
- **WHEN** o Codex é aberto pela primeira vez no container
- **THEN** ele pede para confiar nos hooks do painel e, depois de confirmados, grava o status nas sessões seguintes sem nova confirmação

### Requirement: Login persistente
O login e o histórico de conversas dos agentes SHALL ficar em volumes do Docker e SHALL sobreviver a reiniciar, parar ou recriar o container.

#### Scenario: Container recriado
- **WHEN** o usuário fez login no Claude Code e no Codex e recria o container com `docker compose up -d --force-recreate`
- **THEN** os dois agentes continuam autenticados, e as conversas anteriores podem ser retomadas
