# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver as três specs em `specs/`.

O que molda a solução:

- O painel registra a sessão nova do Claude no clique, com um GUID gerado por ele, e a abre com `claude --session-id`. O Claude só grava a conversa depois da primeira mensagem. Se a aba for fechada antes disso, `claude --resume` com esse GUID não encontra a conversa. Confirmado pelo usuário: retomar funciona quando houve mensagem e falha quando não houve.
- O Codex só roda o hook de início ao enviar a primeira mensagem, então o arquivo de status de uma sessão nova só surge nesse momento.
- A etapa 4c já grava, no clique de "Nova sessão" do Codex, um lançamento pendente no repositório do card, com identificador, agente, rótulo e data. Ele é removido quando vincula uma sessão e vale por 24 horas. O `DadosPainel` já tem a operação que desfaz um lançamento, usada quando o terminal não abre.
- O painel não pode usar valores da pasta de status para montar comandos. Saber se uma sessão do Claude tem conversa gravada é algo que só o usuário `ia` enxerga, então a decisão cabe ao `abrir.ps1`.
- O `verificar.ps1` hoje só exercita as validações do `abrir.ps1`, que terminam antes de iniciar o agente. A raiz dos fontes é fixa no script e não vem por parâmetro.

## Goals / Non-Goals

**Goals:**

- "Retomar" abre a sessão do Claude mesmo quando ela foi fechada antes da primeira mensagem, mantendo o mesmo identificador e, com isso, o mesmo registro e o mesmo arquivo de status.
- O usuário vê no card, logo depois do clique, que há uma sessão do Codex aberta à espera da primeira mensagem, e pode desfazer um lançamento que não vai usar.
- O comando que o painel monta não muda.

**Non-Goals:**

- Corrigir os status do Codex ao fechar a aba ou ao retomar.
- Conhecer o identificador da sessão do Codex antes da primeira mensagem.
- Fechar a aba do terminal ao cancelar um lançamento.

## Decisions

### 1. A segunda tentativa fica no `abrir.ps1`

Na retomada do Claude, o script roda `claude --resume`. Se o Claude terminar com código diferente de 0 em menos de 30 segundos, o script avisa na aba e roda `claude --session-id` com o mesmo identificador.

- Por quê: quem sabe se a conversa existe é o Claude, rodando como `ia`. O painel não tem como saber sem ler dados do `ia`, e usar o arquivo de status para escolher o modo quebraria a regra de que ele não compõe comandos.
- O identificador é o mesmo já validado, então o registro no arquivo de dados e o arquivo de status continuam valendo. Nenhum parâmetro novo.
- Alternativa descartada: o script procurar o arquivo da conversa na pasta do Claude. Depende do formato interno dessa pasta; se ele mudar, a retomada de todas as sessões quebra, e não só a das sessões sem mensagem.
- Alternativa descartada: o painel só registrar a sessão do Claude depois da primeira mensagem. A sessão deixaria de aparecer no card ao abrir.

### 2. Limite de 30 segundos para a segunda tentativa

A segunda tentativa só acontece quando a retomada falha logo. Uma retomada que termina com erro depois de mais tempo encerra o script com o código do agente.

- Por quê: uma conversa retomada com sucesso e encerrada com código diferente de 0, por exemplo por interrupção, não deve disparar uma tentativa de sessão nova, que só mostraria um erro de identificador já usado.
- 30 segundos cobrem com folga o tempo de o Claude iniciar e informar que não encontrou a conversa.
- Se uma conversa existente falhar em menos de 30 segundos, a segunda tentativa roda e o Claude a recusa. O efeito é uma mensagem de erro a mais na aba.

### 3. Ensaio antes do script

A primeira tarefa é um ensaio manual, como `ia`, numa pasta de `C:\Fontes`:

```powershell
$id = [guid]::NewGuid().ToString()
```

```powershell
claude --session-id $id
```

Sair com `/exit`, sem enviar mensagem. Depois:

```powershell
claude --resume $id; $LASTEXITCODE
```

```powershell
claude --session-id $id; $LASTEXITCODE
```

Esperado: a retomada informa que não encontrou a conversa e termina com código diferente de 0; a segunda linha abre o Claude normalmente.

- Por quê: a decisão 1 depende desses dois comportamentos. Se a retomada terminar com código 0, ou se o identificador não puder ser reutilizado, o desenho é revisto antes de mexer no script.

### 4. Só o Claude tem segunda tentativa

A retomada do Codex continua como está.

- Por quê: uma sessão do Codex só é registrada depois da primeira mensagem, então o painel nunca retoma uma sessão do Codex sem conversa.

### 5. O lançamento pendente aparece como linha do card

A visão agrupada passa a levar, em cada card, os lançamentos pendentes válidos do repositório, com o tempo decorrido. A tela mostra uma linha por lançamento, depois das sessões registradas, do mais recente para o mais antigo.

- A linha mostra o agente, o rótulo quando houver e o texto "aguardando a primeira mensagem". Não é uma sessão: não tem status de sessão, identificador nem "Retomar".
- Lançamentos vencidos e os de demanda arquivada não são mostrados, pela mesma regra que os impede de vincular.
- Por quê depois das sessões: a ordem das sessões é por urgência, e um lançamento não tem status para entrar nessa ordem.
- Tudo o que a linha mostra vem do arquivo de dados. A pasta de status não participa.

### 6. O lançamento fica fora das contagens e do status da demanda

A quantidade de sessões, o status mais urgente da demanda, a ordem das demandas e o resumo do cabeçalho continuam considerando só sessões.

- Por quê: o lançamento ainda não é uma sessão com status. Contá-lo mudaria a ordem das demandas a cada clique.

### 7. Cancelar reaproveita o desfazer do lançamento

"Cancelar" remove o lançamento do arquivo de dados com a operação que a etapa 4c já usa quando o terminal não abre. Não pede confirmação e não fecha a aba.

- Por quê sem confirmação: nada se perde. Se a aba ainda estiver aberta e receber uma mensagem, a sessão aparece em "Não vinculadas" e pode ser vinculada manualmente.
- Se o lançamento já tiver sido usado ou removido por outra janela do painel, o cancelamento não altera nada e a linha some na atualização seguinte.

### 8. Sincronização da tela

As linhas de lançamento seguem a mesma sincronização por chave das sessões, com o identificador do lançamento como chave, para que a demanda selecionada e a posição da lista não mudem quando a linha surge, some ou é substituída pela sessão.

- A substituição acontece no mesmo ciclo: o vínculo automático é aplicado antes de a tela ser montada, então a leitura que traz a sessão já não traz o lançamento.

### 9. Testes

- `verificar.ps1`: casos novos para a retomada do Claude, com um `claude` falso posto à frente no `PATH`, que registra os argumentos recebidos e termina com o código pedido. Casos: retomada com sucesso (uma chamada só); retomada que falha logo (segunda chamada com `--session-id` e o mesmo identificador, código final da segunda); retomada que falha depois de 30 segundos (sem segunda chamada); retomada do Codex que falha (sem segunda chamada). Como a raiz dos fontes é fixa, esses casos usam uma pasta existente em `C:\Fontes` e são pulados, com aviso, quando não houver nenhuma.
- Core: lançamentos pendentes na visão (válido, vencido, demanda arquivada, ordem, tempo), fora das contagens e do status; cancelamento (remove só aquele lançamento, não altera nada quando ele não existe, e a sessão que surge depois não é vinculada).

## Risks / Trade-offs

- **Uma versão futura do Claude mudar o código de saída da retomada sem conversa** → a segunda tentativa deixa de acontecer e a retomada volta a falhar como hoje. Repetir o ensaio depois de atualizar o Claude.
- **Uma versão futura do Claude deixar de aceitar o identificador reutilizado** → a segunda tentativa falha com a mensagem do Claude; o usuário remove o registro e abre uma sessão nova.
- **Linha de lançamento de uma aba já fechada** → fica no card até o usuário cancelar ou até vencer, em 24 horas. É o que a torna visível; antes ficava pendente sem aparecer.
- **Cancelar com a aba aberta** → a sessão, ao receber mensagem, vai para "Não vinculadas". O vínculo manual resolve.
- **O caso de teste da falha tardia leva mais de 30 segundos** → aceito; é um caso só.

## Migration Plan

1. Copiar `abrir.ps1` e `verificar.ps1` para `C:\Painel`, como administrador, e rodar a verificação como `ia`.
2. Usar o painel novo. A ordem entre os dois passos não importa: o comando montado pelo painel não muda.

Reverter: voltar ao painel anterior e ao `abrir.ps1` anterior, juntos ou separados. Nenhum dado gravado muda de formato.

## Open Questions

- Mostrar na linha do Claude recém-aberto que ele também ainda não recebeu mensagem. Hoje ele aparece como "ativa".
- Fechar a aba pelo painel ao cancelar um lançamento. Exigiria o painel acompanhar as abas que abriu.
