# Design

## Context

Motivação em proposal.md. Hoje:
- `Constantes` fixa `PastaStatusPadrao`, `DestinoSsh = "ia@localhost"` e `ScriptAbrir`.
- `MontadorComando.Montar` sempre inclui `ssh -t ia@localhost`.
- `App.OnStartup` resolve a pasta de status por `--pasta-status` ou pelo padrão e não a cria. Sem a pasta, o `LeitorPasta` mostra "pasta indisponível".
- `AberturaSessao` já chama `DadosPainel.Carregar()` a cada abertura e retomada.
- `DadosPainel` grava o `demandas.json` de forma atômica, preserva campos desconhecidos e não grava quando o arquivo está ilegível.

## Goals / Non-Goals

**Goals:**
- Configurar o uso do SSH e o usuário do SSH sem recompilar.
- Mostrar a pasta de status em uso e criá-la ao abrir o painel.
- Nenhum valor configurado entra num comando sem validação.

**Non-Goals:**
- Configurar a pasta de status: ela continua sendo a padrão ou a do `--pasta-status`.
- Configurar o host do SSH, que continua `localhost`.
- Configurar a pasta do arquivo de dados, a raiz dos fontes, o caminho do `abrir.ps1` ou o hook do `ia`.
- Mostrar aviso na tela sobre o modo sem SSH, por decisão do usuário.

## Decisions

### 1. Configurações dentro do `demandas.json`

Uma seção opcional `"configuracoes": { "usarSsh", "usuarioSsh" }` no arquivo de dados. Ela é lida em `Carregar()`, ficando disponível no `EstadoDados`, e gravada por um método novo, `DadosPainel.SalvarConfiguracoes`, que usa o mesmo `Alterar` dos trabalhos.

Por quê: a leitura, a validação, a gravação atômica, a proteção do arquivo ilegível e a pasta fora do alcance do `ia` já existem nesse arquivo.

Alternativa considerada: um `configuracoes.json` separado. Rejeitada por duplicar a leitura e a gravação seguras.

### 2. Validação do usuário em `ValidadorEntrada`

- **Regra:** `UsuarioSshValido` aceita `^[A-Za-z0-9_][A-Za-z0-9_.-]{0,19}$`. Começar por letra, dígito ou `_` impede que o valor vire uma opção do `ssh`, como `-oProxyCommand`.
- **Onde roda:** ao salvar no diálogo, ao carregar do arquivo (valor inválido vale o padrão) e de novo no `MontadorComando`, como já acontece com a pasta e o identificador.
- **SSH desligado:** com "Abrir pelo SSH" desligado, o campo fica desabilitado e o usuário gravado continua o mesmo.

### 3. Comando com e sem SSH

`MontadorComando` recebe as opções de execução (usar SSH e usuário), que vêm do `EstadoDados` carregado pela `AberturaSessao`. Com SSH, o comando inclui `ssh`, `-t` e `<usuário>@localhost`. Sem SSH, o `powershell` vem logo depois de `--suppressApplicationTitle`. `Constantes.DestinoSsh` dá lugar a `UsuarioSshPadrao = "ia"` e `HostSsh = "localhost"`.

Como a `AberturaSessao` recarrega o arquivo a cada operação, a mudança vale na próxima sessão sem mecanismo extra.

### 4. Pasta de status criada ao abrir

Em `App.OnStartup`, depois de resolver a pasta como hoje, o painel chama `Directory.CreateDirectory` dentro de um `try`. Se falhar, segue sem a pasta, e o `LeitorPasta` mostra o aviso de pasta indisponível que já existe. `CreateDirectory` não faz nada se a pasta já existir, então não é preciso testar antes.

### 5. Diálogo `ConfiguracoesWindow`

- **Campos:** um `TextBox` com `IsReadOnly` para a pasta de status (pode ser selecionada e copiada), "Abrir pelo SSH" e o usuário do SSH.
- **Comportamento:** segue os outros diálogos, com estilo `EstiloDialogo`, erro junto do campo, Enter salvando e Esc cancelando.
- **Acesso:** a ação "Configurações" fica na barra superior, com ícone de engrenagem e nome acessível.
- **Interface:** `IInteracao` ganha `DialogoConfiguracoes(pastaStatus, atual, aplicar)`, no padrão dos outros diálogos, em que `aplicar` grava e devolve a mensagem de recusa.

## Risks / Trade-offs

- **[O modo sem SSH dá ao agente os acessos de quem usa o painel]** → É decisão do usuário: fica desligado por padrão e sem aviso na tela. O PRD e a RS-7 são atualizados para registrar o risco.
- **[A pasta de status passa a pertencer ao usuário principal, que poderia gravar nela]** → Isso acontece quando é o painel que cria a pasta. A pasta instalada pelo guia não tem esse problema. O painel nunca grava dentro dela, e o guia de instalação orienta a aplicar as permissões da instalação depois.
- **[A criação falha quando `C:\ProgramData\Painel` já existe com as permissões da instalação (usuário principal só lê)]** → Nesse caso a instalação já criou `sessoes`, e a falha só cai no aviso de pasta indisponível existente.
- **[Conflito com a change `refatorar-estrutura`, que mexe em `MontadorComando`, `AberturaSessao` e `App`]** → Implementar esta change depois dela.

## Migration Plan

Não há nada a migrar. Arquivos de dados sem `configuracoes` usam os padrões, que são os valores fixos de hoje.
