# Design

## Context

Motivação em proposal.md. Fatos que moldam a solução:
- Na raiz `C:\`, "Usuários autenticados" podem criar pastas e herdam Modificar nelas. Uma `C:\Painel` criada com a herança padrão deixaria o `ia` alterar os scripts. No modo sem SSH, o `abrir.ps1` e o `hook.ps1` rodam como o usuário principal, que é administrador nesta máquina; um script alterado pelo `ia` executaria código com esses acessos.
- O `ia` também pode criar `C:\Painel` antes do painel e ficar dono dela.
- A `C:\Painel` instalada pelo guia, nesta máquina, tem a herança desligada: o sistema e os administradores com controle total, e `Gabriel Sanga` e `IA` só com leitura e execução. Como o painel roda sem elevação, o grupo Administradores não vale para ele, e ele não consegue gravar ali.
- O `abrir.ps1` precisa continuar em ASCII. Os bytes gravados têm de ser exatamente os do repositório.

## Goals / Non-Goals

**Goals:**
- O `abrir.ps1` e o `hook.ps1` em `C:\Painel` acompanham sempre a versão do painel, sem cópia manual.
- O painel nunca grava numa pasta que outra conta possa controlar.

**Non-Goals:**
- Configurar os hooks nos perfis dos agentes, nem o do `ia` nem o do usuário principal.
- Instalar `exemplos` e `testes` (o `verificar.ps1`), que continuam no guia.
- Pedir elevação (UAC) para gravar.
- Versionar os scripts por número: a comparação é pelo conteúdo.

## Decisions

### 1. Scripts embutidos no Core

`scripts/abrir.ps1` e `scripts/hook.ps1` entram como `EmbeddedResource` no `DataPilot.Core.csproj`, com `LogicalName` fixo. Uma classe nova, `ScriptsDoPainel`, faz a instalação. Ela recebe a pasta de destino, para os testes usarem uma pasta temporária, e devolve um resultado com o aviso, se houver.

Ficar no Core permite testar a instalação com os bytes reais. Alternativa considerada: copiar os scripts ao lado do executável e lê-los do disco. Rejeitada: qualquer conta que alcance a pasta do executável poderia trocá-los antes da cópia.

### 2. Verificação antes de qualquer gravação

Na ordem:
1. Se a pasta existe e é ponto de reanálise (atalho ou junção), recusa.
2. O dono da pasta precisa ser SYSTEM, Administradores ou o usuário atual (comparação por SID).
3. Toda regra de permissão que concede algum direito de gravação (`WriteData`, `AppendData`, `Delete`, `ChangePermissions`, `TakeOwnership` e afins) precisa ser para SYSTEM, Administradores ou o usuário atual.
4. Cada script existente não pode ser ponto de reanálise.

Uma lista de permitidos é melhor que procurar o `ia`, porque o nome do usuário isolado é configurável e pode haver outras contas na máquina.

### 3. Criação com permissões explícitas

`Directory.CreateDirectory` recebe um `DirectorySecurity` com a herança cortada (`SetAccessRuleProtection(true, false)`) e estas regras, herdadas por subpastas e arquivos:
- SYSTEM e Administradores: controle total;
- o usuário atual: Modificar;
- Usuários: leitura e execução.

O usuário atual é o dono da pasta.

### 4. Gravação do script

Comparar os bytes do arquivo com os do recurso; se iguais, não fazer nada. Se diferentes ou ausentes, gravar um temporário na própria `C:\Painel` e trocar com `File.Move(temp, destino, overwrite: true)`. O arquivo novo herda as permissões da pasta, o que não acontece com o `File.Replace`, que preserva a permissão do arquivo antigo. Um hook lendo o arquivo durante a troca vê a versão antiga ou a nova, nunca metade.

### 5. Na abertura do painel

`App.OnStartup` chama a instalação antes de criar a janela, num `try`. O resultado com aviso é mostrado pela faixa de mensagens do `MainViewModel`, o mesmo `MostrarErro` usado pelas outras operações. O aviso diz o motivo e, no caso de falta de permissão, o comando `icacls` que dá gravação ao usuário atual.

## Risks / Trade-offs

- **[Instalações feitas pelo guia não deixam o usuário principal gravar]** → O aviso traz o comando `icacls` e o guia é atualizado. Até lá, o painel só avisa quando um script estiver desatualizado; com os scripts iguais, nada aparece.
- **[O usuário principal passa a poder gravar em `C:\Painel`]** → Ele é a conta confiável, e o `ia` continua só lendo. Isso não amplia o que o `ia` alcança.
- **[Um script alterado localmente, de propósito, é sobrescrito na próxima abertura]** → É o comportamento esperado: a versão do painel é a fonte. O guia passa a dizer que mudanças devem ir para o repositório.
- **[Conflito com a `configuracoes-do-painel`, que mexe em `App.xaml.cs`]** → Implementar esta change depois dela.

## Migration Plan

Na máquina já instalada, dar gravação ao usuário principal uma vez, como administrador: `icacls C:\Painel /grant "<usuário>:(OI)(CI)M"`. Para desfazer, basta remover essa permissão: o painel volta a só avisar quando um script estiver desatualizado.
