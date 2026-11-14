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
