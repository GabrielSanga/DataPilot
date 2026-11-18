# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a conta `ia`, elevação de administrador ou um agente interativo.

## 1. Core

- [ ] 1.1 Implementar a contagem das tarefas de um texto de `tasks.md`; cobrir com testes os cenários de "Contagem das tarefas", incluindo `X` maiúsculo, indentação e linhas parecidas que não casam, e verificar que passam
- [ ] 1.2 Implementar a localização do `tasks.md` (change ativa e arquivada, com a data mais recente e a prioridade da ativa), com a raiz recebida por parâmetro e revalidação de `pasta` e `openspecChange`; cobrir com testes os cenários de "Localização do tasks.md" e "Change arquivada" e verificar que passam
- [ ] 1.3 Implementar a leitura segura: recusa de atalho ou junção em cada trecho do caminho, conferência do arquivo aberto, limite de 256 KB, compartilhamento de escrita e exclusão, UTF-8 estrito e cache por data de modificação e tamanho; cobrir com testes os cenários de "Leitura segura do tasks.md", criando junções com `mklink /J` numa pasta temporária, e verificar que passam
- [ ] 1.4 Montar o progresso de uma demanda (soma dos repositórios lidos, progresso por repositório, arquivada, não encontrada, ilegível, sem tarefas); cobrir com testes os cenários de "Exibição do progresso" e verificar que passam

## 2. Painel

- [ ] 2.1 Acrescentar a constante da raiz dos fontes e o parâmetro `--raiz-fontes`, e reler o progresso da demanda selecionada a cada atualização da tela e ao trocar de demanda, sem `FileSystemWatcher` nas pastas do `ia`; verificar que a demanda selecionada se mantém
- [ ] 2.2 Mostrar a barra com a soma abaixo do nome da change, o progresso em cada card, a indicação "arquivada", "change não encontrada nos repositórios", "progresso indisponível" e "sem tarefas"; verificar que o painel compila e que os testes continuam passando
- [ ] 2.3 Fazer o gerador de exemplos criar uma raiz de fontes de exemplo com uma change ativa, uma arquivada e uma junção, e um arquivo de dados que aponte para elas; verificar no painel, com `--raiz-fontes`, cada indicação da tarefa 2.2 e a atualização ao marcar uma tarefa no arquivo de exemplo
- [ ] 2.4 Atualizar o `README.md` com o progresso do OpenSpec, o parâmetro `--raiz-fontes` e o roteiro de verificação manual, e o guia de instalação com a recomendação de manter o modo de desenvolvedor do Windows desligado; verificar que os comandos documentados funcionam como escritos

## 3. Verificação integrada

- [ ] 3.1 **[manual]** Como `ia`, rodar `whoami /priv` e verificar que `SeCreateSymbolicLinkPrivilege` não aparece
- [ ] 3.2 **[manual]** Criar uma change de teste num repositório de `C:\Fontes`, como `ia`, e informá-la no `openspecChange` de uma demanda; verificar que a barra e o card mostram o progresso certo e que, ao marcar uma tarefa, o número muda em até 5 segundos
- [ ] 3.3 **[manual]** Arquivar a change de teste com o OpenSpec, como `ia`; verificar que a demanda passa a mostrar a change como arquivada, com o progresso final
- [ ] 3.4 **[manual]** Trocar, como `ia`, a pasta da change de teste por uma junção para outra pasta; verificar que o card mostra "progresso indisponível" e desfazer a junção

## Workflow follow-up

- Repetir a verificação 3.3 depois de atualizar o OpenSpec.
- Arquivar a change depois que as verificações manuais do grupo 3 estiverem concluídas.
