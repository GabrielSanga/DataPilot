# Ambiente dos agentes do DataPilot

# SO Linux Debian Bookworm + Node.js 22
FROM node:22-bookworm

# Instalação do Claude Code e Codex
ARG CLAUDE_CODE_VERSION=2.1.332
ARG CODEX_VERSION=0.172.0

# Instalação de dependências do sistema, Claude Code e Codex
RUN apt-get update \
 && apt-get install -y --no-install-recommends git ripgrep ca-certificates \
 && rm -rf /var/lib/apt/lists/* \
 && npm install -g "@anthropic-ai/claude-code@${CLAUDE_CODE_VERSION}" "@openai/codex@${CODEX_VERSION}" \
 && npm cache clean --force

# Na montagem do Windows os arquivos aparecem com outro dono, e o git recusaria os repositorios.
RUN git config --system --add safe.directory '*'

# Scripts do painel: dono root, so leitura e execucao para o usuario dos agentes.
COPY entrypoint.sh painel/abrir.sh painel/hook.mjs painel/codex-hooks.json /opt/painel/
COPY painel/claude-managed-settings.json /etc/claude-code/managed-settings.json
RUN chown -R root:root /opt/painel /etc/claude-code \
 && chmod 0755 /opt/painel /etc/claude-code \
 && chmod 0555 /opt/painel/* \
 && chmod 0444 /etc/claude-code/managed-settings.json \
 && install -d -o node -g node /home/node/.claude /home/node/.codex

USER node
WORKDIR /workspace
ENTRYPOINT ["/opt/painel/entrypoint.sh"]
CMD ["sleep", "infinity"]
