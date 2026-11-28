# Instalação do ambiente em container

O DataPilot roda o Claude Code e o Codex num container Docker. O container não tem rota para nenhuma rede (a única saída é um proxy com lista de destinos), não vê o perfil nem as credenciais do Windows e roda com um usuário sem privilégios. Este guia instala e confere esse ambiente. Os comandos são para o PowerShell, na raiz deste repositório.

O que o container enxerga do computador:

| No Windows | No container | Acesso |
|---|---|---|
| `C:\Fontes`, com as pastas filhas | `/workspace` | leitura e escrita |
| `C:\openspec\ERP`, com as pastas filhas | `/openspec/ERP` | leitura e escrita |
| `C:\ProgramData\Painel\sessoes` | `/status` | leitura e escrita |
| volumes `datapilot-claude` e `datapilot-codex` | login e conversas dos agentes | do agente |

O painel só vincula e abre repositórios de `C:\Fontes` e `C:\openspec\ERP`. Se `C:\openspec\ERP` não existir, o Docker a cria vazia.

**Antes de trabalhar com o agente, faça commit.** Ele pode escrever nas pastas montadas, inclusive no `.git`.

## 1. Docker Desktop

Instale o Docker Desktop com o backend WSL2 e deixe-o aberto.

```powershell
docker version
```

## 2. Permissão da pasta de status

O container grava o status com a conta que roda o Docker Desktop, normalmente a sua conta principal. A pasta precisa existir e essa conta precisa poder modificá-la. Abra o PowerShell **como administrador**:

```powershell
New-Item -ItemType Directory -Force C:\ProgramData\Painel\sessoes | Out-Null
```

```powershell
icacls C:\ProgramData\Painel\sessoes /grant "$(whoami):(OI)(CI)M"
```

Se você elevou com outra conta de administrador, troque `$(whoami)` pelo nome da conta principal.

Isso é necessário em quem tem a pasta criada por uma versão anterior do DataPilot, que deixava a conta principal só com leitura. Com a permissão nova, um programa da conta principal também consegue gravar arquivos de status. O painel continua tratando todo arquivo dessa pasta como não confiável: só exibe e compara os valores.

## 3. Subir o ambiente

```powershell
docker compose -f docker\compose.yml up -d
```

A primeira vez baixa as imagens e constrói a do agente. Confira:

```powershell
docker compose -f docker\compose.yml ps
```

```powershell
docker exec datapilot-agente claude --version
```

```powershell
docker exec datapilot-agente codex --version
```

Para usar outra pasta no lugar de `C:\Fontes`, defina `RAIZ_FONTES` antes de subir (e abra o painel com `--raiz-fontes` na mesma pasta). Nesse caso, só os repositórios diretos dessa pasta e os de `C:\openspec\ERP` abrem no container.

## 4. Login dos agentes

Uma vez só. O login fica nos volumes e sobrevive a recriar o container.

Claude Code (use `/login`, abra o endereço no navegador do Windows e cole o código):

```powershell
docker exec -it datapilot-agente claude
```

Codex, por código de dispositivo (o retorno em `localhost` do login comum não funciona na rede do container):

```powershell
docker exec -it datapilot-agente codex login --device-auth
```

Ou com uma chave de API que já esteja na variável `OPENAI_API_KEY`:

```powershell
$env:OPENAI_API_KEY | docker exec -i datapilot-agente codex login --with-api-key
```

## 5. Confiar nos hooks do Codex

Os hooks de status já vêm configurados. O Claude os usa direto; o Codex pede confirmação. Abra-o, digite `/hooks`, revise e marque como confiáveis:

```powershell
docker exec -it datapilot-agente codex
```

A confiança é ligada ao conteúdo: só é pedida de novo quando o hook mudar numa versão nova do ambiente. Sem ela, o status das sessões do Codex não aparece.

### Limitação conhecida: sandbox do Codex

A sandbox própria do Codex (`bubblewrap`) **não funciona** neste container, que não tem as permissões para criar namespaces. Os comandos que o Codex tenta rodar nela falham com `bwrap: No permissions to create a new namespace`. Desligar essa sandbox (`sandbox_mode = "danger-full-access"` em `/home/node/.codex/config.toml`) deixa o isolamento só por conta do container. O ambiente **não** faz isso sozinho.

## 6. Conferir o isolamento

Os três comandos abaixo devem falhar. Troque o endereço do primeiro por um servidor da sua empresa:

```powershell
docker exec datapilot-agente curl --max-time 10 https://servidor.suaempresa.local
```

```powershell
docker exec datapilot-agente curl --max-time 10 https://example.com
```

```powershell
docker exec datapilot-agente ls /var/run/docker.sock
```

Para rodar a verificação automatizada dos scripts do container (leva cerca de um minuto; a última linha deve terminar com `Falhas: 0`):

```powershell
docker cp docker\testes\verificar.sh datapilot-agente:/tmp/verificar.sh
```

```powershell
docker exec datapilot-agente bash /tmp/verificar.sh
```

## Lista de destinos do proxy

A lista fica em [docker/proxy/squid.conf](../docker/proxy/squid.conf). Para ver o que foi recusado:

```powershell
docker compose -f docker\compose.yml exec proxy grep TCP_DENIED /var/log/squid/access.log
```

Para liberar um destino, acrescente o domínio numa linha `acl liberados dstdomain -n ...` e reinicie o proxy:

```powershell
docker compose -f docker\compose.yml restart proxy
```

### TFS com acesso somente leitura

1. No TFS, crie um PAT só com o escopo de leitura de código (**Code: Read**). O proxy libera o servidor inteiro: quem impede a escrita é o PAT.
2. Em `squid.conf`, tire o `#` da linha do exemplo do TFS e troque o domínio. Se o TFS usar outra porta (como 8080), acrescente-a na linha `acl portas_liberadas port`.
3. Reinicie o proxy.
4. Use o PAT dentro do container, nunca uma credencial do Windows.

## Parar e iniciar

Se o container do agente estiver parado, o painel o inicia sozinho. Ele **não** inicia o proxy: se você parou o ambiente inteiro, suba-o de novo com o comando do passo 3. O Docker Desktop precisa estar aberto.

Não use `docker compose down -v`: o `-v` apaga os volumes com o login e as conversas.

## Atualizar os agentes

Mude `CLAUDE_CODE_VERSION` ou `CODEX_VERSION` em [docker/Dockerfile](../docker/Dockerfile) e reconstrua. O mesmo comando leva ao container as mudanças nos scripts de `docker\painel`:

```powershell
docker compose -f docker\compose.yml up -d --build
```

Depois, confira o log do proxy: os domínios usados podem mudar entre versões.

## Remover a instalação antiga do modo Windows (opcional)

Só para quem usou uma versão anterior do DataPilot, com os agentes direto no Windows. O painel não usa mais o OpenSSH Server, o usuário `ia` nem `C:\Painel`; removê-los é opcional. Abra o PowerShell **como administrador**.

**Não apague `C:\ProgramData\Painel\sessoes`:** o container continua gravando o status nessa pasta.

### Hooks do perfil do ia

Se for manter o usuário, abra os dois arquivos e remova a chave `hooks` (ou só as entradas do painel). Se for remover o usuário e o perfil (abaixo), pule este passo.

```powershell
notepad C:\Users\IA\.claude\settings.json
```

```powershell
notepad C:\Users\IA\.codex\hooks.json
```

### OpenSSH Server

Desinstale só se não usar o servidor SSH para mais nada.

```powershell
Get-WindowsCapability -Online -Name OpenSSH.Server*
```

```powershell
Remove-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
```

### Usuário ia e perfil

Apaga de vez as conversas, os logins e os arquivos do perfil do `ia`. Copie antes o que quiser guardar. Se o usuário tiver outro nome, troque-o nos comandos.

```powershell
$sid = (Get-LocalUser -Name ia -ErrorAction Stop).SID.Value
```

```powershell
Get-CimInstance Win32_UserProfile -Filter "SID='$sid'" | Remove-CimInstance
```

```powershell
Remove-LocalUser -Name ia
```

### Pasta dos scripts

```powershell
Remove-Item -Recurse -Force C:\Painel
```
