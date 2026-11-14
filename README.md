# DataPilot
Painel de demandas integrado com IA (Claude Code e Codex)

## Painel (Etapa 2)

Requer o SDK do .NET 10.

```bash
dotnet build
dotnet test
dotnet run --project src/DataPilot.Wpf
```

O painel lê `C:\ProgramData\Painel\sessoes` por padrão e nunca escreve nela.

### Pasta de exemplo e roteiro de conferência

Para ver o painel sem a conta `ia`, gere uma pasta de exemplo (datas relativas ao momento da geração) e aponte o painel para ela:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo"
```

Logo após gerar, a janela deve mostrar:

- Cabeçalho: `1 precisa de atenção · 3 aguardando você · 3 executando`.
- Ordem: precisa de atenção (vermelho), aguardando você (amarelo), executando (azul), ativa (verde), inativa e encerrada (cinza); dentro de cada status, a mais recente primeiro.
- `ex-cwd-longo` com a pasta cortada em 260 caracteres (indicação `…`) e `ex-cwd-controle` sem quebras de linha.
- Não aparecem: os arquivos inválidos (`ex-json-ruim`, `ex-status-ruim`, `ex-id-diferente`, `ex-sem-fuso`, `ex-grande`, `.tmp`).
- Ocultas até marcar "Mostrar antigas": `ex-antiga-encerrada` (7 dias) e `ex-antiga-inativa` (4 dias).
- Passar o mouse sobre uma linha mostra a pasta completa e o identificador.

## Abrir e retomar sessões (Etapa 3)

O cabeçalho do painel tem dois botões:

- **Repositórios**: cadastra o nome da pasta (dentro de `C:\Fontes`, só `[A-Za-z0-9_.-]`, nunca só pontos) e remove repositórios. Remover um repositório com sessões registradas pede confirmação.
- **Nova sessão**: escolhe repositório, agente (Claude ou Codex) e um rótulo opcional (até 40 caracteres, sem `;`) e abre uma aba do Windows Terminal que chama o `abrir.ps1` como o usuário `ia`, pelo SSH local. Todas as abas ficam na janela `painel`.

As linhas da lista cujo identificador foi registrado pelo painel ganham o botão **Retomar** e a indicação do repositório e do rótulo. Se a sessão não estiver encerrada nem inativa, o painel pede confirmação antes de abrir a aba. Sessões do Codex e a sessão nova que o Claude cria depois de um `/clear` não têm o botão (o vínculo delas fica para a Etapa 4).

O comando é montado só com dados do arquivo de dados ou da escolha na tela, nunca com valores lidos de arquivos de status, e iniciado como lista de argumentos, sem shell. Requer o Windows Terminal (`wt.exe`) e o SSH local da Etapa 1.

### Arquivo de dados

O painel guarda repositórios e sessões abertas por ele em `%APPDATA%\Painel\demandas.json`. O arquivo só é criado na primeira alteração. Se existir e não puder ser lido, o painel mostra um aviso com o caminho, não grava nada e desabilita cadastrar, abrir e retomar (a lista continua funcionando). Para usar outro arquivo, sem tocar nos dados reais:

```bash
dotnet run --project src/DataPilot.Wpf -- --dados "$TEMP/meus-dados.json"
```

### Roteiro de verificação

O gerador de exemplo também cria o arquivo de dados de exemplo (`$TEMP/datapilot-exemplo-demandas.json`):

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-exemplo-demandas.json"
```

Com esses dois arquivos:

- Só duas linhas mostram **Retomar**: a do `C:\Windows` (sessão encerrada, indica `Repositório: Faturamento · implementacao`, porque vale o repositório registrado e não a pasta do status) e a `ativa` de `Estoque` (pede confirmação).
- A outra sessão `ativa` de `Estoque`, que não está no arquivo de dados, não mostra o botão (equivale à sessão criada por `/clear`).
- Para ver o aviso de dados indisponíveis, aponte `--dados` para um arquivo com texto que não seja JSON: os botões **Nova sessão** e **Repositórios** ficam desabilitados e o arquivo não é alterado.

Verificação manual com a conta `ia` (grupo 6 de `openspec/changes/etapa-3-abrir-e-retomar/tasks.md`):

1. Cadastre um repositório que exista em `C:\Fontes` e abra uma sessão nova do Claude; a aba deve ter o título `Claude · <repositório>` e a linha deve passar a mostrar **Retomar**.
2. Encerre a sessão e acione **Retomar**: abre sem confirmação. Com a sessão aberta, pede confirmação.
3. Abra uma sessão do Codex: a aba abre, a sessão aparece sem **Retomar** e o `demandas.json` não muda.
4. Em uma sessão do Claude aberta pelo painel, execute `/clear`: a sessão nova aparece sem **Retomar** e a original mantém o botão.
5. Confirme que o `ia` não lê o arquivo de dados (deve ser negado), executando como usuário principal: `ssh ia@localhost powershell -NoProfile -Command Get-Content $env:APPDATA\Painel\demandas.json`.
