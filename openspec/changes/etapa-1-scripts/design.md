# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver `specs/status-de-sessao/spec.md` e `specs/abertura-de-sessao/spec.md`.

O que molda a solução:

- Dois usuários no mesmo Windows. Os scripts rodam como `ia` (sem administrador); quem os instala e edita é o usuário principal.
- O PRD já traz um rascunho de `hook.ps1` e `abrir.ps1` e fixa os nomes dos campos do arquivo de status. Este design mantém os contratos e corrige o que a verificação das ferramentas mostrou.
- Verificado na documentação e nas CLIs do usuário principal (Claude Code 2.1.323, codex-cli 0.153.4):
  - `claude --session-id <uuid>`, `claude --resume <id>` e `codex resume <id>` existem.
  - Os dois agentes têm `SessionStart`, `UserPromptSubmit`, `PostToolUse`, `PermissionRequest`, `Stop` e `SessionEnd`. O Codex não tem `Notification`.
  - Nos dois, a saída padrão de `SessionStart` e `UserPromptSubmit` entra no contexto do modelo. No Codex, saída em texto puro é inválida em `Stop`.
  - O `Notification` do Claude tem tipos (`permission_prompt`, `idle_prompt`, `elicitation_dialog`, entre outros).
  - Hooks de comando aceitam `async` nos dois agentes. No Claude, `SessionEnd` tem orçamento de 1,5 s, ampliável com `timeout` por hook.
  - No Codex, hooks vêm ativos, mas só rodam depois de confiados em `/hooks`; a confiança é ligada ao hash do hook.
- Não verificado em execução, nem na instalação do `ia`. As tarefas começam por essa conferência.

## Goals / Non-Goals

**Goals:**

- Um único `hook.ps1` para os dois agentes, que nunca atrapalha o agente: sem saída, sempre código 0, rápido.
- Status que não fica preso em "atenção" nem parece parado durante turnos longos.
- `abrir.ps1` que recusa qualquer parâmetro fora do formato antes de tocar no sistema.
- Scripts verificáveis sem instalar nada: um roteiro automatizado para o hook e para as validações do `abrir.ps1`.

**Non-Goals:**

- Instalador automático. A instalação mexe em ACLs e no perfil de outro usuário; fica manual e visível, passo a passo.
- Limpeza de arquivos de status antigos.
- Executável .NET com AOT no lugar do PowerShell (melhoria posterior prevista no PRD).
- Qualquer coisa do lado do painel: leitura dos arquivos, regra de "inativa", montagem da linha do `wt`.

## Decisions

### 1. Mapa de eventos revisado

`PermissionRequest` → `atencao` e `PostToolUse` → `executando` entram no mapa; `SessionEnd` vale para os dois agentes.

- Por quê: com o mapa do PRD, uma permissão aprovada deixava a sessão em "atenção" até o `Stop`, e um turno longo ficava sem eventos e seria tratado como inativo. `PermissionRequest` existe nos dois agentes e cobre o Codex, que não tem `Notification`.
- Alternativa descartada: `PreToolUse` como batimento. Dispara antes da permissão e apagaria o "atenção" recém-gravado.

### 2. Notification filtrada no script e no matcher

O script só mapeia `Notification` quando o tipo é `permission_prompt` ou `elicitation_dialog`. O exemplo de configuração do Claude usa o mesmo filtro no `matcher`, para nem iniciar o PowerShell nos outros tipos.

- Por quê: a notificação de ociosidade transformaria toda sessão "aguardando" em "atenção". O script é a fonte da regra; o matcher é só economia.
- A confirmar na implementação: o nome do campo do tipo no evento (`notification_type`, segundo a documentação).

### 3. PostToolUse assíncrono, com proteção contra eventos fora de ordem

`PostToolUse` é configurado com `async: true`. O script registra o instante em que começou e, antes de substituir o arquivo, lê o `atualizado` existente: se for mais novo, não grava.

- Por quê: `PostToolUse` dispara a cada ferramenta; síncrono, somaria o tempo de partida do PowerShell a cada chamada. Assíncrono, um evento antigo pode terminar depois de um `Stop` e regredir o status; a comparação de instantes fecha essa janela a custo de uma leitura pequena.
- Alternativa descartada: síncrono em todos os eventos. Simples, mas penaliza justamente o evento mais frequente.
- Os demais eventos ficam síncronos: são raros e a ordem entre eles importa.

### 4. Escrita atômica com temporário único e UTF-8 sem BOM

O script grava num temporário com nome único por processo (`<session_id>.<pid>.tmp`) na mesma pasta e renomeia sobre o destino. O conteúdo é escrito em UTF-8 sem BOM.

- Por quê: o temporário fixo do rascunho (`<session_id>.tmp`) colide quando dois eventos da mesma sessão rodam juntos, o que passa a acontecer com `PostToolUse` assíncrono e com ferramentas em paralelo. O BOM que o Windows PowerShell 5.1 grava com `-Encoding UTF8` é um tropeço conhecido para leitores JSON; o painel lerá esses arquivos com `System.Text.Json`.
- Temporários órfãos são inofensivos: o contrato só inclui arquivos `.json`.
- Na implementação, a troca usa a substituição atômica do Windows (`File.Replace`) quando o destino já existe, com novas tentativas curtas. Um leitor que mantenha o arquivo aberto sem compartilhar exclusão faz a troca falhar; o hook desiste em silêncio e o status é corrigido no evento seguinte. Consequência para a Etapa 2: o painel deve abrir os arquivos de status compartilhando leitura, escrita e exclusão.

### 5. Windows PowerShell 5.1 e scripts só em ASCII

Os scripts têm como alvo o `powershell.exe` (5.1), presente em qualquer Windows, e usam apenas caracteres ASCII, inclusive nas mensagens.

- Por quê: o `pwsh` pode não existir para o `ia`. O 5.1 lê `.ps1` sem BOM como ANSI, e acentos virariam lixo dependendo de como o arquivo foi salvo ou copiado.

### 6. Validação explícita no corpo do abrir.ps1

Além dos atributos de parâmetro, o script valida no corpo e sai com código próprio: GUID pelo formato canônico (`TryParseExact` com formato `D`), `-Repo` pela expressão do contrato, recusa de nomes só com pontos, e conferência de que a pasta resolvida é um diretório cujo pai é exatamente a raiz.

- Por quê: `^[0-9a-fA-F-]{36}$` aceita 36 hifens e `[A-Za-z0-9_.-]+` aceita `..`; o Windows ainda descarta pontos finais de nomes de pasta. Conferir o pai da pasta resolvida cobre todos esses casos de uma vez.
- A raiz é uma constante única no topo do script (`C:\Fontes`). Migrar para `C:\Fontes-IA` é trocar essa linha.
- `-Sessao` obrigatório é decidido por combinação de agente e modo, o que atributos de parâmetro não expressam.

### 7. Parâmetro opcional de pasta só no hook

`hook.ps1` aceita `-PastaStatus`, com padrão `C:\ProgramData\Painel\sessoes`. `abrir.ps1` não aceita a raiz por parâmetro.

- Por quê: permite testar o hook numa pasta temporária. Não abre brecha: o hook roda como `ia`, que já controla a própria configuração de hooks. No `abrir.ps1` a raiz delimita o que quem chama pode alcançar, então fica fixa.

### 8. Verificação automatizada sem dependências

Um script `scripts/testes/verificar.ps1` alimenta o `hook.ps1` com eventos de exemplo numa pasta temporária e confere arquivo, saída e código de saída; e chama o `abrir.ps1` com parâmetros inválidos conferindo que nada inicia. Roda como qualquer usuário, sem Pester.

- Por quê: o mapa tem cerca de dez casos e vai mudar quando os agentes mudarem. O Pester que acompanha o Windows é a versão 3, de sintaxe incompatível com a atual; depender dele traria mais atrito que valor.
- Os casos que exigem um agente real (abrir, retomar, hooks disparando) ficam no roteiro manual.

### 9. Disposição no repositório

```
docs/PRD.md
docs/instalacao-etapa-1.md        guia de instalacao e roteiro manual
scripts/hook.ps1                  -> C:\Painel\
scripts/abrir.ps1                 -> C:\Painel\
scripts/exemplos/claude-settings.json
scripts/exemplos/codex-hooks.json
scripts/testes/verificar.ps1
```

O guia usa SIDs conhecidos nas ACLs (`*S-1-5-18`, `*S-1-5-32-544`) em vez de nomes de grupo, que mudam com o idioma do Windows. O exemplo do Claude define `timeout` no hook de `SessionEnd` para sair do orçamento de 1,5 s.

### 10. Forma do comando nos exemplos de configuração

O exemplo do Claude usa `command` com `args` (execução direta do `powershell.exe`). O exemplo do Codex usa uma linha de comando única com barras normais no caminho (`C:/Painel/hook.ps1`).

- Por quê: no Windows, o Claude Code executa a forma em texto único pelo Git Bash quando ele está instalado, e o Bash remove as barras invertidas de `C:\Painel\hook.ps1`. A linha do PRD falharia em silêncio. A documentação do Codex não diz qual shell ele usa no Windows; barras normais funcionam em qualquer um.
- No Codex, `SessionEnd` roda sempre de forma síncrona, com 1 s por padrão e no máximo 3 s; o exemplo define `timeout` 3. Um evento do hook leva cerca de 0,25 s na máquina de desenvolvimento.
- A documentação do Claude Code confirma o campo `notification_type` e que o matcher `permission_prompt|elicitation_dialog` é tratado como lista de valores exatos.

## Risks / Trade-offs

- **CLIs do `ia` em versão diferente da conferida** → a primeira tarefa confere versões e opções como `ia` antes de escrever os scripts.
- **`session_id` do Codex não ser um GUID** → `codex resume` documenta "UUID ou nome", mas o formato no hook não é documentado, e os exemplos da documentação de hooks usam `thr_123`. O `abrir.ps1` foi implementado conforme a spec atual (GUID); o hook aceita qualquer identificador seguro. O roteiro manual confere o formato logo na primeira sessão do Codex; se não for GUID, a spec de `abertura-de-sessao` e a validação de `-Sessao` são revistas antes de usar a retomada do Codex.
- **Eventos com mais de 2 MB** → o leitor de JSON do PowerShell 5.1 recusa entradas acima desse tamanho, o que pode ocorrer em `PostToolUse` com saída de ferramenta muito grande. O hook ignora o evento em silêncio; o status é corrigido no evento seguinte.
- **`/clear` gerar novo `session_id` no Claude** → o arquivo de status passaria a ter outro nome e a sessão antiga ficaria "encerrada". Não afeta esta etapa; o resultado do teste é registrado para a Etapa 4 (sessões não vinculadas).
- **`SessionEnd` não disparar ao fechar a aba** → o status fica no último valor. O painel compensa com a regra de inatividade, que hoje só cobre "ativa" e "executando"; uma sessão "aguardando" fechada assim ficaria amarela. Registrar para a Etapa 2.
- **Muitos processos PowerShell em turnos com muitas ferramentas** → assíncrono, não atrasa o agente; custa CPU. Se incomodar, o caminho é o executável AOT previsto no PRD.
- **Hooks mudarem entre versões dos agentes** → fixar versões no `ia` e repetir o roteiro manual depois de atualizar. No Codex, toda alteração no hook exige confiar de novo em `/hooks`.
- **Arquivos de status se acumulam** → sem limpeza nesta etapa. O painel não pode apagar (só lê); ver questão em aberto.
- **Aprovação da TI (RS-9) pendente** → fora do controle desta change; o PRD recomenda obtê-la antes de investir no painel.

## Migration Plan

Instalação manual, descrita no guia, nesta ordem: criar `C:\ProgramData\Painel\sessoes` e `C:\Painel` com as ACLs; copiar os dois scripts; mesclar os exemplos em `C:\Users\IA\.claude\settings.json` e `C:\Users\IA\.codex\hooks.json`; confiar nos hooks do Codex em `/hooks`; executar o roteiro manual.

Reversão: remover os blocos de hooks dos dois arquivos de configuração do `ia`. Os agentes voltam ao comportamento anterior imediatamente; as pastas podem ser apagadas depois.

## Open Questions

- Política de limpeza dos arquivos de status antigos (quem apaga e quando).
- Os hooks do Codex disparam também no aplicativo do Codex ou só na CLI? Só afeta quais sessões aparecem no painel.
- Migrar o `ia` para um clone próprio (`C:\Fontes-IA`): muda apenas a constante da raiz.
