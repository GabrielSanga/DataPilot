# Design

## Context

Motivação em proposal.md. Hoje:
- `Constantes` fixa `PastaStatusPadrao`, `DestinoSsh = "ia@localhost"` e `ScriptAbrir`.
- `MontadorComando.Montar` sempre inclui `ssh -t ia@localhost`.
- `App.OnStartup` resolve a pasta de status só por `--pasta-status` ou pelo padrão.
- `AberturaSessao` já chama `DadosPainel.Carregar()` a cada abertura e retomada.
- `DadosPainel` grava o `demandas.json` de forma atômica, preserva campos desconhecidos e não grava quando o arquivo está ilegível.
- O `hook.ps1` do `ia` recebe a pasta de status por parâmetro (`-PastaStatus`, padrão `C:\ProgramData\Painel\sessoes`) e o `abrir.ps1` fixa `C:\Fontes`. Nenhum dos dois é alterado por esta change.

## Goals / Non-Goals

**Goals:**
- Configurar a pasta de status, o uso do SSH e o usuário do SSH sem recompilar.
- Nenhum valor configurado entra num comando sem validação.

**Non-Goals:**
- Host do SSH configurável. O host continua `localhost`.
- Configurar a pasta do arquivo de dados, que já é criada na primeira gravação.
- Configurar a raiz dos fontes, o caminho do `abrir.ps1` ou o hook do `ia`.
- Aplicar a nova pasta de status sem reabrir o painel.
- Aviso na tela sobre o modo sem SSH (decisão do usuário).

## Decisions

### 1. Configurações dentro do `demandas.json`

Uma seção opcional `"configuracoes": { "pastaStatus", "usarSsh", "usuarioSsh" }` no arquivo de dados, lida em `Carregar()` (ficando disponível no `EstadoDados`) e gravada por um método novo, `DadosPainel.SalvarConfiguracoes`, que usa o mesmo `Alterar` dos trabalhos.

Por quê: a leitura, a validação, a gravação atômica, a proteção do arquivo ilegível e a pasta fora do alcance do `ia` já existem nesse arquivo. Um arquivo novo repetiria tudo isso.

Alternativa considerada: um `configuracoes.json` separado. Rejeitada por duplicar a leitura e a gravação seguras.

### 2. Validação em `ValidadorEntrada`

- `UsuarioSshValido`: `^[A-Za-z0-9_][A-Za-z0-9_.-]{0,19}$`. Começar por letra, dígito ou `_` impede que o valor vire uma opção do `ssh`, como `-oProxyCommand`.
- `PastaStatusValida`: caminho com unidade local (`Path.IsPathFullyQualified` e letra de unidade, sem UNC), sem `Path.GetInvalidPathChars()`, com até 260 caracteres.

Os dois validadores rodam em três momentos: ao salvar no diálogo, ao carregar do arquivo (valor inválido vale o padrão) e, no caso do usuário, de novo no `MontadorComando`, como já acontece com a pasta e o identificador.

### 3. Comando com e sem SSH

`MontadorComando` recebe as opções de execução (usar SSH e usuário), que vêm do `EstadoDados` carregado pela `AberturaSessao`. Com SSH, o comando inclui `ssh`, `-t` e `<usuário>@localhost`. Sem SSH, o `powershell` vem logo depois de `--suppressApplicationTitle`. `Constantes.DestinoSsh` dá lugar a `UsuarioSshPadrao = "ia"` e `HostSsh = "localhost"`.

Como a `AberturaSessao` recarrega o arquivo a cada operação, a mudança no SSH vale na próxima sessão sem nenhum mecanismo extra.

### 4. Pasta de status resolvida e criada ao abrir

Em `App.OnStartup`, a ordem é `--pasta-status`, depois a configuração, depois o padrão. Se a pasta não existir, o painel chama `Directory.CreateDirectory` dentro de um `try`. Se falhar, segue sem a pasta, e o `LeitorPasta` mostra o aviso de pasta indisponível que já existe. O `MainViewModel` recebe a pasta resolvida, como hoje.

Trocar a pasta exige reabrir o painel. Aplicar na hora exigiria recriar o `MonitorPasta` e limpar a tela; o ganho não justifica.

### 5. Diálogo `ConfiguracoesWindow`

O diálogo segue os outros: estilo `EstiloDialogo`, erro junto do campo, Enter salva e Esc cancela. A escolha de pasta usa a mesma `OpenFolderDialog` de `InteracaoJanelas`. A ação "Configurações" fica na barra superior, com ícone de engrenagem e nome acessível. A interface `IInteracao` ganha `DialogoConfiguracoes(atual, aplicar)`, no mesmo padrão dos outros diálogos, em que `aplicar` grava e devolve a mensagem de recusa.

## Risks / Trade-offs

- [O modo sem SSH dá ao agente os acessos de quem usa o painel] → É decisão do usuário: fica desligado por padrão e sem aviso na tela. O PRD e a RS-7 são atualizados para registrar o risco.
- [A pasta configurada difere da pasta em que o hook do `ia` grava, e o painel fica vazio] → O guia de instalação passa a dizer que o `-PastaStatus` do hook precisa ser o mesmo caminho.
- [A pasta criada pelo painel pertence ao usuário principal, que poderia gravar nela, ao contrário da pasta padrão instalada] → O painel nunca grava dentro dela. O guia de instalação orienta a aplicar à pasta nova as mesmas permissões da padrão.
- [Uma pasta sem permissão de gravação para o `ia` impede os hooks de gravar] → É o mesmo item do guia de instalação. O painel não altera permissões.
- [Conflito com a change `refatorar-estrutura`, que mexe em `MontadorComando`, `AberturaSessao` e `App`] → Implementar esta change depois dela.

## Migration Plan

Nada a migrar. Arquivos de dados sem `configuracoes` usam os padrões, que são os valores fixos de hoje. Para voltar ao comportamento anterior, basta não salvar configurações ou restaurar os padrões no diálogo.
