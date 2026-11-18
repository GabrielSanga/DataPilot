# Proposal

## Why

O painel funciona, mas não parece um produto: usa os controles padrão do WPF sem nenhum tratamento, todos os botões têm o mesmo peso, e o que precisa do usuário compete com tudo o mais na tela. Capturas da versão atual, com os dados de exemplo, mostram problemas concretos:

- Com nenhuma demanda selecionada, a área da direita mostra uma barra vazia, os botões "Editar", "Arquivar", "Desarquivar" e "Vincular repositório" e o texto de demanda sem repositórios, por cima da orientação "Selecione uma demanda na lista".
- O status de todas as sessões aparece como etiqueta colorida cheia. Numa lista de 18 sessões, a coluna vira um arco-íris e as duas que precisam do usuário não se destacam.
- Cada linha tem "Retomar", "Mover" e "Remover" lado a lado, com o mesmo peso; no card, "Remover" o repositório fica colado em "Nova sessão".
- O resumo do cabeçalho diz "2 precisa de atenção", com o plural errado, e os textos de resumo encadeiam pedaços com "·" ("2 repositórios · 0 sessões · sem status").
- Não há como filtrar a lista de demandas nem ir direto à sessão que pede atenção.
- Erros e confirmações aparecem em caixas de mensagem genéricas do Windows.

O objetivo do MVP é "ver de relance o que precisa de mim". A tela atual mostra tudo com a mesma intensidade.

## What Changes

- **Identidade visual "cockpit escuro".** Como num cockpit, onde as luzes só acendem quando algo está anormal, a tela fica calma por padrão e só acende o que precisa do usuário. Cor passa a ser só sinal de status; o resto da interface é grafite sobre fundo claro. Identificadores, contagens e tempos usam a Bahnschrift, fonte de sinalização que acompanha o Windows; o texto corrido usa a Segoe UI.
- **Alerta no topo.** Uma faixa acende em vermelho com quantas sessões precisam de atenção, ou em amarelo com quantas aguardam o usuário, com "Ir para a próxima" (também pela tecla F8), que seleciona a demanda dessa sessão.
- **Linhas de sessão como faixas.** As sessões em "precisa de atenção" e "aguardando você" ganham uma faixa colorida à esquerda e o status em cor; as demais mostram só um indicador pequeno.
- **Uma ação principal por linha.** "Retomar" fica visível; "Mover para outra demanda" e "Remover registro" vão para o menu "Mais ações" da linha. O mesmo vale para o repositório ("Nova sessão" visível, "Remover repositório" no menu) e para a demanda ("Vincular repositório" visível, "Editar", "Arquivar" e "Desarquivar" no menu).
- **Filtro de demandas** acima da lista (Ctrl+F), por identificador, título, change e repositório.
- **Estado sem seleção corrigido**: só a orientação aparece, sem ações nem progresso.
- **Diálogos e mensagens no mesmo tema**: botão principal com o nome da ação, Enter e Esc, erro de validação junto do campo, confirmação de remoção que nomeia o que será removido, e erros de operação numa faixa dentro da janela, em vez de caixas de mensagem.
- **Textos revisados**: plural correto, contagens sem "·", estados vazios que dizem o que fazer.
- **Teclado e acessibilidade**: foco visível, atalhos (Ctrl+N, Ctrl+F, F8), menus pelo teclado e respeito ao alto contraste do Windows.

O comportamento das sessões, dos dados e dos scripts não muda. O título das abas do terminal continua no formato atual, que é parte do comando montado pelo painel.

Fora desta change: tema escuro, barra de título personalizada, lembrar tamanho e posição da janela, animações além do alerta, a bandeja e as notificações (`etapa-5b-bandeja-e-avisos`).

## Capabilities

### New Capabilities

- `padroes-de-interface`: regras comuns a toda a interface do painel: teclado e foco, alto contraste, diálogos, confirmação de remoção, erros dentro da janela e texto.

### Modified Capabilities

- `lista-de-sessoes`: alerta do que precisa do usuário, "Ir para a próxima", filtro de demandas, estado sem seleção, destaque das sessões que precisam do usuário e organização das ações em ação principal e menu.

## Impact

- **Código:** um dicionário de tema novo no `DataPilot.Wpf`, aplicado à janela principal e a todos os diálogos; a janela principal é reorganizada; os diálogos de confirmação e de erro passam a ser do próprio painel. No `DataPilot.Core`, o resumo passa a ser estruturado, com plural correto, e entram duas funções puras: o filtro de demandas e a escolha da próxima sessão que precisa do usuário.
- **Dependências:** nenhum pacote novo. Fontes e ícones são os que acompanham o Windows 10 (Bahnschrift, Segoe UI e Segoe MDL2 Assets).
- **Dados, scripts e segurança:** nenhuma mudança. Os textos exibidos continuam vindo do arquivo de dados ou, no caso do arquivo de status, limpos como hoje.
- **Pendência anterior:** a change `etapa-5a-progresso-openspec` está concluída e ainda não foi arquivada; esta change não altera os requisitos dela.
