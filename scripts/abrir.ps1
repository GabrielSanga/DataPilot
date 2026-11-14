# Abre ou retoma uma sessao do Claude Code ou do Codex num repositorio. Roda como o usuario ia,
# chamado pelo SSH local:
#
#   ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1
#       -Agente claude -Repo Faturamento -Sessao <guid> -Modo novo
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
# Nome de pasta: so letras, digitos, "_", "." e "-", e nunca formado apenas por pontos.
if ($Repo -notmatch '\A[A-Za-z0-9_.-]+\z' -or $Repo -match '\A\.+\z') {
    Recusar 2 'Parametro -Repo invalido: use apenas letras, digitos, "_", "." e "-".'
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

# A pasta precisa existir e ser filha direta da raiz, com exatamente o nome pedido.
$raiz = [System.IO.Path]::GetFullPath($RaizFontes).TrimEnd('\')
$pasta = Join-Path $raiz $Repo
$item = $null
if (Test-Path -LiteralPath $pasta -PathType Container) {
    $item = Get-Item -LiteralPath $pasta -Force
}
if (-not $item -or -not $item.Parent -or
    ($item.Parent.FullName.TrimEnd('\') -ine $raiz) -or ($item.Name -ine $Repo)) {
    Recusar 3 ('Repositorio nao encontrado em ' + $raiz + ': ' + $Repo)
}

# --- Inicio do agente --------------------------------------------------------

if (-not (Get-Command $Agente -ErrorAction SilentlyContinue)) {
    Recusar 4 ('Comando nao encontrado: ' + $Agente)
}

Set-Location -LiteralPath $item.FullName

switch ("$Agente-$Modo") {
    'claude-novo'    { & claude --session-id $Sessao }
    'claude-retomar' { & claude --resume $Sessao }
    'codex-novo'     { & codex }
    'codex-retomar'  { & codex resume $Sessao }
}

$codigo = $LASTEXITCODE
if ($null -eq $codigo) { $codigo = 0 }
exit $codigo
