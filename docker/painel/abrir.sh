#!/bin/bash
# Abre ou retoma uma sessao do Claude Code ou do Codex num repositorio, dentro do container:
#
#   docker exec --interactive --tty datapilot-agente /opt/painel/abrir.sh
#       --agente claude --repo Faturamento --sessao <guid> --modo novo
#
# --repo e uma pasta direta da raiz dos fontes. Para as demais pastas dos locais montados, use
# --caminho com o caminho no container codificado em Base64 (UTF-8).
#
# Todos os parametros sao validados antes de qualquer acao.
#
# Codigos de saida proprios: 2 = parametro invalido; 3 = repositorio nao encontrado;
# 4 = comando do agente nao encontrado. Nos demais casos, o codigo e o do proprio agente.

# Raiz dos fontes e segundo local no container. Nunca vem por parametro.
raiz=/workspace
openspec=/openspec/ERP

recusar() {
    echo "$2"
    exit "$1"
}

validar() {
    local LC_ALL=C # as faixas A-Za-z valem so para ASCII
    local guid='^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$'

    [[ $agente == claude || $agente == codex ]] ||
        recusar 2 'Parametro --agente invalido: use claude ou codex.'
    [[ $modo == novo || $modo == retomar ]] ||
        recusar 2 'Parametro --modo invalido: use novo ou retomar.'
    [[ (-n $repo && -z $caminho) || (-z $repo && -n $caminho) ]] ||
        recusar 2 'Informe --repo ou --caminho (um dos dois).'
    if [[ -n $repo ]]; then
        # Nome de pasta: so letras, digitos, "_", "." e "-", e nunca formado apenas por pontos.
        [[ $repo =~ ^[A-Za-z0-9_.-]+$ && ! $repo =~ ^\.+$ ]] ||
            recusar 2 'Parametro --repo invalido: use apenas letras, digitos, "_", "." e "-".'
    else
        # Dentro de um dos locais, ja normalizado: sem trechos "." e "..", "//" nem barra no fim.
        destino=$(base64 -d <<<"$caminho" 2>/dev/null) &&
            [[ $destino/ == "$raiz"/* || $destino/ == "$openspec"/* ]] &&
            [[ $destino != *[[:cntrl:]]* && ! $destino/ =~ //|/\.\.?/ ]] ||
            recusar 2 "Parametro --caminho invalido: use, em Base64, uma pasta de $raiz ou de $openspec."
    fi
    [[ -z $sessao || $sessao =~ $guid ]] ||
        recusar 2 'Parametro --sessao invalido: use um GUID no formato 00000000-0000-0000-0000-000000000000.'
    # O Claude sempre recebe o identificador; o Codex so na retomada.
    [[ -n $sessao || ($modo == novo && $agente == codex) ]] ||
        recusar 2 'Parametro --sessao obrigatorio para esta combinacao de agente e modo.'
}

agente='' repo='' caminho='' destino='' sessao='' modo=novo
while (($#)); do
    (($# >= 2)) || recusar 2 'Parametro sem valor ou desconhecido.'
    case $1 in
        --agente) agente=$2 ;;
        --repo) repo=$2 ;;
        --caminho) caminho=$2 ;;
        --sessao) sessao=$2 ;;
        --modo) modo=$2 ;;
        *) recusar 2 'Parametro desconhecido: use --agente, --repo, --caminho, --sessao e --modo.' ;;
    esac
    shift 2
done
validar

# A pasta precisa existir e nao pode ser um atalho para outro lugar, nem passar por um.
[[ -n $repo ]] && destino=$raiz/$repo
[[ -d $destino && $(realpath -- "$destino") == "$destino" ]] && cd "$destino" ||
    recusar 3 "Repositorio nao encontrado: $destino"

command -v "$agente" >/dev/null || recusar 4 "Comando nao encontrado: $agente"

# "exec" entrega o terminal e os sinais ao agente.
case $agente-$modo in
    claude-novo) exec claude --session-id "$sessao" ;;
    codex-novo) exec codex ;;
    codex-retomar) exec codex resume "$sessao" ;;
esac

# Retomada do Claude: sem "exec", porque pode haver uma segunda tentativa. O Claude so grava a
# conversa depois da primeira mensagem; se a retomada falha logo, a sessao provavelmente foi
# fechada antes disso, entao inicia como sessao nova, com o mesmo identificador.
inicio=$SECONDS
claude --resume "$sessao"
codigo=$?
if ((codigo != 0 && SECONDS - inicio < 30)); then
    echo 'A conversa ainda nao tem mensagens. Iniciando a sessao com o mesmo identificador.'
    exec claude --session-id "$sessao"
fi
exit "$codigo"
