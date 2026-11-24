# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a conta `ia` ou a área de trabalho do Windows. Ao fim de cada grupo, `dotnet build` sem avisos e `dotnet test` passando.

## 1. Configurações no Core

- [ ] 1.1 Acrescentar a `ValidadorEntrada` os validadores `UsuarioSshValido` e `PastaStatusValida` (decisão 2), com testes de `ia`, `agente-ia`, `-oProxyCommand=calc`, `.x`, 21 caracteres, `D:\Painel\sessoes`, `sessoes`, `\\servidor\pasta` e caminho com caractere inválido; verificar que os testes passam
- [ ] 1.2 Ler a seção `configuracoes` em `DadosPainel.Carregar()` (campo ausente ou inválido vale o padrão; arquivo ilegível usa os padrões) e criar `SalvarConfiguracoes` com o `Alterar` existente, preservando os demais campos; cobrir com testes os cenários de "Valores padrão" e de "Configurações gravadas no arquivo de dados" e verificar que passam
- [ ] 1.3 Trocar `Constantes.DestinoSsh` por `UsuarioSshPadrao` e `HostSsh` e fazer o `MontadorComando` montar com e sem SSH, validando o usuário de novo; a `AberturaSessao` passa as opções do `EstadoDados` carregado; cobrir com testes os cenários de "Composição do comando" (os já existentes e os de SSH com `ia`, com `agente-ia` e sem SSH) e verificar que passam

## 2. Pasta de status

- [ ] 2.1 Resolver a pasta em `App.OnStartup` na ordem `--pasta-status`, configuração e padrão, e criá-la quando não existir (falha cai no aviso de pasta indisponível); extrair a resolução para uma função do Core com testes de ordem, de criação e de criação negada, e verificar que passam

## 3. Tela

- [ ] 3.1 Criar o `ConfiguracoesWindow` (pasta com "Escolher pasta…", "Abrir pelo SSH" e usuário desabilitado com o SSH desligado, erro junto do campo, Enter salva, Esc cancela) e `IInteracao.DialogoConfiguracoes`; avisar ao salvar uma pasta diferente da atual que ela vale ao reabrir; com o arquivo ilegível, mostrar que não é possível salvar
- [ ] 3.2 Acrescentar a ação "Configurações" (ícone de engrenagem com nome acessível) na barra superior da janela principal; abrir o painel com os dados de exemplo e conferir o diálogo, o campo desabilitado, os erros de usuário e de pasta e o aviso da pasta

## 4. Documentação

- [ ] 4.1 Atualizar o PRD (o isolamento pelo usuário `ia` passa a ser o padrão; RS-7 com o SSH opcional, o usuário configurável e o host `localhost`) e o guia de instalação (o `-PastaStatus` do hook deve ser a pasta configurada, com as mesmas permissões da pasta padrão); verificar que os caminhos e nomes citados existem
- [ ] 4.2 Atualizar o README com a tela de configurações; verificar que os passos descritos funcionam como escritos

## 5. Verificação manual

- [ ] 5.1 **[manual]** Configurar outra pasta de status inexistente, reabrir o painel e conferir que ela foi criada; configurar o hook do `ia` com essa pasta e conferir que uma sessão nova aparece no painel
- [ ] 5.2 **[manual]** Com o SSH ligado e o usuário `ia`, abrir e retomar uma sessão do Claude e uma do Codex; conferir que rodam como `ia`
- [ ] 5.3 **[manual]** Desligar "Abrir pelo SSH", abrir uma sessão e conferir que a aba roda como o usuário principal, sem `ssh`; religar e conferir que a próxima sessão volta a usar o SSH

## Workflow follow-up

- Implementar depois da change `refatorar-estrutura`.
- Arquivar esta change depois da verificação manual.
