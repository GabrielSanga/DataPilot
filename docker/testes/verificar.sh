#!/bin/bash
# Verificacao dos scripts do container (abrir.sh e hook.mjs). Roda dentro do container:
#
#   docker cp docker/testes/verificar.sh datapilot-agente:/tmp/verificar.sh
#   docker exec datapilot-agente bash /tmp/verificar.sh
#
# O primeiro parametro troca a pasta dos scripts (padrao: /opt/painel). Nenhum agente real e
# iniciado. Termina com codigo 0 se todos os casos passarem e 1 se algum falhar.
pasta_scripts=${1:-/opt/painel}
abrir=$pasta_scripts/abrir.sh
hook=$pasta_scripts/hook.mjs
id=0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10
total=0
falhas=0
base=$(mktemp -d) || exit 1
trap 'rm -rf "$base"' EXIT

grupo() { printf '\n%s\n' "$1"; }

# conferir <nome> <comando de teste...>
conferir() {
    local nome=$1
    shift
    total=$((total + 1))
    if "$@"; then
        echo "  ok     $nome"
    else
        falhas=$((falhas + 1))
        echo "  FALHA  $nome"
    fi
}

nova_pasta() { mktemp -d "$base/XXXXXX"; }
contar() { ls -A "$1" | wc -l; }

# ----- hook.mjs ------------------------------------------------------------

# evento <nome> [id] [json com campos extras]
evento() {
    node -e '
        const [nome, id, extra] = process.argv.slice(1);
        process.stdout.write(JSON.stringify({
            session_id: id, transcript_path: "/home/node/.claude/projects/x/conversa.jsonl",
            cwd: "/workspace/Faturamento", hook_event_name: nome, ...JSON.parse(extra || "{}"),
        }));' "$1" "${2-$id}" "${3-}"
}

# rodar_hook <pasta> <entrada> [agente]
rodar_hook() {
    printf '%s' "$2" | node "$hook" "${3-claude}" "$1" >"$base/saida" 2>"$base/erro"
    codigo=$?
}

silencio() {
    conferir "$1: codigo 0" test "$codigo" -eq 0
    conferir "$1: sem saida padrao" test ! -s "$base/saida"
    conferir "$1: sem saida de erro" test ! -s "$base/erro"
}

# campo <pasta> <nome do campo>; "*" devolve os nomes dos campos
campo() {
    node -e '
        try {
            const o = JSON.parse(require("fs").readFileSync(process.argv[1], "utf8"));
            process.stdout.write(process.argv[2] === "*" ? Object.keys(o).join(",") : String(o[process.argv[2]] ?? ""));
        } catch {}' "$1/$id.json" "$2"
}

texto() { cat "$1/$id.json" 2>/dev/null; }

# gravar_status <pasta> <status> <evento> <deslocamento em ms>
gravar_status() {
    node -e '
        const [arquivo, id, status, evento, ms] = process.argv.slice(1);
        require("fs").writeFileSync(arquivo, JSON.stringify({ agente: "claude", session_id: id,
            cwd: "/workspace/Faturamento", status, evento, atualizado: new Date(Date.now() + Number(ms)).toISOString() }));
    ' "$1/$id.json" "$id" "$2" "$3" "$4"
}

grupo 'hook: mapa de evento para status'
for par in SessionStart:ativa UserPromptSubmit:executando PostToolUse:executando \
    PermissionRequest:atencao Stop:aguardando SessionEnd:encerrada; do
    nome=${par%:*}
    pasta=$(nova_pasta)
    rodar_hook "$pasta" "$(evento "$nome")"
    silencio "$nome"
    conferir "$nome grava ${par#*:}" test "$(campo "$pasta" status)" = "${par#*:}"
    conferir "$nome: campo evento" test "$(campo "$pasta" evento)" = "$nome"
done

grupo 'hook: contrato do arquivo'
pasta=$(nova_pasta)
rodar_hook "$pasta" "$(evento SessionStart)"
conferir 'exatamente os seis campos' test "$(campo "$pasta" '*')" = 'agente,session_id,cwd,status,evento,atualizado'
conferir 'agente claude' test "$(campo "$pasta" agente)" = claude
conferir 'session_id do evento' test "$(campo "$pasta" session_id)" = "$id"
conferir 'cwd do evento' test "$(campo "$pasta" cwd)" = /workspace/Faturamento
conferir 'atualizado em ISO 8601 com deslocamento' \
    grep -Eq '"atualizado":"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]+[+-][0-9]{2}:[0-9]{2}"' "$pasta/$id.json"
conferir 'so o status e o marcador na pasta' test "$(echo $(ls -A "$pasta"))" = "$id.criado $id.json"
conferir 'marcador vazio' test ! -s "$pasta/$id.criado"
touch -d '3 days ago' "$pasta/$id.criado"
antes=$(stat -c '%i %Y' "$pasta/$id.criado")
rodar_hook "$pasta" "$(evento Stop)"
conferir 'evento seguinte nao regrava o marcador' test "$(stat -c '%i %Y' "$pasta/$id.criado")" = "$antes"
pasta=$(nova_pasta)
mkdir "$pasta/$id.json"
rodar_hook "$pasta" "$(evento Stop)"
silencio 'destino impossivel de gravar'
conferir 'destino impossivel de gravar: sem temporarios' test "$(contar "$pasta")" -eq 2
pasta=$(nova_pasta)
rodar_hook "$pasta" "$(evento SessionStart)" codex
conferir 'agente codex' test "$(campo "$pasta" agente)" = codex
pasta=$(nova_pasta)
rodar_hook "$pasta" "$(evento SessionStart)" outro
silencio 'agente desconhecido'
conferir 'agente desconhecido nao grava' test "$(contar "$pasta")" -eq 0

grupo 'hook: evento desconhecido e notificacoes'
pasta=$(nova_pasta)
rodar_hook "$pasta" "$(evento PreCompact)"
conferir 'evento desconhecido nao cria arquivo' test "$(contar "$pasta")" -eq 0
rodar_hook "$pasta" "$(evento Stop)"
antes=$(texto "$pasta")
for nome in PreCompact toString; do
    rodar_hook "$pasta" "$(evento "$nome")"
    silencio "evento $nome"
    conferir "evento $nome nao altera o arquivo" test "$(texto "$pasta")" = "$antes"
done
for tipo in idle_prompt auth_success ''; do
    rodar_hook "$pasta" "$(evento Notification "$id" "{\"notification_type\":\"$tipo\"}")"
    conferir "Notification '$tipo' continua aguardando" test "$(texto "$pasta")" = "$antes"
done
for tipo in permission_prompt elicitation_dialog; do
    pasta=$(nova_pasta)
    rodar_hook "$pasta" "$(evento Notification "$id" "{\"notification_type\":\"$tipo\"}")"
    conferir "Notification $tipo grava atencao" test "$(campo "$pasta" status)" = atencao
done

grupo 'hook: ordem dos eventos e escrita atomica'
pasta=$(nova_pasta)
gravar_status "$pasta" aguardando Stop 3600000
antes=$(texto "$pasta")
rodar_hook "$pasta" "$(evento PostToolUse)"
silencio 'evento fora de ordem'
conferir 'evento mais antigo nao substitui status mais novo' test "$(texto "$pasta")" = "$antes"
gravar_status "$pasta" aguardando Stop -3600000
rodar_hook "$pasta" "$(evento UserPromptSubmit)"
conferir 'evento mais novo substitui status antigo' test "$(campo "$pasta" status)" = executando
echo '{{{ lixo' >"$pasta/$id.json"
rodar_hook "$pasta" "$(evento Stop)"
conferir 'arquivo anterior corrompido e substituido' test "$(campo "$pasta" status)" = aguardando
pasta=$(nova_pasta)
entrada=$(evento PostToolUse)
for _ in 1 2 3 4 5 6 7 8; do
    printf '%s' "$entrada" | node "$hook" claude "$pasta" &
done
wait
conferir 'oito eventos em paralelo: JSON completo' test "$(campo "$pasta" status)" = executando
conferir 'oito eventos em paralelo: status e marcador, sem temporarios' test "$(contar "$pasta")" -eq 2

grupo 'hook: entradas invalidas'
recusar_entrada() {
    local raiz pasta
    raiz=$(nova_pasta)
    pasta=$raiz/sessoes
    mkdir "$pasta"
    rodar_hook "$pasta" "$2"
    silencio "$1"
    conferir "$1: nenhum arquivo criado" test "$(contar "$pasta"),$(contar "$raiz")" = 0,1
}
recusar_entrada 'entrada que nao e JSON' 'isto nao e json'
recusar_entrada 'entrada vazia' ''
recusar_entrada 'entrada null' 'null'
recusar_entrada 'entrada que e uma lista' '[1,2,3]'
recusar_entrada 'session_id ausente' '{"hook_event_name":"Stop","cwd":"/workspace/x"}'
recusar_entrada 'session_id com barra' "$(evento Stop a/b)"
recusar_entrada 'session_id apontando para fora' "$(evento Stop ../fora)"
recusar_entrada 'session_id com barra invertida' "$(evento Stop 'a\b')"
recusar_entrada 'session_id com ponto' "$(evento Stop a.b)"
recusar_entrada 'session_id com quebra de linha no fim' "$(evento Stop $'abc\n')"
recusar_entrada 'session_id com 101 caracteres' "$(evento Stop "$(printf 'a%.0s' {1..101})")"
inexistente=$(nova_pasta)/nao-existe
rodar_hook "$inexistente" "$(evento Stop)"
silencio 'pasta de status ausente'
conferir 'pasta de status ausente: nada e criado' test ! -e "$inexistente"
pasta=$(nova_pasta)
rodar_hook "$pasta" "$(evento UserPromptSubmit "$id" '{"prompt":"SEGREDO-123","tool_input":{"command":"SEGREDO-456"}}')"
conferir 'prompt confidencial: status gravado' test "$(campo "$pasta" status)" = executando
conferir 'prompt confidencial: texto do evento fora do arquivo' \
    test -z "$(grep -E 'SEGREDO|conversa\.jsonl|transcript' "$pasta/$id.json")"

# ----- abrir.sh ------------------------------------------------------------

# claude e codex falsos, a frente no PATH: registram os argumentos e terminam com o codigo pedido.
falsos=$(nova_pasta)
cat >"$falsos/claude" <<'EOF'
#!/bin/bash
primeira=1
[ -f "$FAKE_LOG" ] && primeira=
[ -n "$primeira" ] && [ -n "$FAKE_ATRASO" ] && sleep "$FAKE_ATRASO"
echo "$*" >>"$FAKE_LOG"
echo "$PWD $(id -u)" >"$FAKE_LOG.info"
[ -n "$primeira" ] && exit "${FAKE_CODIGO:-0}"
exit "${FAKE_CODIGO2:-0}"
EOF
cp "$falsos/claude" "$falsos/codex"
chmod +x "$falsos/claude" "$falsos/codex"
PATH=$falsos:$PATH
if [[ $(command -v claude) != "$falsos/claude" || $(command -v codex) != "$falsos/codex" ]]; then
    echo 'FALHA  os agentes falsos nao ficaram a frente no PATH; nada do abrir.sh foi testado.'
    exit 1
fi

# rodar_abrir <argumentos...>; deixa em "codigo", "saida" e "chamadas" o resultado.
rodar_abrir() {
    export FAKE_LOG=$(nova_pasta)/chamadas
    saida=$("$abrir" "$@" 2>&1 </dev/null)
    codigo=$?
    chamadas=$(cat "$FAKE_LOG" 2>/dev/null)
}

# recusa <nome> <codigo esperado> <argumentos...>
recusa() {
    local nome=$1 esperado=$2
    shift 2
    rodar_abrir "$@"
    conferir "$nome: codigo $esperado" test "$codigo" -eq "$esperado"
    conferir "$nome: nenhum agente iniciado" test ! -e "$FAKE_LOG"
}

grupo 'abrir: validacao do repositorio'
for repo in .. . ... 'a b' 'a;b' 'a&b' 'a|b' 'a\b' a/b ../etc $'a\n' 'Produção' ''; do
    recusa "--repo '$repo'" 2 --agente codex --repo "$repo" --modo novo
done
recusa 'sem --repo' 2 --agente codex --modo novo
recusa 'argumento solto' 2 --agente codex --repo a b --modo novo
recusa 'parametro desconhecido' 2 --agente codex --repo a --caminho x
recusa 'parametro sem valor' 2 --agente codex --repo

grupo 'abrir: validacao do caminho'
b64() { printf '%s' "$1" | base64 -w0; }
for caminho in workspace/x / /home/node /workspacex /openspec /openspec/ERPx /workspace/ /workspace//x \
    /workspace/../etc /workspace/./x /openspec/ERP/.. $'/workspace/a\nb'; do
    recusa "--caminho '$caminho'" 2 --agente codex --caminho "$(b64 "$caminho")" --modo novo
done
recusa '--caminho fora de Base64' 2 --agente codex --caminho 'nao-e-base64!' --modo novo
recusa '--repo e --caminho juntos' 2 --agente codex --repo Faturamento --caminho "$(b64 /workspace/x)" --modo novo
recusa '--caminho inexistente' 3 --agente codex --caminho "$(b64 /openspec/ERP/painel-verificar-inexistente)" --modo novo

grupo 'abrir: validacao da sessao'
for sessao in ------------------------------------ zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz \
    0b6f1c2e5-d1a-4c8e-9f3a-2b7d4e6a8c10 0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c1; do
    recusa "--sessao $sessao" 2 --agente claude --repo Faturamento --sessao "$sessao" --modo retomar
done
recusa 'claude novo sem --sessao' 2 --agente claude --repo Faturamento --modo novo
recusa 'claude retomar sem --sessao' 2 --agente claude --repo Faturamento --modo retomar
recusa 'codex retomar sem --sessao' 2 --agente codex --repo Faturamento --modo retomar

grupo 'abrir: validacao do agente e do modo'
recusa 'agente desconhecido' 2 --agente outro --repo Faturamento --sessao "$id" --modo novo
recusa 'agente com maiuscula' 2 --agente Claude --repo Faturamento --sessao "$id" --modo novo
recusa 'sem --agente' 2 --repo Faturamento --sessao "$id" --modo novo
recusa 'modo desconhecido' 2 --agente claude --repo Faturamento --sessao "$id" --modo apagar
recusa 'sem parametros' 2

grupo 'abrir: repositorio inexistente'
recusa 'repositorio inexistente' 3 --agente codex --repo painel-verificar-inexistente --modo novo
conferir 'repositorio inexistente: mensagem' grep -q 'nao encontrado' <<<"$saida"

# A raiz e fixa no abrir.sh, entao os casos abaixo usam uma pasta existente em /workspace.
grupo 'abrir: inicio do agente'
repo=
for pasta in /workspace/*/; do
    nome=$(basename "$pasta")
    if [[ -d $pasta && ! -L ${pasta%/} && $nome =~ ^[A-Za-z0-9_.-]+$ ]]; then
        repo=$nome
        break
    fi
done
if [[ -z $repo ]]; then
    echo '  AVISO  sem pasta valida em /workspace: casos de inicio do agente pulados'
else
    # PATH so com o que o abrir.sh usa antes de procurar o agente.
    mkdir "$base/sem-agentes" && ln -s "$(command -v realpath)" "$base/sem-agentes/"
    PATH=$base/sem-agentes "$abrir" --agente codex --repo "$repo" --modo novo >/dev/null 2>&1 </dev/null
    conferir 'agente ausente: codigo 4' test $? -eq 4

    rodar_abrir --agente claude --repo "$repo" --sessao "$id" --modo novo
    conferir 'claude novo: --session-id' test "$chamadas" = "--session-id $id"
    conferir 'claude novo: pasta do repositorio e usuario sem privilegios' \
        test "$(cat "$FAKE_LOG.info")" = "/workspace/$repo $(id -u)" -a "$(id -u)" -ne 0
    for local in "/workspace/$repo" /workspace /openspec/ERP; do
        rodar_abrir --agente codex --caminho "$(b64 "$local")" --modo novo
        conferir "--caminho $local: agente iniciado na pasta" test "$codigo $(cat "$FAKE_LOG.info" 2>/dev/null)" = "0 $local $(id -u)"
    done
    rodar_abrir --agente codex --repo "$repo" --modo novo
    conferir 'codex novo: sem argumentos' test -e "$FAKE_LOG" -a -z "$chamadas"
    rodar_abrir --agente codex --repo "$repo" --sessao "$id" --modo retomar
    conferir 'codex retomar: resume' test "$chamadas" = "resume $id"

    export FAKE_CODIGO=0 FAKE_CODIGO2=0 FAKE_ATRASO=
    rodar_abrir --agente claude --repo "$repo" --sessao "$id" --modo retomar
    conferir 'claude retomar com sucesso: uma chamada' test "$codigo,$chamadas" = "0,--resume $id"

    FAKE_CODIGO=1
    rodar_abrir --agente claude --repo "$repo" --sessao "$id" --modo retomar
    conferir 'claude retomar falha logo: segunda chamada com --session-id' \
        test "$chamadas" = "--resume $id"$'\n'"--session-id $id"
    conferir 'claude retomar falha logo: codigo 0 da segunda' test "$codigo" -eq 0
    conferir 'claude retomar falha logo: avisa' grep -q 'nao tem mensagens' <<<"$saida"
    FAKE_CODIGO2=7
    rodar_abrir --agente claude --repo "$repo" --sessao "$id" --modo retomar
    conferir 'claude retomar falha logo: codigo 7 da segunda' test "$codigo" -eq 7

    FAKE_CODIGO=5 FAKE_ATRASO=31
    rodar_abrir --agente claude --repo "$repo" --sessao "$id" --modo retomar
    conferir 'claude retomar falha depois de 30 s: sem segunda chamada' test "$codigo,$chamadas" = "5,--resume $id"

    FAKE_CODIGO=1 FAKE_ATRASO=
    rodar_abrir --agente codex --repo "$repo" --sessao "$id" --modo retomar
    conferir 'codex retomar falha: sem segunda chamada' test "$codigo,$chamadas" = "1,resume $id"
fi

printf '\nCasos: %s   Falhas: %s\n' "$total" "$falhas"
((falhas == 0))
