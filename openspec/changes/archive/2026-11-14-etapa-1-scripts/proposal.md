# Proposal

## Why

O Painel de Demandas depende de duas peças que rodam como o usuário isolado `ia`: um hook que grava o status de cada sessão do Claude Code e do Codex, e um script que abre ou retoma uma sessão no repositório certo. Sem elas não há dado para o painel exibir nem caminho seguro para lançar agentes. Elas são baratas, úteis sozinhas durante o período de teste de duas semanas e respondem às questões em aberto do PRD antes de qualquer código WPF.

## What Changes

- Novo `scripts/hook.ps1`: recebe o evento de hook do agente pela entrada padrão e grava um arquivo de status por sessão em `C:\ProgramData\Painel\sessoes\<session_id>.json`, sem saída padrão e sempre com código 0.
- Novo `scripts/abrir.ps1`: recebe agente, repositório, sessão e modo já validados, entra na pasta do repositório e inicia ou retoma o agente.
- Mapa evento → status revisado em relação ao PRD, sem renomear campos do contrato:
  - `PermissionRequest` passa a gerar "atenção" nos dois agentes (o Codex não tem `Notification`).
  - `Notification` do Claude só gera "atenção" para os tipos de permissão e de pergunta; o aviso de ociosidade é ignorado.
  - `PostToolUse` passa a gerar "executando", servindo de batimento durante turnos longos e devolvendo a sessão a "executando" depois de uma permissão aprovada.
  - `SessionEnd` passa a valer também para o Codex.
- Exemplos de configuração de hooks para o Claude Code (`settings.json`) e para o Codex (`hooks.json`), prontos para copiar para o perfil do `ia`.
- Guia de instalação: criação das pastas, permissões (ACL), cópia dos scripts e ativação dos hooks. A instalação é manual, feita pelo usuário principal como administrador; nada nesta change é instalado automaticamente.
- Roteiro de verificação manual cobrindo o critério de aceite da Etapa 1 e três questões que só podem ser respondidas rodando como `ia`: se `/clear` muda o `session_id` no Claude, o formato do `session_id` do Codex e se fechar a aba dispara `SessionEnd`.
- `docs/PRD.md`: o PRD exportado em Markdown, como referência versionada.

Fora desta change: solution .NET, `Painel.Core`, `Painel.Wpf`, `demandas.json`, a regra de "inativa" (calculada pelo painel) e qualquer leitura dos arquivos de status.

## Capabilities

### New Capabilities

- `status-de-sessao`: registro do status de cada sessão de agente em arquivos lidos pelo painel — contrato do arquivo, mapa evento → status, e regras de silêncio, código de saída e escrita atômica do hook.
- `abertura-de-sessao`: início e retomada de uma sessão de agente no repositório certo como usuário `ia`, a partir de parâmetros validados.

### Modified Capabilities

Nenhuma.

## Impact

- **Repositório:** novos arquivos em `scripts/` e `docs/`. Nenhum código .NET.
- **Máquina (instalação manual, fora do repositório):** `C:\ProgramData\Painel\sessoes` (o `ia` escreve, o usuário principal só lê), `C:\Painel\*.ps1` (o `ia` lê, só o usuário principal edita), `C:\Users\IA\.claude\settings.json` e `C:\Users\IA\.codex\hooks.json`.
- **Agentes:** cada evento de hook passa a iniciar um PowerShell. `PostToolUse` roda a cada ferramenta; por isso é configurado como assíncrono. No Codex, os hooks precisam ser revisados e confiados em `/hooks`, e a confiança é perdida quando a configuração do hook muda.
- **Segurança:** implementa o lado `ia` de RS-3, RS-4 e RS-6 do PRD. Não altera a configuração do SSH (RS-7).
- **Etapas seguintes:** o arquivo de status e a linha de comando do `abrir.ps1` viram contratos consumidos pelo painel nas Etapas 2 e 3.
