# Verificacao automatizada dos scripts da Etapa 1 (hook.ps1 e abrir.ps1).
# Sem dependencias: roda em powershell.exe 5.1, como qualquer usuario, sem instalar nada.
#
# Uso:
#   powershell -NoProfile -ExecutionPolicy Bypass -File verificar.ps1 [-PastaScripts <pasta>]
#
# -PastaScripts e a pasta que contem hook.ps1 e abrir.ps1. O padrao e a pasta acima desta.
# Termina com codigo 0 se todos os casos passarem e 1 se algum falhar.
param(
    [string]$PastaScripts = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$script:Total = 0
$script:Falhas = 0
$script:Pastas = @()
$Utf8SemBom = New-Object System.Text.UTF8Encoding($false)
$Hook = Join-Path $PastaScripts 'hook.ps1'
$Abrir = Join-Path $PastaScripts 'abrir.ps1'
$IdPadrao = '0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10'

# ---------------------------------------------------------------------------
# Estrutura de casos
# ---------------------------------------------------------------------------

function Citar([string]$Valor) {
    # Monta um argumento da linha de comando do Windows.
    if ($Valor -eq '') { return '""' }
    if ($Valor -match '\A[A-Za-z0-9_.:\\/=-]+\z') { return $Valor }
    $v = $Valor -replace '(\\*)"', '$1$1\"'
    $v = $v -replace '(\\+)\z', '$1$1'
    return '"' + $v + '"'
}

function Iniciar([string]$Script, [string[]]$Argumentos, [string]$Entrada) {
    # Inicia o script num powershell.exe separado, com a entrada padrao em UTF-8.
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'powershell.exe'
    $partes = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Citar $Script))
    foreach ($a in $Argumentos) { $partes += (Citar $a) }
    $psi.Arguments = $partes -join ' '
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $saida = $p.StandardOutput.ReadToEndAsync()
    $erro = $p.StandardError.ReadToEndAsync()
    if ($Entrada) {
        $bytes = $Utf8SemBom.GetBytes($Entrada)
        $p.StandardInput.BaseStream.Write($bytes, 0, $bytes.Length)
        $p.StandardInput.BaseStream.Flush()
    }
    $p.StandardInput.Close()
    return [pscustomobject]@{ Processo = $p; Saida = $saida; Erro = $erro }
}

function Concluir($Execucao) {
    # Espera o processo e devolve codigo de saida, saida padrao e saida de erro.
    $p = $Execucao.Processo
    if (-not $p.WaitForExit(60000)) {
        try { $p.Kill() } catch { }
        return [pscustomobject]@{ Codigo = -1; Saida = ''; Erro = 'tempo esgotado' }
    }
    $p.WaitForExit()
    return [pscustomobject]@{ Codigo = $p.ExitCode; Saida = $Execucao.Saida.Result; Erro = $Execucao.Erro.Result }
}

function Executar([string]$Script, [string[]]$Argumentos, [string]$Entrada) {
    return (Concluir (Iniciar $Script $Argumentos $Entrada))
}

function Conferir([string]$Nome, [bool]$Condicao, [string]$Detalhe = '') {
    $script:Total++
    if ($Condicao) {
        Write-Host ('  ok     ' + $Nome)
    } else {
        $script:Falhas++
        Write-Host ('  FALHA  ' + $Nome + ' ' + $Detalhe)
    }
}

function Grupo([string]$Nome) {
    Write-Host ''
    Write-Host $Nome
}

function Nova-Pasta {
    # Pasta temporaria propria deste script, removida no final.
    $p = Join-Path ([System.IO.Path]::GetTempPath()) ('painel-verificar-' + [guid]::NewGuid().ToString('N'))
    [void](New-Item -ItemType Directory -Path $p)
    $script:Pastas += $p
    return $p
}

function Conferir-Ascii([string]$Arquivo) {
    $nome = Split-Path -Leaf $Arquivo
    $bytes = [System.IO.File]::ReadAllBytes($Arquivo)
    $fora = @($bytes | Where-Object { $_ -gt 127 }).Count
    Conferir ($nome + ': somente ASCII') ($fora -eq 0) ('(' + $fora + ' bytes acima de 127)')
}

# ---------------------------------------------------------------------------
# Casos
# ---------------------------------------------------------------------------

try {
    # ----- hook.ps1 -----------------------------------------------------------

    function Evento([string]$Nome, [string]$Id = $IdPadrao, [hashtable]$Extra) {
        # Evento de hook de exemplo, com campos que NAO podem aparecer no arquivo de status.
        $h = [ordered]@{
            session_id      = $Id
            transcript_path = 'C:\Users\ia\.claude\projects\x\conversa.jsonl'
            cwd             = 'C:\Fontes\Faturamento'
            hook_event_name = $Nome
        }
        if ($Extra) { foreach ($k in $Extra.Keys) { $h[$k] = $Extra[$k] } }
        return ($h | ConvertTo-Json -Compress)
    }

    function Rodar-Hook([string]$Pasta, [string]$Entrada, [string]$Agente = 'claude') {
        return (Executar $Hook @('-Agente', $Agente, '-PastaStatus', $Pasta) $Entrada)
    }

    function Ler-Texto([string]$Pasta, [string]$Id = $IdPadrao) {
        $arq = Join-Path $Pasta ($Id + '.json')
        if (-not (Test-Path -LiteralPath $arq)) { return $null }
        return [System.IO.File]::ReadAllText($arq, $Utf8SemBom)
    }

    function Ler-Status([string]$Pasta, [string]$Id = $IdPadrao) {
        $t = Ler-Texto $Pasta $Id
        if ($null -eq $t) { return $null }
        try { return ($t | ConvertFrom-Json) } catch { return $null }
    }

    function Contar([string]$Pasta, [string]$Filtro = '*') {
        return @(Get-ChildItem -LiteralPath $Pasta -Filter $Filtro -Force).Count
    }

    function Conferir-Silencio([string]$Nome, $R) {
        Conferir ($Nome + ': codigo 0') ($R.Codigo -eq 0) ('(codigo ' + $R.Codigo + ')')
        Conferir ($Nome + ': sem saida padrao') ($R.Saida -eq '') ('(' + $R.Saida + ')')
        Conferir ($Nome + ': sem saida de erro') ($R.Erro -eq '') ('(' + $R.Erro + ')')
    }

    function Gravar-Status([string]$Pasta, [string]$Status, [string]$Evento, [DateTimeOffset]$Quando) {
        # Grava um arquivo de status pronto, como se um evento anterior o tivesse escrito.
        $c = '{"agente":"claude","session_id":"' + $IdPadrao + '","cwd":"C:\\Fontes\\Faturamento","status":"' +
            $Status + '","evento":"' + $Evento + '","atualizado":"' + $Quando.ToString('o') + '"}'
        [System.IO.File]::WriteAllText((Join-Path $Pasta ($IdPadrao + '.json')), $c, $Utf8SemBom)
        return $c
    }

    Grupo 'hook: mapa de evento para status'
    $mapa = @(
        @('SessionStart', 'ativa'), @('UserPromptSubmit', 'executando'), @('PostToolUse', 'executando'),
        @('PermissionRequest', 'atencao'), @('Stop', 'aguardando'), @('SessionEnd', 'encerrada')
    )
    foreach ($par in $mapa) {
        $pasta = Nova-Pasta
        $r = Rodar-Hook $pasta (Evento $par[0])
        $s = Ler-Status $pasta
        Conferir ($par[0] + ' grava ' + $par[1]) ($s -and $s.status -ceq $par[1])
        Conferir ($par[0] + ': campo evento') ($s -and $s.evento -ceq $par[0])
        Conferir-Silencio $par[0] $r
    }

    Grupo 'hook: contrato do arquivo'
    $pasta = Nova-Pasta
    [void](Rodar-Hook $pasta (Evento 'SessionStart'))
    $s = Ler-Status $pasta
    $t = [string](Ler-Texto $pasta)
    $campos = @()
    if ($s) { $campos = @($s.PSObject.Properties | ForEach-Object { $_.Name }) }
    Conferir 'exatamente os seis campos, na ordem do contrato' (($campos -join ',') -ceq 'agente,session_id,cwd,status,evento,atualizado') ('(' + ($campos -join ',') + ')')
    Conferir 'agente claude' ($s -and $s.agente -ceq 'claude')
    Conferir 'session_id do evento' ($s -and $s.session_id -ceq $IdPadrao)
    Conferir 'cwd do evento' ($s -and $s.cwd -ceq 'C:\Fontes\Faturamento')
    Conferir 'atualizado em ISO 8601 com fuso' ($t -match '"atualizado":"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?[+-]\d{2}:\d{2}"')
    Conferir 'um unico arquivo na pasta' ((Contar $pasta) -eq 1)
    $pasta = Nova-Pasta
    [void](Rodar-Hook $pasta (Evento 'SessionStart') 'codex')
    $s = Ler-Status $pasta
    Conferir 'agente codex' ($s -and $s.agente -ceq 'codex')
    $pasta = Nova-Pasta
    $r = Rodar-Hook $pasta (Evento 'SessionStart') 'outro'
    Conferir 'agente desconhecido nao grava' ((Contar $pasta) -eq 0)
    Conferir-Silencio 'agente desconhecido' $r
    $pasta = Nova-Pasta
    $cwdAcentuado = 'C:\Fontes\Produ' + [char]0xE7 + [char]0xE3 + 'o'
    [void](Rodar-Hook $pasta (Evento 'Stop' $IdPadrao @{ cwd = $cwdAcentuado }))
    $s = Ler-Status $pasta
    Conferir 'cwd com acentos preservado (UTF-8)' ($s -and $s.cwd -ceq $cwdAcentuado)

    Grupo 'hook: sequencia de uma sessao'
    $pasta = Nova-Pasta
    [void](Rodar-Hook $pasta (Evento 'SessionStart'))
    [void](Rodar-Hook $pasta (Evento 'UserPromptSubmit'))
    [void](Rodar-Hook $pasta (Evento 'PermissionRequest'))
    Conferir 'pedido de permissao deixa em atencao' ((Ler-Status $pasta).status -ceq 'atencao')
    [void](Rodar-Hook $pasta (Evento 'PostToolUse'))
    $s1 = Ler-Status $pasta
    Conferir 'PostToolUse depois da permissao volta a executando' ($s1.status -ceq 'executando')
    [void](Rodar-Hook $pasta (Evento 'PostToolUse'))
    $s2 = Ler-Status $pasta
    Conferir 'PostToolUse seguinte avanca atualizado' ($s2.status -ceq 'executando' -and ([string]$s2.atualizado -cne [string]$s1.atualizado))
    [void](Rodar-Hook $pasta (Evento 'Stop'))
    Conferir 'Stop deixa em aguardando' ((Ler-Status $pasta).status -ceq 'aguardando')
    Conferir 'a sessao continua com um unico arquivo' ((Contar $pasta) -eq 1)
    $antes = Ler-Texto $pasta
    $r = Rodar-Hook $pasta (Evento 'PreCompact')
    Conferir 'evento desconhecido nao altera o arquivo' ((Ler-Texto $pasta) -ceq $antes)
    Conferir-Silencio 'evento desconhecido' $r
    $pasta = Nova-Pasta
    [void](Rodar-Hook $pasta (Evento 'PreCompact'))
    Conferir 'evento desconhecido nao cria arquivo' ((Contar $pasta) -eq 0)

    Grupo 'hook: data de criacao do arquivo preservada'
    $pasta = Nova-Pasta
    [void](Rodar-Hook $pasta (Evento 'SessionStart'))
    $arq = Join-Path $pasta ($IdPadrao + '.json')
    $fixada = [DateTime]::UtcNow.AddDays(-3)
    [System.IO.File]::SetCreationTimeUtc($arq, $fixada)
    $esperada = [System.IO.File]::GetCreationTimeUtc($arq)
    Conferir 'data de criacao recuada para o teste' ($esperada -lt [DateTime]::UtcNow.AddDays(-2))
    Start-Sleep -Milliseconds 50
    [void](Rodar-Hook $pasta (Evento 'UserPromptSubmit'))
    Conferir 'evento posterior atualiza o status' ((Ler-Status $pasta).status -ceq 'executando')
    Conferir 'evento posterior preserva a data de criacao' ([System.IO.File]::GetCreationTimeUtc($arq) -eq $esperada) ('(' + [System.IO.File]::GetCreationTimeUtc($arq).ToString('o') + ')')
    [void](Rodar-Hook $pasta (Evento 'Stop'))
    Conferir 'varios eventos preservam a data de criacao' ([System.IO.File]::GetCreationTimeUtc($arq) -eq $esperada)
    $pasta = Nova-Pasta
    $antes = [DateTime]::UtcNow.AddSeconds(-2)
    [void](Rodar-Hook $pasta (Evento 'SessionStart'))
    $criado = [System.IO.File]::GetCreationTimeUtc((Join-Path $pasta ($IdPadrao + '.json')))
    Conferir 'primeiro evento cria o arquivo no momento da gravacao' ($criado -ge $antes -and $criado -le [DateTime]::UtcNow.AddSeconds(2)) ('(' + $criado.ToString('o') + ')')

    Grupo 'hook: notificacoes filtradas por tipo'
    foreach ($tipo in @('permission_prompt', 'elicitation_dialog')) {
        $pasta = Nova-Pasta
        $r = Rodar-Hook $pasta (Evento 'Notification' $IdPadrao @{ notification_type = $tipo; message = 'texto' })
        Conferir ('Notification ' + $tipo + ' grava atencao') ((Ler-Status $pasta).status -ceq 'atencao')
        Conferir-Silencio ('Notification ' + $tipo) $r
    }
    foreach ($tipo in @('idle_prompt', 'auth_success', '')) {
        $pasta = Nova-Pasta
        [void](Rodar-Hook $pasta (Evento 'Stop'))
        $antes = Ler-Texto $pasta
        $extra = @{ message = 'texto' }
        if ($tipo) { $extra['notification_type'] = $tipo }
        $r = Rodar-Hook $pasta (Evento 'Notification' $IdPadrao $extra)
        $rotulo = $tipo
        if (-not $rotulo) { $rotulo = 'sem tipo' }
        Conferir ('Notification ' + $rotulo + ' depois de Stop continua aguardando') ((Ler-Texto $pasta) -ceq $antes)
        Conferir-Silencio ('Notification ' + $rotulo) $r
    }

    Grupo 'hook: escrita atomica e ordem dos eventos'
    $pasta = Nova-Pasta
    [void](Rodar-Hook $pasta (Evento 'SessionStart'))
    $bytes = [System.IO.File]::ReadAllBytes((Join-Path $pasta ($IdPadrao + '.json')))
    Conferir 'arquivo sem BOM' ($bytes.Length -gt 0 -and $bytes[0] -eq 0x7B) ('(primeiro byte ' + $bytes[0] + ')')
    $pasta = Nova-Pasta
    $execs = @()
    for ($i = 0; $i -lt 8; $i++) {
        $nome = 'PostToolUse'
        if ($i % 2) { $nome = 'PermissionRequest' }
        $execs += (Iniciar $Hook @('-Agente', 'claude', '-PastaStatus', $pasta) (Evento $nome))
    }
    $quietos = $true
    foreach ($e in $execs) {
        $r = Concluir $e
        if ($r.Codigo -ne 0 -or $r.Saida -ne '' -or $r.Erro -ne '') { $quietos = $false }
    }
    $s = Ler-Status $pasta
    Conferir 'oito eventos em paralelo: JSON completo de um deles' ($s -and (@('executando', 'atencao') -ccontains $s.status))
    Conferir 'oito eventos em paralelo: todos com codigo 0 e sem saida' $quietos
    Conferir 'oito eventos em paralelo: um unico arquivo, sem temporarios' ((Contar $pasta) -eq 1 -and (Contar $pasta '*.tmp') -eq 0)
    $pasta = Nova-Pasta
    $conteudo = Gravar-Status $pasta 'aguardando' 'Stop' ([DateTimeOffset]::Now.AddHours(1))
    $r = Rodar-Hook $pasta (Evento 'PostToolUse')
    Conferir 'evento mais antigo nao substitui status mais novo' ((Ler-Texto $pasta) -ceq $conteudo)
    Conferir-Silencio 'evento mais antigo' $r
    $pasta = Nova-Pasta
    [void](Gravar-Status $pasta 'aguardando' 'Stop' ([DateTimeOffset]::Now.AddHours(-1)))
    [void](Rodar-Hook $pasta (Evento 'UserPromptSubmit'))
    Conferir 'evento mais novo substitui status antigo' ((Ler-Status $pasta).status -ceq 'executando')
    $pasta = Nova-Pasta
    [System.IO.File]::WriteAllText((Join-Path $pasta ($IdPadrao + '.json')), '{{{ lixo', $Utf8SemBom)
    [void](Rodar-Hook $pasta (Evento 'Stop'))
    Conferir 'arquivo anterior corrompido e substituido' ((Ler-Status $pasta).status -ceq 'aguardando')

    # Um leitor com o arquivo aberto nao pode derrubar o hook nem deixar lixo na pasta.
    $pasta = Nova-Pasta
    $conteudo = Gravar-Status $pasta 'aguardando' 'Stop' ([DateTimeOffset]::Now.AddHours(-1))
    $arq = Join-Path $pasta ($IdPadrao + '.json')
    $leitor = [System.IO.File]::Open($arq, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try { $r = Rodar-Hook $pasta (Evento 'UserPromptSubmit') } finally { $leitor.Dispose() }
    $s = Ler-Status $pasta
    Conferir 'leitor sem compartilhar exclusao: arquivo continua um JSON completo' ($s -and (@('aguardando', 'executando') -ccontains $s.status))
    Conferir 'leitor sem compartilhar exclusao: sem temporarios' ((Contar $pasta '*.tmp') -eq 0)
    Conferir-Silencio 'leitor sem compartilhar exclusao' $r
    $pasta = Nova-Pasta
    [void](Gravar-Status $pasta 'aguardando' 'Stop' ([DateTimeOffset]::Now.AddHours(-1)))
    $arq = Join-Path $pasta ($IdPadrao + '.json')
    $compartilhado = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
    $leitor = [System.IO.File]::Open($arq, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $compartilhado)
    try { $r = Rodar-Hook $pasta (Evento 'UserPromptSubmit') } finally { $leitor.Dispose() }
    Conferir 'leitor compartilhando exclusao: status atualizado mesmo com o arquivo aberto' ((Ler-Status $pasta).status -ceq 'executando')
    Conferir-Silencio 'leitor compartilhando exclusao' $r

    Grupo 'hook: robustez'
    $entradas = [ordered]@{
        'entrada que nao e JSON' = 'isto nao e json'
        'entrada vazia'          = ''
        'entrada que e uma lista' = '[1,2,3]'
        'session_id ausente'     = '{"hook_event_name":"Stop","cwd":"C:\\Fontes\\Faturamento"}'
        'session_id com barra invertida' = (Evento 'Stop' '..\fora')
        'session_id com barra'   = (Evento 'Stop' 'a/b')
        'session_id com ponto'   = (Evento 'Stop' 'a.b')
        'session_id com espaco'  = (Evento 'Stop' 'a b')
        'session_id com quebra de linha no fim' = (Evento 'Stop' ("abc" + [char]10))
        'session_id com 101 caracteres' = (Evento 'Stop' ('a' * 101))
    }
    foreach ($nome in $entradas.Keys) {
        $base = Nova-Pasta
        $pasta = Join-Path $base 'sessoes'
        [void](New-Item -ItemType Directory -Path $pasta)
        $r = Rodar-Hook $pasta $entradas[$nome]
        Conferir ($nome + ': nenhum arquivo criado') ((Contar $pasta) -eq 0 -and (Contar $base) -eq 1)
        Conferir-Silencio $nome $r
    }
    $inexistente = Join-Path (Nova-Pasta) 'nao-existe'
    $r = Rodar-Hook $inexistente (Evento 'Stop')
    Conferir 'pasta de status inexistente: nada e criado' (-not (Test-Path -LiteralPath $inexistente))
    Conferir-Silencio 'pasta de status inexistente' $r
    $pasta = Nova-Pasta
    $r = Rodar-Hook $pasta (Evento 'UserPromptSubmit' $IdPadrao @{ prompt = 'SEGREDO-123 senha do banco'; tool_input = @{ command = 'SEGREDO-456' } })
    $t = [string](Ler-Texto $pasta)
    Conferir 'prompt confidencial: status gravado' ((Ler-Status $pasta).status -ceq 'executando')
    Conferir 'prompt confidencial: texto do evento fora do arquivo' (($t -notmatch 'SEGREDO') -and ($t -notmatch 'conversa\.jsonl') -and ($t -notmatch 'transcript'))
    Conferir-Silencio 'prompt confidencial' $r

    Grupo 'somente ASCII'
    Conferir-Ascii $Hook
    Conferir-Ascii $Abrir
    Conferir-Ascii $PSCommandPath

    # ----- abrir.ps1 ----------------------------------------------------------
    # Todos os casos abaixo sao recusados antes de iniciar qualquer agente.
    # Codigos: 2 = parametro invalido; 3 = repositorio nao encontrado; 1 = erro do proprio PowerShell
    # ao interpretar a linha de comando.

    function Conferir-Recusa([string]$Nome, [string[]]$Argumentos, [int[]]$Codigos = @(2)) {
        $r = Executar $Abrir $Argumentos ''
        Conferir ($Nome + ': recusado') ($Codigos -contains $r.Codigo) ('(codigo ' + $r.Codigo + ')')
        return $r
    }

    Grupo 'abrir: validacao do repositorio'
    foreach ($repo in @('..', '.', '...', 'a b', 'a;b', 'a&b', 'a|b', 'a\b', 'a/b', '..\Windows')) {
        [void](Conferir-Recusa ('-Repo ' + $repo) @('-Agente', 'codex', '-Repo', $repo, '-Modo', 'novo'))
    }
    [void](Conferir-Recusa 'sem -Repo' @('-Agente', 'codex', '-Modo', 'novo'))
    [void](Conferir-Recusa 'argumento solto depois de -Repo' @('-Agente', 'codex', '-Repo', 'a', 'b', '-Modo', 'novo') @(1, 2))

    Grupo 'abrir: validacao da sessao'
    [void](Conferir-Recusa '-Sessao com 36 hifens' @('-Agente', 'claude', '-Repo', 'Faturamento', '-Sessao', ('-' * 36), '-Modo', 'retomar') @(1, 2))
    [void](Conferir-Recusa '-Sessao com 36 letras fora de hexadecimal' @('-Agente', 'claude', '-Repo', 'Faturamento', '-Sessao', 'zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz', '-Modo', 'retomar'))
    [void](Conferir-Recusa '-Sessao com hifens fora do lugar' @('-Agente', 'claude', '-Repo', 'Faturamento', '-Sessao', '0b6f1c2e5-d1a-4c8e-9f3a-2b7d4e6a8c10', '-Modo', 'retomar'))
    [void](Conferir-Recusa '-Sessao com 35 caracteres' @('-Agente', 'claude', '-Repo', 'Faturamento', '-Sessao', '0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c1', '-Modo', 'retomar'))
    [void](Conferir-Recusa 'claude novo sem -Sessao' @('-Agente', 'claude', '-Repo', 'Faturamento', '-Modo', 'novo'))
    [void](Conferir-Recusa 'claude retomar sem -Sessao' @('-Agente', 'claude', '-Repo', 'Faturamento', '-Modo', 'retomar'))
    [void](Conferir-Recusa 'codex retomar sem -Sessao' @('-Agente', 'codex', '-Repo', 'Faturamento', '-Modo', 'retomar'))

    Grupo 'abrir: validacao do agente e do modo'
    [void](Conferir-Recusa 'agente desconhecido' @('-Agente', 'outro', '-Repo', 'Faturamento', '-Sessao', $IdPadrao, '-Modo', 'novo'))
    [void](Conferir-Recusa 'agente com maiuscula' @('-Agente', 'Claude', '-Repo', 'Faturamento', '-Sessao', $IdPadrao, '-Modo', 'novo'))
    [void](Conferir-Recusa 'sem -Agente' @('-Repo', 'Faturamento', '-Sessao', $IdPadrao, '-Modo', 'novo'))
    [void](Conferir-Recusa 'modo desconhecido' @('-Agente', 'claude', '-Repo', 'Faturamento', '-Sessao', $IdPadrao, '-Modo', 'apagar'))
    [void](Conferir-Recusa 'sem parametros' @())

    Grupo 'abrir: repositorio inexistente'
    $r = Conferir-Recusa 'repositorio valido e inexistente' @('-Agente', 'codex', '-Repo', 'painel-verificar-inexistente', '-Modo', 'novo') @(3)
    Conferir 'repositorio inexistente: mensagem' ($r.Saida -match 'nao encontrado')
    [void](Conferir-Recusa 'repositorio inexistente com sessao valida' @('-Agente', 'claude', '-Repo', 'painel-verificar-inexistente', '-Sessao', $IdPadrao, '-Modo', 'retomar') @(3))
}
finally {
    foreach ($p in $script:Pastas) {
        if ($p -and (Test-Path -LiteralPath $p)) {
            Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host ''
Write-Host ('Casos: ' + $script:Total + '   Falhas: ' + $script:Falhas)
if ($script:Falhas -gt 0) { exit 1 }
exit 0
