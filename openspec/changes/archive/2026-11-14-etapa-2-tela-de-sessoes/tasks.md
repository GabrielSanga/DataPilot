# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem olhar a janela ou usar agentes reais na conta `ia`.

## 1. Estrutura da solution

- [x] 1.1 Criar a solution e os projetos `src/DataPilot.Core` (`net10.0`), `src/DataPilot.Wpf` (`net10.0-windows`, WPF) e `tests/DataPilot.Core.Tests` (`net10.0`, xUnit), com as referências entre eles e `Nullable` ativado; verificar que `dotnet build` conclui sem erros nem avisos
- [x] 1.2 Adicionar `CommunityToolkit.Mvvm` ao projeto WPF, com versão fixa; verificar que `dotnet restore` conclui e que `dotnet test` roda o teste de exemplo do modelo de projeto
- [x] 1.3 Acrescentar ao `README.md` como compilar, testar e executar o painel; verificar que os comandos documentados funcionam como escritos

## 2. Modelo e validação do arquivo de status

- [x] 2.1 Criar no Core o modelo de sessão e os status gravados e exibidos; verificar que o projeto compila e que o Core não referencia nenhum pacote de interface
- [x] 2.2 Implementar a validação de um arquivo de status a partir do seu conteúdo e do nome do arquivo; cobrir com testes cada cenário de "Validação do conteúdo" da spec `leitura-de-status` (JSON inválido, status desconhecido, identificador diferente do nome, campo ausente, campo adicional, agente desconhecido, data sem fuso) e verificar que passam
- [x] 2.3 Implementar a limpeza de texto (sem caracteres de controle, corte em 260 caracteres) e a extração do nome da pasta; cobrir com testes os cenários de "Texto dos arquivos exibido como texto simples" e pastas com barra final ou vazias, e verificar que passam

## 3. Regras de exibição

- [x] 3.1 Implementar o status exibido com os dois prazos de inatividade, usando `TimeProvider`; cobrir com testes cada cenário de "Status exibido com regra de inatividade", incluindo os limites exatos de 15 minutos e 8 horas e a data no futuro, e verificar que passam
- [x] 3.2 Implementar a ordem por urgência e o resumo do cabeçalho; cobrir com testes os cenários de "Ordem por urgência" e "Resumo no cabeçalho" e verificar que passam
- [x] 3.3 Implementar o filtro de sessões antigas (3 dias) e o texto de tempo decorrido; cobrir com testes os cenários de "Sessões antigas ocultas por padrão", o limite exato de 3 dias e que sessões ocultas não entram no resumo, e verificar que passam

## 4. Leitura e monitoramento da pasta

- [x] 4.1 Implementar a leitura de um retrato da pasta: só `.json`, teto de 16 KB por arquivo, 500 arquivos mais recentes, abertura compartilhando leitura, escrita e exclusão, sem seguir atalhos; cobrir com testes em pasta temporária os cenários de "Sessões lidas da pasta de status", "Limite de tamanho" e "Limite de quantidade" e verificar que passam
- [x] 4.2 Tratar falhas: arquivo removido ou bloqueado durante a leitura e pasta inexistente ou ilegível marcada como indisponível; cobrir com testes os cenários de "Tolerância a falhas de leitura" e verificar que passam
- [x] 4.3 Adicionar um teste em que o arquivo é substituído pela operação atômica usada no `hook.ps1` enquanto está aberto pelo leitor do painel; verificar que a substituição conclui e que o leitor obtém um conteúdo completo
- [x] 4.4 Implementar o monitor com aviso de mudança, releitura a cada 5 segundos e releituras sem sobreposição; cobrir com testes em pasta temporária os três cenários de "Atualização em até 5 segundos" e o de pasta que passa a existir, com o aviso de mudança desligado para provar o caminho do temporizador, e verificar que passam
- [x] 4.5 Adicionar um teste que executa leitura e monitor sobre uma pasta e confere que nenhum arquivo foi criado, alterado ou excluído nela; verificar que passa

## 5. Janela de sessões

- [x] 5.1 Criar a janela com cabeçalho, opção "mostrar antigas" e a lista (agente, pasta, status com cor e texto, tempo decorrido, com a pasta completa e o identificador disponíveis na linha); verificar que o projeto compila e que a janela abre com `dotnet run`
- [x] 5.2 Implementar o view model que recebe os retratos do monitor, aplica as regras do Core e atualiza a coleção no lugar, na thread da interface; verificar, com uma pasta de exemplo, que mudar um arquivo altera a linha sem perder a seleção
- [x] 5.3 Implementar o aviso de pasta indisponível, o estado "sem sessões" e o parâmetro `--pasta-status`; verificar iniciando o painel sem a pasta, com a pasta vazia e com a pasta de exemplo
- [x] 5.4 Adicionar ao repositório uma pasta de exemplo com arquivos de status válidos, inválidos e antigos, e documentar no `README.md` como iniciar o painel com ela; verificar que a lista mostra só os válidos, na ordem e com as cores da spec `lista-de-sessoes`

## 6. Verificação integrada

- [x] 6.1 **[manual]** Abrir o painel com a pasta de exemplo e conferir o roteiro do `README.md`: cores, ordem, resumo, opção "mostrar antigas" e texto cortado
- [x] 6.2 **[manual]** Abrir o painel sem parâmetros e usar uma sessão do Claude e uma do Codex como `ia`; verificar que cada mudança de status aparece na janela em até 5 segundos e que os hooks continuam gravando normalmente
- [x] 6.3 **[manual]** Como `ia`, colocar na pasta de status um arquivo `.json` inválido e outro com mais de 16 KB; verificar que o painel continua aberto e não os lista, e depois apagar os dois como `ia`
- [x] 6.4 **[manual]** Deixar o painel aberto durante um período de trabalho; verificar que uma sessão parada passa a "inativa" sozinha e que a janela continua respondendo

## Workflow follow-up

- Levar para a Etapa 3 o que o uso diário mostrar sobre a lista (colunas que faltam, prazos de inatividade).
- Arquivar a change depois que as verificações manuais do grupo 6 estiverem concluídas.
