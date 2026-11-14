<#
.SYNOPSIS
    Gera uma pasta de exemplo com arquivos de status para testar o painel sem a conta `ia`.

.DESCRIPTION
    As datas sao relativas a hora atual, entao as cores e a ordem esperadas sempre valem logo apos a geracao.
    Mistura arquivos validos, invalidos e antigos. Roteiro de conferencia no README.md.

.EXAMPLE
    ./scripts/exemplos/gerar-sessoes-exemplo.ps1
    dotnet run --project src/DataPilot.Wpf -- --pasta-status "$env:TEMP\datapilot-exemplo"
#>
param(
    [string]$Pasta = (Join-Path $env:TEMP 'datapilot-exemplo'),
    [string]$Dados = (Join-Path $env:TEMP 'datapilot-exemplo-demandas.json')
)

$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
New-Item -ItemType Directory -Force -Path $Pasta | Out-Null
Get-ChildItem -Path $Pasta -File | Remove-Item -Force

$agora = [DateTimeOffset]::Now

function Gravar([string]$nome, [string]$conteudo) {
    [System.IO.File]::WriteAllText((Join-Path $Pasta $nome), $conteudo, $utf8)
}

function Gravar-Dados([string]$conteudo) {
    [System.IO.File]::WriteAllText($Dados, $conteudo, $utf8)
}

function Sessao([string]$id, [string]$agente, [string]$cwd, [string]$status, [TimeSpan]$idade) {
    $obj = [ordered]@{
        agente     = $agente
        session_id = $id
        cwd        = $cwd
        status     = $status
        evento     = 'Exemplo'
        atualizado = ($agora - $idade).ToString('o')
    }
    Gravar "$id.json" ($obj | ConvertTo-Json -Compress)
}

# Validos, na ordem esperada: atencao, aguardando (mais recente primeiro), executando, ativa, inativa, encerrada.
Sessao 'ex-atencao'      'codex'  'C:\Fontes\Faturamento'      'atencao'    ([TimeSpan]::FromMinutes(2))
Sessao 'ex-aguardando-1' 'claude' 'C:\Fontes\Estoque'          'aguardando' ([TimeSpan]::FromHours(1))
Sessao 'ex-aguardando-2' 'claude' 'C:\Fontes\Compras\'         'aguardando' ([TimeSpan]::FromHours(3))
Sessao 'ex-executando-1' 'claude' 'C:\Fontes\Portal'           'executando' ([TimeSpan]::FromMinutes(1))
Sessao 'ex-executando-2' 'codex'  'C:\Fontes\Relatorios'       'executando' ([TimeSpan]::FromMinutes(5))
Sessao 'ex-ativa'        'claude' 'C:\Fontes\Novo'             'ativa'      ([TimeSpan]::FromMinutes(1))
Sessao 'ex-inativa-exec' 'claude' 'C:\Fontes\Parado'           'executando' ([TimeSpan]::FromMinutes(20))
Sessao 'ex-inativa-codex' 'codex' 'C:\Fontes\AbaFechada'       'aguardando' ([TimeSpan]::FromHours(9))
Sessao 'ex-encerrada'    'claude' 'C:\Fontes\Antigo'           'encerrada'  ([TimeSpan]::FromDays(1))

# Texto problematico, mas valido: cwd com mais de 260 caracteres e com quebras de linha.
Sessao 'ex-cwd-longo'    'claude' ('C:\' + ('PastaMuitoLonga\' * 25) + 'Final') 'aguardando' ([TimeSpan]::FromMinutes(30))
Sessao 'ex-cwd-controle' 'codex'  "C:\Fontes`r`nLinhaDois`tTab"        'executando' ([TimeSpan]::FromMinutes(8))

# Antigos: ocultos por padrao (aparecem com "Mostrar antigas").
Sessao 'ex-antiga-encerrada' 'claude' 'C:\Fontes\UmaSemana' 'encerrada'  ([TimeSpan]::FromDays(7))
Sessao 'ex-antiga-inativa'   'codex'  'C:\Fontes\QuatroDias' 'executando' ([TimeSpan]::FromDays(4))

# Invalidos: nao devem aparecer nunca.
Gravar 'ex-json-ruim.json' 'isto nao e json'
Sessao 'ex-status-ruim' 'claude' 'C:\Fontes\X' 'dormindo' ([TimeSpan]::FromMinutes(1))
Gravar 'ex-id-diferente.json' (([ordered]@{
    agente = 'claude'; session_id = 'outro-id'; cwd = 'C:\Fontes\X'; status = 'executando'
    evento = 'Exemplo'; atualizado = $agora.ToString('o')
}) | ConvertTo-Json -Compress)
Gravar 'ex-sem-fuso.json' '{"agente":"claude","session_id":"ex-sem-fuso","cwd":"C:\\X","status":"ativa","atualizado":"2026-01-01T10:00:00"}'
Gravar 'ex-grande.json' ('{"agente":"claude","session_id":"ex-grande","cwd":"C:\\X","status":"ativa","atualizado":"' +
    $agora.ToString('o') + '","enchimento":"' + ('x' * 20000) + '"}')
Gravar 'ex-temporario.123.tmp' '{"agente":"claude","session_id":"ex-temporario","cwd":"C:\\X","status":"ativa","atualizado":"' + $agora.ToString('o') + '"}'

# Sessoes com identificador GUID, para o arquivo de dados de exemplo (Etapa 3):
#  - 0b6f1c2e... registrada em Faturamento, encerrada, com cwd divergente: "Retomar" abre sem confirmacao em Faturamento;
#  - 11111111... registrada em Estoque, ativa: "Retomar" pede confirmacao;
#  - 22222222... NAO registrada (como a sessao criada por /clear): sem "Retomar".
Sessao '0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10' 'claude' 'C:\Windows' 'encerrada' ([TimeSpan]::FromHours(2))
Sessao '11111111-2222-3333-4444-555555555555' 'claude' 'C:\Fontes\Estoque' 'ativa' ([TimeSpan]::FromMinutes(1))
Sessao '22222222-3333-4444-5555-666666666666' 'claude' 'C:\Fontes\Estoque' 'ativa' ([TimeSpan]::FromMinutes(1))

# Arquivo de dados de exemplo (fora da pasta de status, que so le arquivos .json de sessao).
$criada = $agora.AddDays(-1).ToString('o')
Gravar-Dados @"
{
  "demandas": [
    {
      "id": "sem-demanda",
      "titulo": "Sem demanda",
      "arquivada": false,
      "repositorios": [
        {
          "nome": "Faturamento",
          "pasta": "Faturamento",
          "sessoes": [
            { "agente": "claude", "sessionId": "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10", "rotulo": "implementacao", "criadaEm": "$criada" }
          ]
        },
        {
          "nome": "Estoque",
          "pasta": "Estoque",
          "sessoes": [
            { "agente": "claude", "sessionId": "11111111-2222-3333-4444-555555555555", "criadaEm": "$criada" }
          ]
        }
      ]
    }
  ]
}
"@

Write-Output "Pasta de exemplo criada em: $Pasta"
Write-Output "Arquivo de dados de exemplo criado em: $Dados"
