#!/bin/sh
# Mantem os hooks do Codex iguais ao modelo da imagem. So regrava quando o conteudo muda, porque a
# confianca dada em /hooks e ligada ao conteudo.
modelo=/opt/painel/codex-hooks.json
destino=/home/node/.codex/hooks.json

cmp -s "$modelo" "$destino" || install -m 0644 "$modelo" "$destino" ||
    echo "Aviso: nao foi possivel gravar $destino; o status do Codex nao sera registrado." >&2

exec "$@"
