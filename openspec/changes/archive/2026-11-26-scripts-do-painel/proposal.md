# Proposal

## Why

O `abrir.ps1` e o `hook.ps1` são copiados à mão para `C:\Painel`, como administrador, a cada versão. Quando a cópia é esquecida, o painel novo chama um script antigo e falha de formas difíceis de entender (a Etapa 4d precisou de um aviso no guia por causa disso).

## What Changes

- **O painel instala e atualiza os scripts ao abrir.** Ele leva o `abrir.ps1` e o `hook.ps1` da própria versão embutidos no executável.
  - Se `C:\Painel` não existe, o painel cria a pasta com as permissões certas e grava os dois scripts.
  - Se um script falta ou está diferente da versão do painel, o painel o substitui.
  - Os demais arquivos da pasta (`exemplos`, `testes`) não são tocados.
- **Pasta não confiável é recusada.** Isso vale quando `C:\Painel` ou um script é atalho ou junção, quando o dono não é o sistema, os administradores ou o usuário que usa o painel, ou quando outra conta pode gravar na pasta. Nesses casos o painel não grava nada e mostra um aviso.
- **Falha não impede o uso.** Sem permissão para gravar, ou com erro de disco, o painel mostra um aviso e continua funcionando com os scripts que já estão lá.
- **Permissões de `C:\Painel` revistas.** O usuário que usa o painel passa a poder gravar na pasta, para o painel atualizar os scripts. O usuário `ia` continua só lendo e executando.
- A configuração dos hooks nos perfis dos agentes continua manual.

## Capabilities

### New Capabilities

- `scripts-do-painel`: instalação e atualização do `abrir.ps1` e do `hook.ps1` em `C:\Painel` pelo próprio painel, com verificação da pasta.

### Modified Capabilities

- `abertura-de-sessao`: as permissões de `C:\Painel` passam a permitir que o usuário principal grave; o `ia` continua sem poder alterar os scripts.

## Impact

- **Código:**
  - `DataPilot.Core`: os dois scripts embutidos como recurso e uma classe de instalação com a verificação da pasta.
  - `App.xaml.cs`: a instalação roda ao abrir.
  - `MainViewModel`: mostra o aviso.
  - `Constantes`: a pasta `C:\Painel`.
- **Testes:** instalação numa pasta temporária (criação, atualização, arquivo igual intocado, pasta com junção, pasta que outra conta pode gravar e permissões depois da criação).
- **Documentação:** guia de instalação (o passo de copiar os scripts vira conferência e a permissão de gravação do usuário principal entra no passo 1) e README.
- **Instalações existentes:** a `C:\Painel` criada pelo guia só deixa administradores gravarem. Enquanto o usuário principal não receber permissão de gravação, o painel avisa quando um script está desatualizado e não o substitui.
- **Ordem:** implementar depois da `configuracoes-do-painel`, que também mexe em `App.xaml.cs`.
