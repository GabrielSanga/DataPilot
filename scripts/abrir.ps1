# Abre ou retoma uma sessao do Claude Code ou do Codex num repositorio. Roda como o usuario ia,
# chamado pelo SSH local:
#
#   ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1
#       -Agente claude -Repo Faturamento -Sessao <guid> -Modo novo
#
# -Repo e uma pasta direta da raiz dos fontes. Para uma pasta fora dela, use -Caminho com o caminho
# completo codificado em Base64 (UTF-8): a linha do ssh e reinterpretada pelo Windows, e o Base64
# impede que espacos, acentos ou caracteres como & e ; virem parte de outro comando.
#
# Todos os parametros sao validados antes de qualquer acao. Nenhum valor precisa de aspas.
#
# Codigos de saida proprios: 2 = parametro invalido; 3 = repositorio nao encontrado;
# 4 = comando do agente nao encontrado. Nos demais casos, o codigo e o do proprio agente.
#
# Compativel com powershell.exe 5.1. Manter este arquivo somente em ASCII.
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$Agente,
    [string]$Repo,
    [string]$Caminho,
    [string]$Sessao,
    [string]$Modo = 'novo'
)

# Raiz dos fontes do usuario ia. E o unico lugar onde ela e definida; nunca vem por parametro.
$RaizFontes = 'C:\Fontes'

function Recusar([int]$Codigo, [string]$Mensagem) {
    Write-Host $Mensagem
    exit $Codigo
}

# --- Validacao dos parametros ------------------------------------------------

if ($Agente -cne 'claude' -and $Agente -cne 'codex') {
    Recusar 2 'Parametro -Agente invalido: use claude ou codex.'
}
if ($Modo -cne 'novo' -and $Modo -cne 'retomar') {
    Recusar 2 'Parametro -Modo invalido: use novo ou retomar.'
}
if ([bool]$Repo -eq [bool]$Caminho) {
    Recusar 2 'Informe -Repo ou -Caminho (um dos dois).'
}
if ($Repo) {
    # Nome de pasta: so letras, digitos, "_", "." e "-", e nunca formado apenas por pontos.
    if ($Repo -notmatch '\A[A-Za-z0-9_.-]+\z' -or $Repo -match '\A\.+\z') {
        Recusar 2 'Parametro -Repo invalido: use apenas letras, digitos, "_", "." e "-".'
    }
} else {
    # Caminho completo de unidade local, ja normalizado, sem caracteres de controle nem curingas.
    $texto = $null
    try {
        $texto = (New-Object System.Text.UTF8Encoding($false, $true)).GetString([Convert]::FromBase64String($Caminho))
    } catch {
        Recusar 2 'Parametro -Caminho invalido: use o caminho completo codificado em Base64 (UTF-8).'
    }
    # Sem trechos "." e "..", sem "\\" e sem barra no fim: o caminho ja chega normalizado.
    if ($texto -notmatch '\A[A-Za-z]:\\[^\x00-\x1f*?:"<>|/]+\z' -or $texto.Length -gt 260 -or
        $texto.EndsWith('\') -or $texto.Contains('\\') -or $texto -match '\\\.{1,2}(\\|\z)') {
        Recusar 2 'Parametro -Caminho invalido: use o caminho completo de uma pasta local.'
    }
}
if ($Sessao) {
    # GUID na forma canonica 8-4-4-4-12.
    $guid = [guid]::Empty
    $canonico = $Sessao -match '\A[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\z'
    if (-not $canonico -or -not [guid]::TryParseExact($Sessao, 'D', [ref]$guid)) {
        Recusar 2 'Parametro -Sessao invalido: use um GUID no formato 00000000-0000-0000-0000-000000000000.'
    }
}
# O Claude sempre recebe o identificador; o Codex so na retomada.
if (-not $Sessao -and ($Modo -ceq 'retomar' -or $Agente -ceq 'claude')) {
    Recusar 2 'Parametro -Sessao obrigatorio para esta combinacao de agente e modo.'
}

# --- Pasta do repositorio ----------------------------------------------------

$item = $null
if ($Repo) {
    # A pasta precisa existir e ser filha direta da raiz, com exatamente o nome pedido.
    $raiz = [System.IO.Path]::GetFullPath($RaizFontes).TrimEnd('\')
    $pasta = Join-Path $raiz $Repo
    if (Test-Path -LiteralPath $pasta -PathType Container) {
        $item = Get-Item -LiteralPath $pasta -Force
    }
    if (-not $item -or -not $item.Parent -or
        ($item.Parent.FullName.TrimEnd('\') -ine $raiz) -or ($item.Name -ine $Repo)) {
        Recusar 3 ('Repositorio nao encontrado em ' + $raiz + ': ' + $Repo)
    }
} else {
    # A pasta precisa existir e nao pode ser atalho nem juncao.
    if (Test-Path -LiteralPath $texto -PathType Container) {
        $item = Get-Item -LiteralPath $texto -Force
    }
    if (-not $item -or ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
        Recusar 3 ('Pasta nao encontrada ou sem acesso: ' + $texto)
    }
}

# --- Inicio do agente --------------------------------------------------------

if (-not (Get-Command $Agente -ErrorAction SilentlyContinue)) {
    Recusar 4 ('Comando nao encontrado: ' + $Agente)
}

Set-Location -LiteralPath $item.FullName

$inicio = Get-Date

switch ("$Agente-$Modo") {
    'claude-novo'    { & claude --session-id $Sessao }
    'claude-retomar' { & claude --resume $Sessao }
    'codex-novo'     { & codex }
    'codex-retomar'  { & codex resume $Sessao }
}

$codigo = $LASTEXITCODE
if ($null -eq $codigo) { $codigo = 0 }

# O Claude so grava a conversa depois da primeira mensagem. Se a retomada falha logo, a sessao
# provavelmente foi fechada antes disso: inicia como sessao nova, com o mesmo identificador.
# O Codex nao tem segunda tentativa.
if ("$Agente-$Modo" -ceq 'claude-retomar' -and $codigo -ne 0 -and
    ((Get-Date) - $inicio).TotalSeconds -lt 30) {
    Write-Host 'A conversa ainda nao tem mensagens. Iniciando a sessao com o mesmo identificador.'
    & claude --session-id $Sessao
    $codigo = $LASTEXITCODE
    if ($null -eq $codigo) { $codigo = 0 }
}

exit $codigo
