# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver `specs/leitura-de-status/spec.md` e `specs/lista-de-sessoes/spec.md`.

O que molda a solução:

- O painel roda como o usuário principal, que tem acesso aos servidores da empresa, e lê arquivos escritos pelo usuário `ia`. Todo o conteúdo desses arquivos é tratado como não confiável.
- O contrato do arquivo de status vem da Etapa 1: um JSON por sessão, com seis campos, em UTF-8 sem BOM, trocado de forma atômica pelo `hook.ps1`.
- Resultados do roteiro manual da Etapa 1 (`docs/instalacao-etapa-1.md`):
  - `/clear` no Claude cria uma sessão nova, com outro arquivo.
  - Uma sessão retomada do Claude continua no mesmo arquivo.
  - O Codex não grava `encerrada` quando a aba é fechada.
  - Um leitor que abra o arquivo sem compartilhar exclusão faz a troca do hook falhar.
- A máquina tem apenas o SDK e o runtime de desktop do .NET 10.
- Não há código .NET no repositório; esta change cria a estrutura que as Etapas 3 a 5 vão estender.

## Goals / Non-Goals

**Goals:**

- Toda regra que dá para testar sem tela fica no `DataPilot.Core`, coberta por testes.
- Nenhum arquivo malformado, enorme ou em excesso consegue travar ou derrubar o painel.
- O painel nunca atrapalha o hook: só lê, e lê sem bloquear a escrita.
- A janela mostra o estado certo em até 5 segundos, mesmo se um aviso do sistema de arquivos for perdido.

**Non-Goals:**

- Qualquer ação sobre as sessões (abrir, retomar, vincular).
- Persistência do lado do painel: nesta etapa ele não grava nada, nem preferências.
- Tela de configuração; os prazos e limites são constantes.
- Testes automatizados da interface WPF.

## Decisions

### 1. Três projetos, com o Core sem dependência de interface

```
DataPilot.slnx
src/DataPilot.Core/          net10.0            modelo, leitura, monitor, regras de exibição
src/DataPilot.Wpf/           net10.0-windows    janela, view models, cores
tests/DataPilot.Core.Tests/  net10.0            xUnit
```

- Por quê: o PRD pede o Core separado para permitir trocar a interface no futuro. Com `net10.0` puro, o Core não consegue referenciar WPF nem por engano.
- Os nomes do domínio no código ficam em português (`Sessao`, `StatusSessao`), iguais aos termos do contrato e das specs.
- O arquivo de solution usa o formato que o SDK 10 gera por padrão.

### 2. Validação manual com `JsonDocument`

O arquivo é lido com `JsonDocument` (profundidade máxima pequena) e os campos são extraídos e validados um a um. Nada é desserializado por reflexão para tipos do painel.

- Por quê: o conteúdo é não confiável, e a spec exige rejeitar o arquivo inteiro em vários casos (status desconhecido, identificador diferente do nome do arquivo). Com extração manual, cada regra é uma linha visível e testável.
- Alternativa descartada: `JsonSerializer.Deserialize<T>` com conversores. Menos código, mas a validação fica espalhada entre atributos e conversores.

### 3. Leitura limitada, compartilhada e sem seguir atalhos

Cada arquivo é aberto só para leitura, compartilhando leitura, escrita e exclusão, e são lidos no máximo 16 KB mais um byte: se vier o byte extra, o arquivo é descartado. Arquivos que sejam pontos de nova análise (links simbólicos, junções) são ignorados. A enumeração considera os 500 `.json` modificados mais recentemente.

- Por quê: compartilhar exclusão é o que permite ao `hook.ps1` trocar o arquivo durante a leitura. Ler com teto evita depender do tamanho informado pelo sistema. Os limites impedem que a conta `ia` degrade o painel com arquivos grandes ou numerosos.
- Um cache por nome de arquivo (data de modificação e tamanho) evita reler o que não mudou.

### 4. Releitura completa, disparada por aviso e por relógio

O monitor produz sempre um retrato completo da pasta. Um `FileSystemWatcher` agenda uma releitura com pequena espera para agrupar eventos, e um temporizador força uma releitura a cada 5 segundos. As releituras nunca se sobrepõem.

- Por quê: o aviso dá resposta quase imediata; o temporizador garante o prazo de 5 segundos quando o aviso se perde (estouro de buffer, pasta recriada). Um retrato completo é mais simples e mais difícil de errar do que aplicar eventos um a um, e o volume é pequeno.
- O mesmo temporizador reavalia a inatividade e o texto de tempo decorrido, mesmo sem mudança nos arquivos.
- Pasta inexistente ou ilegível produz um retrato vazio marcado como "indisponível"; o temporizador continua tentando.

### 5. Regras de exibição como funções puras

Status exibido, ordem, filtro de antigas, resumo do cabeçalho, nome da pasta e limpeza de texto são funções puras no Core. O relógio entra por `TimeProvider`.

- Por quê: são as regras com mais casos de borda (limites de 15 minutos, 8 horas e 3 dias, data no futuro) e as que mais vão mudar. Puras, são testadas sem pasta, sem tela e sem esperar o tempo passar.
- `atencao` segue o prazo de 8 horas, como `aguardando`: um pedido de permissão do Codex cuja aba foi fechada também nunca seria encerrado.
- `ativa` ganha a cor verde. O PRD define quatro cores e não cita esse status.
- Os prazos e limites ficam num único tipo de constantes no Core.

### 6. Janela única com lista atualizada no lugar

O `DataPilot.Wpf` tem uma janela com cabeçalho, opção "mostrar antigas" e a lista. O view model recebe cada retrato, aplica as funções do Core e atualiza a coleção existente por identificador de sessão, em vez de recriá-la.

- Por quê: atualizar no lugar preserva seleção e rolagem a cada 5 segundos. View models ficam no projeto WPF com `CommunityToolkit.Mvvm`; como a lógica está no Core, eles só fazem a ligação com a tela.
- Os retratos chegam fora da thread da interface e são entregues a ela pelo `Dispatcher`.
- O texto dos arquivos é exibido em controles de texto simples, já limpo e cortado pelo Core.

### 7. Pasta de status por parâmetro de linha de comando

`--pasta-status <caminho>` substitui a pasta padrão.

- Por quê: permite desenvolver e demonstrar o painel com uma pasta de exemplo, sem depender da conta `ia`. O valor vem de quem inicia o painel (o usuário principal), não de dados do `ia`, então não fere RS-1.

### 8. Testes

xUnit no `DataPilot.Core.Tests`. Regras puras são testadas com relógio falso. Leitura e monitor usam pastas temporárias reais, incluindo um teste em que o arquivo é substituído pela mesma operação do hook enquanto está aberto para leitura.

- A interface é verificada por um roteiro manual curto, com uma pasta de exemplo e depois com sessões reais.

## Risks / Trade-offs

- **Interface sem testes automatizados** → a lógica fica toda no Core; o que sobra na tela é ligação de dados, coberta pelo roteiro manual.
- **Sessão do Claude realmente esperando há mais de 8 horas aparece como inativa** → efeito aceito da decisão dos dois prazos. Ela continua na lista por 3 dias e volta ao status real no próximo evento.
- **`/clear` gera duas linhas para o que o usuário vê como uma sessão** → a antiga fica "encerrada" e some depois de 3 dias. O vínculo entre elas é assunto da Etapa 4.
- **Relógio da máquina alterado** → datas no futuro são tratadas como "agora"; a ordem pode ficar estranha até o próximo evento.
- **Restauração de pacotes NuGet bloqueada pela rede da empresa** → são quatro pacotes conhecidos; se o `nuget.org` não for alcançável pelo usuário principal, é preciso apontar para o feed interno antes da primeira compilação.
- **Formato novo de solution não abrir numa versão antiga do Visual Studio** → se for o caso, converter para `.sln` com o próprio `dotnet`; nenhum outro arquivo muda.
- **Arquivos de status se acumulam** → o limite de 500 e o filtro de 3 dias mantêm a tela utilizável; a limpeza continua sem dono (questão em aberto da Etapa 1).

## Migration Plan

Não há instalação nesta etapa: o painel é compilado e executado a partir do repositório pelo usuário principal (`dotnet run`). Reverter é fechar o painel; nada fora do repositório é alterado.

## Open Questions

- Qual versão do Visual Studio (ou outro editor) será usada: afeta apenas o formato do arquivo de solution.
- Publicação do painel como executável único: fica para quando houver uso diário.
