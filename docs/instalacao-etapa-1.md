# Instalação da Etapa 1: scripts de status e de abertura

Este guia instala o `hook.ps1` e o `abrir.ps1` para o usuário isolado `ia` e descreve como verificar que funcionam. Nada aqui é automático: cada passo é executado por você.

Resumo do que será criado:

| Local | Conteúdo | Quem escreve |
|---|---|---|
| `C:\ProgramData\Painel\sessoes` | Um arquivo de status por sessão | Só o `ia` |
| `C:\Painel` | `hook.ps1`, `abrir.ps1`, exemplos e testes | Só um administrador |
| `C:\Users\IA\.claude\settings.json` | Hooks do Claude Code | `ia` |
| `C:\Users\IA\.codex\hooks.json` | Hooks do Codex | `ia` |

## Versões conferidas

Conferidas numa sessão `ssh ia@localhost`.

| Ferramenta | Versão | Opções confirmadas |
|---|---|---|
| Claude Code | 2.1.325 | `--session-id <uuid>`; `-r, --resume [value]` (por ID ou nome) |
| Codex | codex-cli 0.169.0 | `codex resume [OPTIONS] [SESSION_ID] [PROMPT]` (UUID ou nome) |

Depois de atualizar qualquer um dos agentes, repita o roteiro de verificação manual deste guia.

## Passo 1: pastas e permissões

Abra o PowerShell **como administrador**, a partir da sua conta principal, na raiz deste repositório.

Guarde o nome da conta principal. Se você elevou com outra conta de administrador, troque `whoami` pelo nome da conta principal.

```powershell
$principal = whoami
```

Pasta de status: o `ia` modifica, a conta principal só lê.

```powershell
New-Item -ItemType Directory -Force C:\ProgramData\Painel\sessoes | Out-Null
```

```powershell
icacls C:\ProgramData\Painel /inheritance:r /grant "*S-1-5-18:(OI)(CI)F" /grant "*S-1-5-32-544:(OI)(CI)F" /grant "ia:(OI)(CI)M" /grant "${principal}:(OI)(CI)RX"
```

Pasta dos scripts: o `ia` e a conta principal só leem e executam.

```powershell
New-Item -ItemType Directory -Force C:\Painel\testes, C:\Painel\exemplos | Out-Null
```

```powershell
icacls C:\Painel /inheritance:r /grant "*S-1-5-18:(OI)(CI)F" /grant "*S-1-5-32-544:(OI)(CI)F" /grant "ia:(OI)(CI)RX" /grant "${principal}:(OI)(CI)RX"
```

`*S-1-5-18` é a conta SYSTEM e `*S-1-5-32-544` é o grupo de administradores; os SIDs funcionam em qualquer idioma do Windows.

Confira o resultado. Na saída, o `ia` deve aparecer com `(M)` na primeira pasta e com `(RX)` na segunda.

```powershell
icacls C:\ProgramData\Painel\sessoes
```

```powershell
icacls C:\Painel
```

## Passo 2: copiar os scripts

No mesmo PowerShell de administrador, na raiz deste repositório:

```powershell
Copy-Item scripts\hook.ps1, scripts\abrir.ps1 C:\Painel\ -Force
```

```powershell
Copy-Item scripts\testes\verificar.ps1 C:\Painel\testes\ -Force
```

```powershell
Copy-Item scripts\exemplos\*.json C:\Painel\exemplos\ -Force
```

Sempre que um script mudar no repositório, repita a cópia. A raiz dos fontes do `ia` é a constante `$RaizFontes` no topo do `abrir.ps1` (`C:\Fontes`).

## Passo 3: verificação automatizada como ia

Num PowerShell comum (sem elevação):

```powershell
ssh ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\testes\verificar.ps1
```

A última linha deve terminar com `Falhas: 0`. Os casos usam só pastas temporárias; nenhum agente é iniciado.

## Passo 4: hooks do Claude Code

Numa sessão `ssh ia@localhost`, entre no PowerShell (`powershell`) e veja se o `ia` já tem configuração:

```powershell
Test-Path $HOME\.claude\settings.json
```

- Se responder `False`, copie o exemplo:

  ```powershell
  Copy-Item C:\Painel\exemplos\claude-settings.json $HOME\.claude\settings.json
  ```

- Se responder `True`, abra o arquivo e acrescente a chave `hooks` do exemplo. Se já existir uma chave `hooks`, acrescente cada evento do exemplo à lista do evento correspondente, sem apagar o que já está lá.

O exemplo usa `command` com `args`. Nessa forma o Claude Code inicia o `powershell.exe` diretamente, sem passar pelo Git Bash, que removeria as barras invertidas do caminho.

## Passo 5: hooks do Codex

Na mesma sessão do `ia`:

```powershell
Test-Path $HOME\.codex\hooks.json
```

- Se responder `False`, copie o exemplo:

  ```powershell
  Copy-Item C:\Painel\exemplos\codex-hooks.json $HOME\.codex\hooks.json
  ```

- Se responder `True`, acrescente os eventos do exemplo dentro da chave `hooks` existente.

Depois abra o Codex, digite `/hooks`, revise os hooks listados e marque-os como confiáveis. Hooks não confiados são ignorados em silêncio. A confiança é ligada ao conteúdo do hook: qualquer alteração no `hooks.json` exige confiar de novo.

O Codex dá no máximo 3 segundos ao hook de `SessionEnd`; o exemplo já usa esse limite.

## Roteiro de verificação manual

Use dois terminais: um para o agente e outro, na conta principal, para observar o status.

Comando de observação (conta principal):

```powershell
Get-ChildItem C:\ProgramData\Painel\sessoes\*.json | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json } | Sort-Object atualizado | Format-Table agente, status, evento, atualizado, session_id
```

Antes de começar, escolha um repositório que exista em `C:\Fontes` e gere um identificador:

```powershell
$repo = 'Faturamento'
```

```powershell
$guid = [guid]::NewGuid().ToString()
```

### 1. Permissões

| O que fazer | O que observar |
|---|---|
| Conta principal, sem elevação: `New-Item C:\ProgramData\Painel\sessoes\teste.txt` | Acesso negado. Se o arquivo for criado, a conta principal está com direitos de administrador ativos nesse terminal; apague o arquivo e repita num terminal sem elevação. |
| `ssh ia@localhost powershell -NoProfile -Command New-Item C:\Painel\teste.txt` | Acesso negado. |

### 2. Sessão do Claude: abrir, status e retomar

Abra a sessão pelo SSH, como o painel fará:

```powershell
ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente claude -Repo $repo -Sessao $guid -Modo novo
```

| O que fazer | O que observar |
|---|---|
| Abrir a sessão | O Claude abre no repositório. Surge o arquivo `<guid>.json`, com o GUID que você gerou, `agente` `claude` e `status` `ativa`. |
| Enviar um prompt simples | `status` passa a `executando` e, ao terminar, a `aguardando`. |
| Pedir algo que exija permissão (por exemplo, criar um arquivo) | `status` passa a `atencao` enquanto o pedido está na tela. |
| Aprovar a permissão | `status` volta a `executando` e depois a `aguardando`. |
| Esperar dois minutos sem digitar | `status` continua `aguardando`. |
| Sair com `/exit` | `status` passa a `encerrada`. |
| Durante todo o uso | Nenhuma mensagem de hook, erro ou texto estranho aparece na conversa. |

Retome a mesma conversa:

```powershell
ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente claude -Repo $repo -Sessao $guid -Modo retomar
```

| O que fazer | O que observar |
|---|---|
| Retomar | A conversa anterior aparece com o histórico. |
| Olhar a pasta de status | Anote se continua existindo um único arquivo para a sessão (questão em aberto 3). |

### 3. Sessão do Codex: abrir, status e retomar

```powershell
ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente codex -Repo $repo -Modo novo
```

| O que fazer | O que observar |
|---|---|
| Abrir a sessão | Surge um arquivo novo com `agente` `codex` e `status` `ativa`. |
| Olhar o `session_id` desse arquivo | Anote o formato. O `abrir.ps1` só retoma identificadores na forma de GUID (36 caracteres, 8-4-4-4-12). **Se não for um GUID, pare aqui**: a spec `abertura-de-sessao` precisa ser revista. |
| Enviar um prompt, pedir algo que exija aprovação, aprovar | Mesma sequência do Claude: `executando`, `atencao`, `executando`, `aguardando`. |
| Sair do Codex | `status` passa a `encerrada`. Se não passar, anote: o hook de `SessionEnd` do Codex tem limite curto. |

Retome com o identificador anotado:

```powershell
ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente codex -Repo $repo -Sessao <session_id-do-codex> -Modo retomar
```

O Codex deve abrir a conversa anterior, com o histórico.

### 4. Recusa de parâmetros pelo SSH

```powershell
ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente claude -Repo .. -Sessao $guid -Modo novo
```

Deve aparecer a mensagem de `-Repo` inválido, sem abrir nenhum agente.

### 5. Questões em aberto

| Questão | Como testar |
|---|---|
| 1. `/clear` no Claude muda o `session_id`? | Numa sessão aberta, digite `/clear` e envie um prompt. Veja se surge um arquivo de status novo e o que acontece com o anterior. |
| 2. Fechar a aba dispara `SessionEnd`? | Com a sessão em `aguardando`, feche a aba do terminal. Depois de alguns segundos, veja se o `status` passou a `encerrada`. Teste com os dois agentes. |
| 3. Uma sessão retomada mantém o mesmo arquivo? | Resultado anotado nos itens 2 e 3 acima. |

### 6. Captura de um evento (tarefa 1.3)

A documentação do Claude Code diz que o tipo da notificação vem no campo `notification_type`. Para confirmar na prática, acrescente temporariamente ao evento `Notification` do `settings.json` do `ia` um segundo hook, que grava o evento bruto:

```json
{
  "type": "command",
  "command": "powershell.exe",
  "args": ["-NoProfile", "-Command", "[Console]::In.ReadToEnd() | Add-Content -Path C:/ProgramData/Painel/captura.jsonl"]
}
```

Provoque um pedido de permissão, leia `C:\ProgramData\Painel\captura.jsonl` e confira o nome do campo. Depois remova o hook temporário e apague o arquivo de captura como `ia`: ele contém o conteúdo bruto dos eventos.

## Resultados

Preencha depois de executar o roteiro.

| Item | Resultado |
|---|---|
| Formato do `session_id` do Codex (tarefa 1.2) | |
| Nome do campo de tipo em `Notification` (tarefa 1.3) | |
| `/clear` muda o `session_id` no Claude? | |
| Fechar a aba dispara `SessionEnd`? (Claude / Codex) | |
| Sessão retomada mantém o mesmo arquivo de status? (Claude / Codex) | |

## Reversão

Remova a chave `hooks` (ou só as entradas do painel) de `C:\Users\IA\.claude\settings.json` e de `C:\Users\IA\.codex\hooks.json`. Os agentes voltam ao comportamento anterior na sessão seguinte.

Para remover também as pastas, num PowerShell de administrador:

```powershell
Remove-Item -Recurse -Force C:\Painel, C:\ProgramData\Painel
```
