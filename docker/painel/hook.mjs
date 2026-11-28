// Hook de status do DataPilot no container.
//
//   node /opt/painel/hook.mjs <claude|codex> [pasta de status]
//
// Le o evento de hook pela entrada padrao (JSON em UTF-8) e grava <pasta>/<session_id>.json.
//
// Regras que nao podem ser quebradas:
//   - nunca escrever na saida padrao (em SessionStart e UserPromptSubmit ela vira contexto do modelo);
//   - sempre terminar com codigo 0 (codigo 2 bloqueia a acao do agente);
//   - gravar somente os seis campos do contrato, nada do conteudo do evento.
import { readFileSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { setTimeout as esperar } from 'node:timers/promises';

// Instante do evento: usado em "atualizado" e para nao regredir um status mais novo.
const agora = new Date();

const STATUS = new Map([
    ['SessionStart', 'ativa'],
    ['UserPromptSubmit', 'executando'],
    ['PostToolUse', 'executando'],
    ['PermissionRequest', 'atencao'],
    ['Stop', 'aguardando'],
    ['SessionEnd', 'encerrada'],
]);
// So pedidos de permissao e perguntas; o aviso de ociosidade (idle_prompt) e ignorado.
const NOTIFICACOES = ['permission_prompt', 'elicitation_dialog'];

// ISO 8601 com o deslocamento local.
function isoLocal(data) {
    const desvio = -data.getTimezoneOffset();
    const local = new Date(data.getTime() + desvio * 60000).toISOString().slice(0, -1);
    const dois = (n) => String(Math.trunc(n)).padStart(2, '0');
    return `${local}${desvio < 0 ? '-' : '+'}${dois(Math.abs(desvio) / 60)}:${dois(Math.abs(desvio) % 60)}`;
}

async function registrar(agente = 'claude', pasta = '/status') {
    if (agente !== 'claude' && agente !== 'codex') return;

    process.stdin.setEncoding('utf8');
    let texto = '';
    for await (const parte of process.stdin) texto += parte;
    const ev = JSON.parse(texto);

    // O identificador vira nome de arquivo: so letras, digitos, "_" e "-".
    const id = ev?.session_id;
    if (typeof id !== 'string' || !/^[A-Za-z0-9_-]{1,100}$/.test(id)) return;

    const evento = ev.hook_event_name;
    const status = evento === 'Notification'
        ? NOTIFICACOES.includes(ev.notification_type) && 'atencao'
        : STATUS.get(evento);
    if (!status) return;

    if (!statSync(pasta).isDirectory()) return;
    const destino = join(pasta, `${id}.json`);

    // Eventos assincronos podem terminar fora de ordem: nao substituir um status mais novo.
    try {
        if (Date.parse(JSON.parse(readFileSync(destino, 'utf8')).atualizado) > agora.getTime()) return;
    } catch {
        // Sem arquivo anterior, ou ilegivel: segue e grava.
    }

    const json = JSON.stringify({
        agente,
        session_id: id,
        cwd: String(ev.cwd ?? ''),
        status,
        evento,
        atualizado: isoLocal(agora),
    });

    // A montagem do container nao preserva a data de criacao na troca do arquivo. O marcador, criado
    // uma unica vez e antes do status, guarda a data de inicio da sessao; sem ele, nada e gravado.
    try {
        writeFileSync(join(pasta, `${id}.criado`), '', { flag: 'wx' });
    } catch (erro) {
        if (erro.code !== 'EEXIST') throw erro;
    }

    // Escrita atomica: temporario unico por processo na mesma pasta e troca sobre o destino. Um leitor
    // no Windows pode fazer a troca falhar por um instante; por isso as novas tentativas.
    const tmp = join(pasta, `${id}.${process.pid}.tmp`);
    try {
        writeFileSync(tmp, json);
        for (let i = 0; i < 10; i++) {
            try {
                renameSync(tmp, destino);
                break;
            } catch {
                await esperar(20);
            }
        }
    } finally {
        rmSync(tmp, { force: true });
    }
}

// Nenhuma falha pode chegar ao agente.
await registrar(...process.argv.slice(2)).catch(() => {});
