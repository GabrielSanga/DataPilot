# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver `specs/lista-de-sessoes/spec.md` e `specs/padroes-de-interface/spec.md`.

**O produto.** Uma mesa de controle pessoal para quem despacha e acompanha sessões de agentes de código (Claude Code e Codex), agrupadas por demanda (um chamado como `CHG-142`) e por repositório. Fica aberta o dia inteiro ao lado do Windows Terminal e é consultada de relance, muitas vezes por dia.

**Quem usa.** Um desenvolvedor, numa máquina Windows 10 corporativa, com monitores de escritório.

**O trabalho principal da tela.** Responder em um segundo "alguma sessão precisa de mim?" e, se sim, levar até ela e retomar com um clique.

O que mais molda a solução:

- O nome do produto é DataPilot. O vocabulário de aviação é natural aqui: o usuário é quem supervisiona vários "pilotos" automáticos.
- Os status já têm cores definidas em spec: vermelho para "precisa de atenção", amarelo para "aguardando você", azul para "executando", verde para "ativa" e cinza para "encerrada" e "inativa", sempre com o nome do status em texto.
- Sem pacotes novos: a etapa 6a mostrou o custo de dependências de terceiros e da aprovação de TI. Bibliotecas de tema para WPF ficam de fora.
- Fontes disponíveis sem instalar nada: conferido nesta máquina, o Windows 10 traz a Bahnschrift, a Segoe UI e a Segoe MDL2 Assets.
- Capturas da versão atual (dados de exemplo) estão descritas no `proposal.md`.

## Goals / Non-Goals

**Goals:**

- O que precisa do usuário é a coisa mais visível da tela, e o resto fica em silêncio.
- Uma ação principal por linha, com as destrutivas longe dela.
- Identidade própria, coerente com o assunto, feita só com recursos do Windows e do WPF.
- Teclado, foco visível e alto contraste funcionando.

**Non-Goals:**

- Tema escuro.
- Barra de título personalizada.
- Mudar o comportamento de sessões, dados ou scripts.

## Decisions

### 1. Direção: "cockpit escuro"

Em aviação, "cockpit escuro" é a filosofia de painel em que as luzes ficam apagadas quando tudo está normal e só acendem quando algo pede o piloto. A tela adota isso:

- **Calma por padrão.** Sessões executando, ativas, inativas e encerradas aparecem em tom baixo: texto do status em cor, um indicador pequeno, nada de etiqueta cheia.
- **Só o que precisa do usuário acende.** "Precisa de atenção" e "aguardando você" ganham uma faixa colorida de 4 px à esquerda da linha e o status em cor forte. O alerta do topo acende em vermelho ou amarelo.
- **Cor é sinal.** Botões, barras de progresso, divisórias e seleção usam grafite e tons neutros. A barra de progresso do OpenSpec é grafite, e não verde, porque verde é o status "ativa".
- **Identificadores falam em Bahnschrift.** `CHG-142`, nomes de repositório, contagens e tempos usam a Bahnschrift, a fonte de sinalização DIN que acompanha o Windows. É a parte "instrumento" da tela.

O memorável é um elemento só: o alerta do topo com as faixas acesas. Todo o resto é discreto.

Direção aprovada pelo usuário em 17/11/2026, a partir de um esboço da janela principal com os dados de exemplo (alerta vermelho no topo, trilho claro de demandas, baia `Faturamento` com faixas em atenção, aguardando, executando e um lançamento pendente).

### 2. Cores

| Nome | Hex | Uso |
|---|---|---|
| Névoa | `#F1F3F5` | fundo da janela |
| Papel | `#FFFFFF` | faixas de sessão, campos, diálogos |
| Trilho | `#E8ECEF` | fundo da lista de demandas |
| Grafite | `#2B3440` | texto, botão principal, barra de progresso, seleção |
| Grafite claro | `#5B6672` | texto secundário |
| Linha | `#D9DEE3` | divisórias e bordas |

Sinais de status (as cores da spec):

| Status | Cor do sinal | Texto sobre fundo claro |
|---|---|---|
| precisa de atenção | `#C8102E` | `#A30D25` |
| aguardando você | `#F2A900` | `#6B4A00` |
| executando | `#1F5FBF` | `#1F5FBF` |
| ativa | `#2E7D4F` | `#2E7D4F` |
| inativa, encerrada, sem status | `#8A949C` | `#6E7781` |

Todo texto tem contraste de pelo menos 4,5:1 com o fundo onde aparece. O amarelo nunca é usado como cor de texto sobre branco; o texto de "aguardando você" é o marrom `#6B4A00`. No alerta vermelho, o texto é branco; no amarelo, é `#3A2A00`.

### 3. Tipografia

- **Bahnschrift**, largura semicondensada, peso regular e seminegrito: identificador da demanda, nome do repositório, contagens, tempo decorrido, nome do produto.
- **Segoe UI**: todo o resto (títulos de demanda, rótulos, botões, textos de ajuda, diálogos).

Escala (em pontos do WPF): 11 para metadados, 12 para texto secundário, 13 para o corpo, 15 para nome do repositório e título da demanda, 26 para o identificador da demanda selecionada. Sem letras maiúsculas como estilo, sem rótulos acima de cada bloco. Números com algarismos de largura fixa onde houver colunas (tempo decorrido), para não dançarem a cada atualização.

### 4. Layout

Tudo alinhado à esquerda. Três regiões: barra superior, trilho de demandas e demanda selecionada.

```
┌────────────────────────────────────────────────────────────────────────────┐
│ DataPilot   [● 2 precisam de atenção │ Ir para a próxima]  4 aguardando você  4 executando │
├─────────────────┬──────────────────────────────────────────────────────────┤
│ [+ Nova demanda]│ CHG-142  Ajuste NF-e                                    ⋯│
│ [Filtrar…     ] │ ajuste-nfe  ▬▬▬▬▬▬▬▭▭▭▭  7 de 12 tarefas  [Vincular repositório]│
│▌CHG-142         │                                                          │
│ Ajuste NF-e     │ Faturamento  4 de 6                       [Nova sessão] ⋯│
│ 2 repositórios, │ ════════════════════════════════════════════════════════ │
│ 3 sessões       │▌Claude  implementação  ● precisa de atenção  2 min [Retomar] ⋯│
│  CHG-150        │▌Codex   revisão        ● aguardando você    14 min [Retomar] ⋯│
│  Relatório…     │ Claude  sem rótulo     • executando          1 min [Retomar] ⋯│
│ ─────────────── │ Codex   aguardando a primeira mensagem  3 min  [Abrir] ⋯ │
│  Não vinculadas │                                                          │
│  18 sessões     │ Estoque  3 de 6                           [Nova sessão] ⋯│
│                 │ ════════════════════════════════════════════════════════ │
│ ☐ Mostrar       │ Nenhuma sessão aqui ainda. Use "Nova sessão" para abrir… │
│   arquivadas    │                                                          │
└─────────────────┴──────────────────────────────────────────────────────────┘
```

- **Barra superior:** nome do produto, o alerta (decisão 5) e as contagens de "aguardando você" e "executando" em texto.
- **Trilho de demandas:** "Nova demanda" (botão principal), filtro, lista. Cada item tem o identificador em Bahnschrift, o título e uma linha de contagens. A demanda com sessão que precisa do usuário ganha a mesma faixa de 4 px na cor do sinal mais urgente. A seleção é um fundo branco. "Não vinculadas" fica separada por uma linha, no fim. "Mostrar arquivadas" fica no pé do trilho, perto do que afeta.
- **Repositórios como baias, não cards.** Cada repositório é um cabeçalho (nome, progresso, "Nova sessão", menu) sobre uma linha grafite de 2 px, seguido das faixas das sessões. Sem caixas com borda e sombra em volta de cada repositório: o agrupamento vem da linha e do espaço.
- **"Não vinculadas":** a explicação vira uma frase curta; "Mostrar antigas" sai do topo da janela e vai para dentro desta área, que é a única que ela afeta.

### 5. Alerta e "Ir para a próxima"

- Com sessões em "precisa de atenção": faixa vermelha com "N precisa(m) de atenção" e "Ir para a próxima".
- Sem nenhuma em atenção e com sessões em "aguardando você": faixa amarela com "N aguardando você" e "Ir para a próxima".
- Sem nenhuma das duas: texto discreto "Nada precisa de você agora", sem faixa.
- "Ir para a próxima" e a tecla F8 selecionam a próxima demanda, na ordem da lista e depois da selecionada, que tenha sessão no status destacado, voltando ao início. Sessões não vinculadas levam a "Não vinculadas". A escolha é uma função pura do Core, testada.
- Movimento: quando o alerta acende, ele aparece com um esmaecimento de 150 ms, uma vez. Nada pisca. Com as animações do Windows desligadas, aparece direto.
- F8 é o atalho de "próximo erro" em editores como o Visual Studio, familiar para quem usa o painel.

### 6. Ações: uma principal, o resto no menu

| Lugar | Visível | Menu "Mais ações" (⋯) |
|---|---|---|
| Sessão registrada | Retomar | Mover para outra demanda, Remover registro |
| Lançamento pendente | Abrir | Cancelar |
| Sessão não vinculada | Vincular | (sem menu) |
| Repositório | Nova sessão | Remover repositório |
| Demanda | Vincular repositório | Editar, Arquivar ou Desarquivar |

- O menu abre com clique, com a tecla de menu ou Shift+F10 sobre a linha.
- O botão do menu é só ícone, com nome acessível ("Mais ações da sessão").
- Um "Vincular" indisponível (identificador fora do formato) continua desabilitado, mas o motivo passa a aparecer na própria linha, em texto pequeno, e não só na dica.

### 7. Tema em WPF, sem bibliotecas

Um dicionário `Tema.xaml`, mesclado no `App.xaml`, define as cores como pincéis com chave, as fontes, os tamanhos e os estilos de: botão (principal, secundário, discreto e de ícone), item de lista, campo de texto, caixa de combinação, caixa de seleção, escolha em botões (usada para o agente), barra de progresso, dica, menu e indicador de foco. A janela principal e todos os diálogos usam esses estilos.

- Os pincéis são referenciados como recurso dinâmico, para o alto contraste poder trocá-los.
- **Alto contraste:** com o alto contraste do Windows ligado, o painel mescla um dicionário que aponta os mesmos nomes para as cores do sistema e deixa os controles com a aparência do sistema. Os textos de status continuam presentes, então a informação não depende de cor.
- **Ícones:** glifos da Segoe MDL2 Assets (mais ações, adicionar, filtrar, retomar), sempre acompanhados de texto ou de nome acessível.
- **Foco:** um contorno grafite de 2 px, afastado 2 px do controle, em todos os controles focáveis.
- A barra de título continua a padrão do Windows, para não perder encaixe de janelas, acessibilidade e comportamento de sistema.

### 8. Diálogos, confirmações e erros

- **Diálogos de formulário** (nova demanda, editar, nova sessão, vincular repositório, vincular sessão, mover sessão): título curto, campos com rótulo acima, ajuda em texto secundário, erro em vermelho logo abaixo do campo, botão principal à direita com o nome da ação ("Criar demanda", "Abrir sessão", "Vincular", "Mover") e "Cancelar". Enter confirma, Esc cancela. Um erro não fecha o diálogo nem apaga o que foi digitado.
- **Agente da nova sessão:** dois botões de escolha lado a lado, "Claude" e "Codex", em vez de uma caixa de combinação.
- **Confirmações** (remover registro, remover repositório, arquivar com sessão em andamento, abrir outra aba de sessão possivelmente aberta): diálogo do próprio painel, que diz o que vai acontecer, com o botão nomeado pela ação ("Remover registro") e "Cancelar" como padrão, para Enter não confirmar uma remoção por engano.
- **Erros de operação** (terminal que não abre, gravação que falha): uma faixa no topo da área da demanda, com o que aconteceu e o que fazer, e um botão para fechar. Não bloqueia a janela. Mensagens informativas usam a mesma faixa, em tom neutro.

### 9. Texto

- Plural correto em todas as contagens: "1 precisa de atenção", "2 precisam de atenção", "1 sessão", "2 sessões", "nenhuma sessão", "1 repositório".
- Contagens em frase, separadas por vírgula ("2 repositórios, 3 sessões"), e não encadeadas com "·".
- Estados vazios dizem o que fazer: "Nenhuma sessão aqui ainda. Use "Nova sessão" para abrir o Claude ou o Codex neste repositório."
- Rótulo vazio aparece como "sem rótulo", em texto secundário.
- O resumo do Core passa a ser estruturado (contagens por status e textos com plural), e a tela monta as partes. O título das abas do terminal (`CHG-142 · Faturamento · rótulo`) não muda, porque faz parte do comando e está em spec.

### 10. Revisão contra o padrão genérico

Para um pedido parecido ("deixe este painel WPF profissional"), a resposta padrão seria: barra lateral escura, uma cor de destaque azul nos botões, cards arredondados com sombra para cada repositório, etiquetas coloridas cheias para todos os status, rótulos em maiúsculas acima das seções e metadados com "·". O plano acima foi revisado contra isso:

- **Barra lateral escura → trilho claro.** O contraste forte fica reservado para o alerta.
- **Cor de destaque nos botões → botão principal grafite.** Se os botões fossem azuis, competiriam com "executando".
- **Cards com sombra → baias com linha.** Menos caixas, a hierarquia vem do cabeçalho do repositório.
- **Etiquetas cheias para tudo → só dois status acesos.** É o que torna o "precisa de mim" visível.
- **Metadados com "·" → frases com vírgula.** Fica só no título das abas, que é funcional.
- **Tinta quase preta → grafite `#2B3440`.** Um cinza-azulado claramente distinto de preto, o mesmo do botão principal e da barra de progresso, para a interface ter um único neutro forte.

### 11. Verificação visual

A implementação é conferida com capturas da janela com os dados de exemplo, comparadas com o layout da decisão 4. As capturas podem ser feitas sem interação manual: iniciar o painel com `--pasta-status`, `--dados` e `--raiz-fontes` apontando para os exemplos, selecionar itens da lista por UI Automation e capturar a janela com `PrintWindow`, numa pasta temporária. As capturas não entram no repositório.

### 12. Testes

No Core: o resumo estruturado (plural e omissão de zeros), o filtro (identificador, título, change, repositório, sem diferenciar maiúsculas nem acentos, "Não vinculadas" sempre presente) e a escolha da próxima sessão (atenção antes de aguardando, ordem da lista, volta ao início, não vinculadas). A tela, o tema, o teclado e o alto contraste são verificados por captura e à mão.

## Risks / Trade-offs

- **Bahnschrift ausente** (Windows mais antigo que a versão 1709) → a família cai para a Segoe UI; a tela continua legível, só perde a voz de instrumento.
- **Mais coisas atrás do menu** → mover e remover ficam a um clique a mais. É intencional: são raras e destrutivas.
- **Estilos próprios em todos os controles** → mais XAML para manter. Fica concentrado num único dicionário.
- **Alto contraste** → os estilos próprios precisam ceder às cores do sistema; coberto por uma verificação manual.
- **Mudança visual grande de uma vez** → os testes do Core seguram o comportamento; a verificação manual repassa todos os fluxos.

## Migration Plan

Nada a instalar nem migrar. O painel novo abre com o tema novo.

## Open Questions

- Tema escuro, para quem usa o Windows em modo escuro.
- Lembrar tamanho e posição da janela.
- Densidade compacta para muitas sessões.
