# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a conta `ia` ou a área de trabalho do Windows. Ao fim de cada grupo, `dotnet build` sem avisos e `dotnet test` passando.

## 1. Configurações no Core

- [ ] 1.1 Acrescentar `UsuarioSshValido` a `ValidadorEntrada` (decisão 2), com testes de `ia`, `agente-ia`, `-oProxyCommand=calc`, `.x`, vazio e 21 caracteres; verificar que os testes passam
- [ ] 1.2 Ler a seção `configuracoes` em `DadosPainel.Carregar()` (campo ausente ou inválido vale o padrão; arquivo ilegível usa os padrões) e criar `SalvarConfiguracoes` com o `Alterar` existente, preservando os demais campos; cobrir com testes os cenários de "Valores padrão" e de "Configurações gravadas no arquivo de dados" e verificar que passam
- [ ] 1.3 Trocar `Constantes.DestinoSsh` por `UsuarioSshPadrao` e `HostSsh` e fazer o `MontadorComando` montar com e sem SSH, validando o usuário de novo; a `AberturaSessao` passa as opções do `EstadoDados` carregado; cobrir com testes os cenários de "Composição do comando" (os já existentes e os de SSH com `ia`, com `agente-ia` e sem SSH) e verificar que passam

## 2. Pasta de status

- [ ] 2.1 Criar a pasta de status em `App.OnStartup` com `Directory.CreateDirectory` dentro de um `try` (falha cai no aviso de pasta indisponível); verificar abrindo o painel com `--pasta-status` apontando para uma pasta inexistente em `$TEMP` (a pasta é criada e a janela indica que não há sessões) e para um caminho sem permissão (aparece o aviso de pasta indisponível)

## 3. Tela

- [ ] 3.1 Criar o `ConfiguracoesWindow` (pasta de status em `TextBox` só para leitura, "Abrir pelo SSH" e usuário desabilitado com o SSH desligado, erro junto do campo, Enter salva, Esc cancela) e `IInteracao.DialogoConfiguracoes`; com o arquivo ilegível, mostrar que não é possível salvar
- [ ] 3.2 Acrescentar a ação "Configurações" (ícone de engrenagem com nome acessível) na barra superior da janela principal; abrir o painel com os dados de exemplo e conferir o diálogo, a pasta só para leitura, o campo desabilitado e o erro de usuário inválido

## 4. Documentação

- [ ] 4.1 Atualizar o PRD (o isolamento pelo usuário `ia` passa a ser o padrão; RS-7 com o SSH opcional, o usuário configurável e o host `localhost`) e o guia de instalação (o painel cria a pasta de status se ela não existir; aplicar depois as permissões da instalação); verificar que os caminhos e nomes citados existem
- [ ] 4.2 Atualizar o README com a tela de configurações; verificar que os passos descritos funcionam como escritos

## 5. Verificação manual

- [ ] 5.1 **[manual]** Com a pasta de status removida (ou renomeada), abrir o painel e conferir que ela foi criada e que a tela de configurações mostra o caminho
- [ ] 5.2 **[manual]** Com o SSH ligado e o usuário `ia`, abrir e retomar uma sessão do Claude e uma do Codex; conferir que rodam como `ia`
- [ ] 5.3 **[manual]** Desligar "Abrir pelo SSH", abrir uma sessão e conferir que a aba roda como o usuário principal, sem `ssh`; religar e conferir que a próxima sessão volta a usar o SSH

## Workflow follow-up

- Implementar depois da change `refatorar-estrutura`.
- Arquivar esta change depois da verificação manual.
