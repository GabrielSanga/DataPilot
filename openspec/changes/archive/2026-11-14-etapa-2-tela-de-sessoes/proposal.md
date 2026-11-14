# Proposal

## Why

Os hooks da Etapa 1 já gravam o status de cada sessão, mas hoje a única forma de vê-lo é rodar um comando no terminal. A Etapa 2 entrega a primeira versão usável do painel: uma janela que mostra todas as sessões do Claude Code e do Codex e o que cada uma está fazendo, atualizada sozinha. É também onde nasce a solution .NET sobre a qual as etapas seguintes constroem.

## What Changes

- Nova solution .NET 10 com três projetos: `DataPilot.Core` (biblioteca sem dependência de interface), `DataPilot.Wpf` (aplicativo WPF) e `DataPilot.Core.Tests` (xUnit).
- Leitura da pasta de status `C:\ProgramData\Painel\sessoes`, somente leitura, com limite de tamanho por arquivo, limite de quantidade de arquivos e descarte silencioso do que for inválido.
- Atualização da tela em até 5 segundos depois de cada evento, combinando aviso de mudança na pasta com releitura periódica.
- Janela com a lista de sessões: agente, pasta, status com cor e tempo desde a última atualização, ordenada pelo que precisa de atenção primeiro, com um resumo no cabeçalho.
- Regra de inatividade calculada pelo painel, com dois prazos: 15 minutos para sessões `ativa` ou `executando` e 8 horas para `aguardando` ou `atencao`.
- Sessões encerradas ou inativas há mais de 3 dias ficam ocultas por padrão, com uma opção para mostrá-las.

Desvios em relação ao PRD, já decididos:

- .NET 10 no lugar de .NET 8: é o que está instalado na máquina, e o .NET 8 saiu de suporte.
- Projetos chamados `DataPilot.*` no lugar de `Painel.*`. Os caminhos da máquina (`C:\Painel`, `C:\ProgramData\Painel`) não mudam.
- Inatividade também para `aguardando` e `atencao`: o Codex não grava `encerrada` quando a aba é fechada, e sem prazo essas sessões nunca sairiam da tela.

Fora desta change: botões de abrir e retomar (Etapa 3), demandas e vínculo de sessões, inclusive o vínculo entre a sessão antiga e a nova depois de um `/clear` (Etapa 4), progresso do OpenSpec, ícone na bandeja e notificações (Etapa 5), e qualquer tela de configuração.

## Capabilities

### New Capabilities

- `leitura-de-status`: leitura segura e atualizada dos arquivos de status gravados pelo usuário `ia` — o que é aceito, o que é descartado, os limites e a garantia de que esses dados só são exibidos.
- `lista-de-sessoes`: a tela de sessões — o que mostra de cada sessão, o status exibido com a regra de inatividade, cores, ordem, resumo e filtro de sessões antigas.

### Modified Capabilities

Nenhuma. O contrato do arquivo de status, definido em `status-de-sessao` pela change `etapa-1-scripts`, é consumido como está.

## Impact

- **Repositório:** novos `DataPilot.slnx`, `src/DataPilot.Core`, `src/DataPilot.Wpf` e `tests/DataPilot.Core.Tests`; instruções de compilação e execução no `README.md`.
- **Dependências:** SDK do .NET 10 e os pacotes NuGet `CommunityToolkit.Mvvm`, `xunit`, `xunit.runner.visualstudio` e `Microsoft.NET.Test.Sdk`.
- **Máquina:** o painel roda como o usuário principal e apenas lê `C:\ProgramData\Painel\sessoes`. Nada é instalado nem alterado fora do repositório.
- **Segurança:** implementa o lado do painel de RS-1 (dados de status só são exibidos), RS-4 (o painel nunca escreve na pasta de status) e RS-5 (limite de tamanho e descarte de inválidos).
- **Scripts da Etapa 1:** sem alteração. O painel abre os arquivos sem bloquear a troca atômica feita pelo `hook.ps1`.
