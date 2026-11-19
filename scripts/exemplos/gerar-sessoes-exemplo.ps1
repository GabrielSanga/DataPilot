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
    [string]$Dados = (Join-Path $env:TEMP 'datapilot-exemplo-demandas.json'),
    [string]$DadosVarias = (Join-Path $env:TEMP 'datapilot-exemplo-varias-demandas.json'),
    [string]$DadosLancamento = (Join-Path $env:TEMP 'datapilot-exemplo-lancamento.json'),
    [string]$DadosAguardando = (Join-Path $env:TEMP 'datapilot-exemplo-aguardando.json'),
    [string]$RaizFontes = (Join-Path $env:TEMP 'datapilot-exemplo-fontes'),
    [string]$DadosProgresso = (Join-Path $env:TEMP 'datapilot-exemplo-progresso.json'),
    [string]$DadosTrabalhos = (Join-Path $env:TEMP 'datapilot-exemplo-trabalhos.json')
)

$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
New-Item -ItemType Directory -Force -Path $Pasta | Out-Null
Get-ChildItem -Path $Pasta -File | Remove-Item -Force

$agora = [DateTimeOffset]::Now

function Gravar([string]$nome, [string]$conteudo) {
    $caminho = Join-Path $Pasta $nome
    [System.IO.File]::WriteAllText($caminho, $conteudo, $utf8)
    # O Windows reaproveita a data de criacao de um arquivo apagado ha pouco com o mesmo nome (tunelamento);
    # o vinculo automatico compara essa data, entao ela e fixada em "agora".
    [System.IO.File]::SetCreationTimeUtc($caminho, [DateTime]::UtcNow)
}

function Gravar-Dados([string]$conteudo) {
    [System.IO.File]::WriteAllText($Dados, $conteudo, $utf8)
}

function Gravar-DadosVarias([string]$conteudo) {
    [System.IO.File]::WriteAllText($DadosVarias, $conteudo, $utf8)
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

# Sessoes do arquivo de varias demandas (Etapa 4a). A 66666666... fica sem arquivo de status de proposito ("sem status").
Sessao '33333333-4444-5555-6666-777777777777' 'claude' 'C:\Fontes\Faturamento' 'atencao'    ([TimeSpan]::FromMinutes(3))
Sessao '55555555-6666-7777-8888-999999999999' 'claude' 'C:\Fontes\Estoque'     'executando' ([TimeSpan]::FromMinutes(1))
Sessao '77777777-8888-9999-aaaa-bbbbbbbbbbbb' 'claude' 'C:\Fontes\Faturamento' 'aguardando' ([TimeSpan]::FromHours(2))
Sessao '88888888-9999-aaaa-bbbb-cccccccccccc' 'claude' 'C:\Fontes\Compras'     'encerrada'  ([TimeSpan]::FromDays(5))

# Sessao do Codex NAO vinculada, com GUID (Etapa 4b): aparece como sugestao nos cards Faturamento de CHG-142, CHG-150 e Sem demanda
# (do arquivo de varias demandas) e em "Nao vinculadas" com a acao "Vincular". Status ativa, para nao mudar o resumo do cabecalho.
Sessao '99999999-aaaa-bbbb-cccc-dddddddddddd' 'codex' 'C:\Fontes\Faturamento' 'ativa' ([TimeSpan]::FromMinutes(2))

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

# Arquivo de dados com varias demandas (Etapa 4a), para ver a tela agrupada:
#  - sem-demanda: as sessoes da Etapa 3 (aparece sempre que tem repositorios);
#  - CHG-142: Faturamento (precisa de atencao) e Estoque (executando + uma sessao sem status);
#  - CHG-150: a MESMA pasta Faturamento, com a propria sessao (aguardando);
#  - CHG-099: arquivada (so aparece com "Mostrar arquivadas");
#  - CHG-200: demanda sem repositorios.
$variasJson = @"
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
    },
    {
      "id": "CHG-142",
      "titulo": "Ajuste NF-e",
      "openspecChange": "ajuste-nfe",
      "criadaEm": "$criada",
      "arquivada": false,
      "repositorios": [
        {
          "nome": "Faturamento",
          "pasta": "Faturamento",@@LANCAMENTOS@@
          "sessoes": [
            { "agente": "claude", "sessionId": "33333333-4444-5555-6666-777777777777", "rotulo": "implementacao", "criadaEm": "$criada" }
          ]
        },
        {
          "nome": "Estoque",
          "pasta": "Estoque",@@LANCAMENTOS_ESTOQUE@@
          "sessoes": [
            { "agente": "claude", "sessionId": "55555555-6666-7777-8888-999999999999", "rotulo": "ajuste de saldo", "criadaEm": "$criada" },
            { "agente": "claude", "sessionId": "66666666-7777-8888-9999-aaaaaaaaaaaa", "rotulo": "sem arquivo de status", "criadaEm": "$criada" }
          ]
        }
      ]
    },
    {
      "id": "CHG-150",
      "titulo": "Relatorio mensal",
      "criadaEm": "$criada",
      "arquivada": false,
      "repositorios": [
        {
          "nome": "Faturamento",
          "pasta": "Faturamento",
          "sessoes": [
            { "agente": "claude", "sessionId": "77777777-8888-9999-aaaa-bbbbbbbbbbbb", "criadaEm": "$criada" }
          ]
        }
      ]
    },
    {
      "id": "CHG-099",
      "titulo": "Concluida",
      "criadaEm": "$criada",
      "arquivada": true,
      "repositorios": [
        {
          "nome": "Compras",
          "pasta": "Compras",
          "sessoes": [
            { "agente": "claude", "sessionId": "88888888-9999-aaaa-bbbb-cccccccccccc", "criadaEm": "$criada" }
          ]
        }
      ]
    },
    {
      "id": "CHG-200",
      "titulo": "Sem repositorios ainda",
      "criadaEm": "$criada",
      "arquivada": false,
      "repositorios": []
    }
  ]
}
"@

Gravar-DadosVarias $variasJson.Replace('@@LANCAMENTOS@@', '').Replace('@@LANCAMENTOS_ESTOQUE@@', '')

# Arquivo de dados com um lancamento pendente (Etapa 4c): igual ao de varias demandas, mas o repositorio Faturamento de
# CHG-142 tem um lancamento do Codex de uma hora atras, com rotulo. A sessao 99999999... do Codex, criada agora na pasta
# Faturamento, e vinculada sozinha a esse card ao abrir o painel. "Remover" a devolve a "Nao vinculadas" e ela nao volta.
$lancamentoJson = '"lancamentos": [ { "id": "aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa", "agente": "codex", "rotulo": "ajuste automatico", "criadoEm": "' +
    $agora.AddHours(-1).ToString('o') + '" } ],'
[System.IO.File]::WriteAllText($DadosLancamento,
    $variasJson.Replace('@@LANCAMENTOS@@', $lancamentoJson).Replace('@@LANCAMENTOS_ESTOQUE@@', ''), $utf8)

# Arquivo de dados com lancamentos pendentes SEM sessao correspondente (Etapa 4d): o repositorio Estoque de CHG-142 tem
# dois lancamentos do Codex, um de 5 minutos atras (com rotulo) e um de 3 horas atras (sem rotulo). Nao ha sessao do Codex
# na pasta Estoque, entao nada os vincula: o card mostra as duas linhas "aguardando a primeira mensagem", a mais recente
# primeiro, com a acao "Cancelar". O repositorio Faturamento de CHG-142 fica sem lancamento.
$aguardandoJson = '"lancamentos": [ ' +
    '{ "id": "bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb", "agente": "codex", "criadoEm": "' + $agora.AddHours(-3).ToString('o') + '" }, ' +
    '{ "id": "cccccccc-3333-4333-8333-cccccccccccc", "agente": "codex", "rotulo": "aguardando exemplo", "criadoEm": "' + $agora.AddMinutes(-5).ToString('o') + '" } ],'
[System.IO.File]::WriteAllText($DadosAguardando,
    $variasJson.Replace('@@LANCAMENTOS@@', '').Replace('@@LANCAMENTOS_ESTOQUE@@', $aguardandoJson), $utf8)

# Raiz de fontes de exemplo e arquivo de dados para o progresso do OpenSpec (Etapa 5a). Uso:
#   dotnet run --project src/DataPilot.Wpf -- --pasta-status "$env:TEMP\datapilot-exemplo" `
#       --dados "$env:TEMP\datapilot-exemplo-progresso.json" --raiz-fontes "$env:TEMP\datapilot-exemplo-fontes"
# Cada demanda mostra uma indicacao diferente:
#  - CHG-142 (ajuste-nfe): Faturamento ativa 4 de 6 + Estoque ativa 3 de 6 = "7 de 12 tarefas";
#  - CHG-150 (relatorio-mensal): Faturamento arquivada 2 de 2, Compras com a pasta da change trocada por juncao
#    ("progresso indisponivel"), Portal com a change ativa sem tarefas ("sem tarefas") e Estoque sem a change (sem indicacao);
#  - CHG-160 (change-fantasma): nenhum repositorio tem a change ("change nao encontrada nos repositorios").
# Para ver a atualizacao: marque uma tarefa em Faturamento\openspec\changes\ajuste-nfe\tasks.md e espere ate 5 segundos.
function Remover-RaizExemplo([string]$raiz) {
    if (-not (Test-Path -LiteralPath $raiz)) { return }
    if ((Split-Path -Leaf $raiz) -notlike 'datapilot-exemplo*') {
        throw "Recusado: a raiz de exemplo '$raiz' deve ter nome iniciado por 'datapilot-exemplo'."
    }
    # Juncoes saem primeiro, so o atalho, para nunca apagar o conteudo do destino.
    Get-ChildItem -LiteralPath $raiz -Recurse -Force -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
        ForEach-Object { [System.IO.Directory]::Delete($_.FullName) }
    Remove-Item -LiteralPath $raiz -Recurse -Force
}

function Gravar-Tasks([string]$repo, [string]$caminhoChange, [int]$marcadas, [int]$pendentes) {
    $dir = Join-Path $RaizFontes "$repo\openspec\changes\$caminhoChange"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $linhas = @('# Tasks', '')
    # 1..0 contaria de tras para frente em PowerShell; por isso os lacos com contagem explicita.
    for ($i = 1; $i -le $marcadas; $i++) { $linhas += "- [x] 1.$i Tarefa concluida $i" }
    for ($i = 1; $i -le $pendentes; $i++) { $linhas += "- [ ] 2.$i Tarefa pendente $i" }
    [System.IO.File]::WriteAllText((Join-Path $dir 'tasks.md'), (($linhas -join "`n") + "`n"), $utf8)
}

Remover-RaizExemplo $RaizFontes
New-Item -ItemType Directory -Force -Path $RaizFontes | Out-Null
Gravar-Tasks 'Faturamento' 'ajuste-nfe' 4 2
Gravar-Tasks 'Estoque' 'ajuste-nfe' 3 3
Gravar-Tasks 'Faturamento' 'archive\2026-11-20-relatorio-mensal' 2 0
Gravar-Tasks 'Compras' '_destino-da-juncao' 5 5
New-Item -ItemType Junction -Path (Join-Path $RaizFontes 'Compras\openspec\changes\relatorio-mensal') `
    -Target (Join-Path $RaizFontes 'Compras\openspec\changes\_destino-da-juncao') | Out-Null
$portal = Join-Path $RaizFontes 'Portal\openspec\changes\relatorio-mensal'
New-Item -ItemType Directory -Force -Path $portal | Out-Null
[System.IO.File]::WriteAllText((Join-Path $portal 'tasks.md'), "# Tasks`n`nsem caixas de selecao`n", $utf8)
New-Item -ItemType Directory -Force -Path (Join-Path $RaizFontes 'Estoque\openspec\changes') | Out-Null

# Cada trabalho mostra uma indicacao diferente (a change fica em cada repositorio):
#  - CHG-142: Faturamento (ajuste-nfe) 4 de 6 e Estoque (ajuste-nfe) 3 de 6, cada um no proprio card, sem soma no topo;
#  - CHG-150: Faturamento (relatorio-mensal) arquivada 2 de 2, Compras com a pasta da change trocada por juncao
#    ("progresso indisponivel"), Portal com a change ativa sem tarefas ("sem tarefas") e Estoque sem change;
#  - CHG-160: Faturamento com a change fantasma ("change nao encontrada").
$progressoJson = @"
{
  "demandas": [
    {
      "id": "CHG-142", "titulo": "Ajuste NF-e", "criadaEm": "$criada", "arquivada": false,
      "repositorios": [
        { "nome": "Faturamento", "pasta": "Faturamento", "openspecChange": "ajuste-nfe", "sessoes": [] },
        { "nome": "Estoque", "pasta": "Estoque", "openspecChange": "ajuste-nfe", "sessoes": [] }
      ]
    },
    {
      "id": "CHG-150", "titulo": "Relatorio mensal", "criadaEm": "$criada", "arquivada": false,
      "repositorios": [
        { "nome": "Faturamento", "pasta": "Faturamento", "openspecChange": "relatorio-mensal", "sessoes": [] },
        { "nome": "Compras", "pasta": "Compras", "openspecChange": "relatorio-mensal", "sessoes": [] },
        { "nome": "Portal", "pasta": "Portal", "openspecChange": "relatorio-mensal", "sessoes": [] },
        { "nome": "Estoque", "pasta": "Estoque", "sessoes": [] }
      ]
    },
    {
      "id": "CHG-160", "titulo": "Change que nao existe", "criadaEm": "$criada", "arquivada": false,
      "repositorios": [
        { "nome": "Faturamento", "pasta": "Faturamento", "openspecChange": "change-fantasma", "sessoes": [] }
      ]
    }
  ]
}
"@
[System.IO.File]::WriteAllText($DadosProgresso, $progressoJson, $utf8)

# Arquivo de dados "Trabalhos e Sessoes": o de varias demandas com a change em cada repositorio, sessoes em "Sessoes"
# (sem-demanda), um trabalho (CHG-170) com a change herdada do campo antigo da demanda e UMA SESSAO EXCLUIDA
# (22222222..., do Claude em Estoque): ela existe na pasta de status, mas nao aparece em lugar nenhum do painel.
# Use uma copia, porque criar, excluir e vincular gravam no arquivo.
$trabalhosJson = @"
{
  "demandas": [
    {
      "id": "sem-demanda", "titulo": "Sem demanda", "arquivada": false,
      "repositorios": [
        { "nome": "Faturamento", "pasta": "Faturamento", "sessoes": [
            { "agente": "claude", "sessionId": "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10", "rotulo": "implementacao", "criadaEm": "$criada" } ] },
        { "nome": "Estoque", "pasta": "Estoque", "sessoes": [
            { "agente": "claude", "sessionId": "11111111-2222-3333-4444-555555555555", "criadaEm": "$criada" } ] }
      ]
    },
    {
      "id": "t3f9a1c20", "titulo": "Ajuste NF-e", "criadaEm": "$criada", "arquivada": false,
      "repositorios": [
        { "nome": "Faturamento", "pasta": "Faturamento", "openspecChange": "ajuste-nfe", "sessoes": [
            { "agente": "claude", "sessionId": "33333333-4444-5555-6666-777777777777", "rotulo": "implementacao", "criadaEm": "$criada" } ] },
        { "nome": "Estoque", "pasta": "Estoque", "openspecChange": "ajuste-nfe", "sessoes": [
            { "agente": "claude", "sessionId": "55555555-6666-7777-8888-999999999999", "rotulo": "ajuste de saldo", "criadaEm": "$criada" },
            { "agente": "claude", "sessionId": "66666666-7777-8888-9999-aaaaaaaaaaaa", "criadaEm": "$criada" } ] }
      ]
    },
    {
      "id": "t7b20d4e1", "titulo": "Relatorio mensal", "criadaEm": "$criada", "arquivada": false,
      "repositorios": [
        { "nome": "Faturamento", "pasta": "Faturamento", "openspecChange": "relatorio-mensal", "sessoes": [
            { "agente": "claude", "sessionId": "77777777-8888-9999-aaaa-bbbbbbbbbbbb", "criadaEm": "$criada" } ] },
        { "nome": "Portal", "pasta": "Portal", "openspecChange": "relatorio-mensal", "sessoes": [] }
      ]
    },
    {
      "id": "CHG-170", "titulo": "Trabalho antigo", "openspecChange": "ajuste-nfe", "criadaEm": "$criada", "arquivada": false,
      "repositorios": [
        { "nome": "Estoque", "pasta": "Estoque", "sessoes": [] }
      ]
    },
    {
      "id": "t0000c0de", "titulo": "Sem repositorios ainda", "criadaEm": "$criada", "arquivada": false,
      "repositorios": []
    }
  ],
  "excluidas": [
    { "agente": "claude", "sessionId": "22222222-3333-4444-5555-666666666666", "excluidaEm": "$criada" }
  ]
}
"@
[System.IO.File]::WriteAllText($DadosTrabalhos, $trabalhosJson, $utf8)

Write-Output "Raiz de fontes de exemplo (progresso do OpenSpec) criada em: $RaizFontes"
Write-Output "Arquivo de dados do progresso do OpenSpec criado em: $DadosProgresso"
Write-Output "Arquivo de dados de trabalhos e sessoes criado em: $DadosTrabalhos"
Write-Output "Pasta de exemplo criada em: $Pasta"
Write-Output "Arquivo de dados de exemplo criado em: $Dados"
Write-Output "Arquivo de dados de varias demandas criado em: $DadosVarias"
Write-Output "Arquivo de dados com lancamento pendente criado em: $DadosLancamento"
Write-Output "Arquivo de dados com lancamentos aguardando a primeira mensagem criado em: $DadosAguardando"
