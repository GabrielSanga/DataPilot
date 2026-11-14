# Hook de status do Painel de Demandas. Roda como o usuario ia, chamado pelo Claude Code e pelo Codex.
#
# Le o evento de hook pela entrada padrao (JSON em UTF-8) e grava o status da sessao em
# <PastaStatus>\<session_id>.json.
#
# Regras que nao podem ser quebradas:
#   - nunca escrever na saida padrao (em SessionStart e UserPromptSubmit ela vira contexto do modelo);
#   - sempre terminar com codigo 0 (codigo 2 bloqueia a acao do agente);
#   - gravar somente os seis campos do contrato, nada do conteudo do evento.
#
# Compativel com powershell.exe 5.1. Manter este arquivo somente em ASCII.
param(
    [string]$Agente = 'claude',
    [string]$PastaStatus = 'C:\ProgramData\Painel\sessoes'
)

try {
    $ErrorActionPreference = 'Stop'

    # Instante do evento: usado em "atualizado" e para nao regredir um status mais novo.
    $agora = [DateTimeOffset]::Now

    if ($Agente -cne 'claude' -and $Agente -cne 'codex') { exit 0 }

    # A entrada e lida como UTF-8 puro; [Console]::In usaria a pagina de codigo do console.
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $leitor = New-Object System.IO.StreamReader([Console]::OpenStandardInput(), $utf8)
    $texto = $leitor.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($texto)) { exit 0 }

    $ev = $texto | ConvertFrom-Json
    if ($null -eq $ev) { exit 0 }

    # O identificador vira nome de arquivo: so letras, digitos, "_" e "-".
    $id = [string]$ev.session_id
    if ($id -notmatch '\A[A-Za-z0-9_-]{1,100}\z') { exit 0 }

    $evento = [string]$ev.hook_event_name
    $status = $null
    switch -CaseSensitive ($evento) {
        'SessionStart'      { $status = 'ativa' }
        'UserPromptSubmit'  { $status = 'executando' }
        'PostToolUse'       { $status = 'executando' }
        'PermissionRequest' { $status = 'atencao' }
        'Stop'              { $status = 'aguardando' }
        'SessionEnd'        { $status = 'encerrada' }
        'Notification' {
            # So pedidos de permissao e perguntas; o aviso de ociosidade (idle_prompt) e ignorado.
            $tipo = [string]$ev.notification_type
            if ($tipo -ceq 'permission_prompt' -or $tipo -ceq 'elicitation_dialog') { $status = 'atencao' }
        }
    }
    if (-not $status) { exit 0 }

    if (-not [System.IO.Directory]::Exists($PastaStatus)) { exit 0 }
    $destino = Join-Path $PastaStatus ($id + '.json')

    # Eventos assincronos podem terminar fora de ordem: nao substituir um status mais novo.
    try {
        if ([System.IO.File]::Exists($destino)) {
            $anterior = [System.IO.File]::ReadAllText($destino, $utf8) | ConvertFrom-Json
            $valor = $anterior.atualizado
            $instante = [DateTimeOffset]::MinValue
            $lido = $false
            if ($valor -is [datetime]) {
                $instante = [DateTimeOffset]$valor
                $lido = $true
            } elseif ($valor) {
                $lido = [DateTimeOffset]::TryParse([string]$valor, [System.Globalization.CultureInfo]::InvariantCulture,
                    [System.Globalization.DateTimeStyles]::None, [ref]$instante)
            }
            if ($lido -and $instante -gt $agora) { exit 0 }
        }
    } catch {
        # Arquivo anterior ilegivel ou invalido: segue e substitui.
    }

    $obj = [ordered]@{
        agente     = $Agente
        session_id = $id
        cwd        = [string]$ev.cwd
        status     = $status
        evento     = $evento
        atualizado = $agora.ToString('o')
    }
    $json = $obj | ConvertTo-Json -Compress

    # Escrita atomica: temporario unico por processo na mesma pasta, UTF-8 sem BOM, e troca sobre o destino.
    # O leitor ve sempre o arquivo anterior ou o novo, completos. Um leitor que abra o arquivo sem
    # compartilhar exclusao pode fazer a troca falhar; por isso as novas tentativas.
    $tmp = Join-Path $PastaStatus ('{0}.{1}.tmp' -f $id, $PID)
    try {
        [System.IO.File]::WriteAllText($tmp, $json, $utf8)
        for ($i = 0; $i -lt 10; $i++) {
            try {
                if ([System.IO.File]::Exists($destino)) {
                    # [NullString]::Value: um $null comum viraria texto vazio e a troca falharia.
                    [System.IO.File]::Replace($tmp, $destino, [NullString]::Value)
                } else {
                    [System.IO.File]::Move($tmp, $destino)
                }
                break
            } catch {
                Start-Sleep -Milliseconds 20
            }
        }
    } finally {
        if ([System.IO.File]::Exists($tmp)) { [System.IO.File]::Delete($tmp) }
    }
} catch {
    # Nenhuma falha pode chegar ao agente.
}
exit 0
