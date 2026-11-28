# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a conta `ia`, um administrador ou a área de trabalho do Windows. Ao fim de cada grupo, `dotnet build` sem avisos e `dotnet test` passando.

## 1. Instalação no Core

- [x] 1.1 Embutir `scripts/abrir.ps1` e `scripts/hook.ps1` no `DataPilot.Core.csproj` como `EmbeddedResource` com `LogicalName` fixo e acrescentar `Constantes.PastaScripts = @"C:\Painel"`; verificar com um teste que os bytes do recurso são iguais aos dos arquivos do repositório
- [x] 1.2 Criar `ScriptsDoPainel` com a verificação da decisão 2, a criação da decisão 3 e a gravação da decisão 4, recebendo a pasta de destino; cobrir com testes numa pasta temporária: criação com os dois scripts, permissões depois da criação (Usuários sem direito de gravação, herança cortada), script diferente substituído, script igual não regravado (data de modificação mantida), `exemplos` intocada, pasta que dá Modificar a "Usuários autenticados" recusada sem gravar, junção recusada (criada com `mklink /J`) e falta de permissão devolvendo aviso; verificar que os testes passam

## 2. Painel

- [x] 2.1 Chamar a instalação em `App.OnStartup` antes de criar a janela e mostrar o aviso, quando houver, pela faixa de mensagens do `MainViewModel`, com o motivo e, na falta de permissão, o comando `icacls`; abrir o painel com uma pasta de destino temporária e conferir a criação, e com uma pasta só leitura e script diferente conferir o aviso e que a lista continua funcionando

## 3. Documentação

- [x] 3.1 Atualizar o guia de instalação: no passo 1, as permissões de `C:\Painel` com gravação para o usuário principal; o passo de copiar os scripts vira conferência (o painel instala o `abrir.ps1` e o `hook.ps1`; `testes` e `exemplos` continuam manuais); mudanças nos scripts vão para o repositório. Atualizar o README; verificar que os comandos citados funcionam como escritos

## 4. Verificação manual

- [x] 4.1 **[manual]** Como administrador, dar gravação ao usuário principal em `C:\Painel` (`icacls C:\Painel /grant "<usuário>:(OI)(CI)M"`), alterar uma linha de comentário no `C:\Painel\hook.ps1`, abrir o painel e conferir que o script voltou ao conteúdo do repositório
- [x] 4.2 **[manual]** Como `ia`, tentar alterar `C:\Painel\abrir.ps1` e conferir que é negado; abrir uma sessão pelo painel e conferir que o hook continua gravando o status

## Workflow follow-up

- Implementar depois da `configuracoes-do-painel`.
- Arquivar esta change depois da verificação manual.
