# Spec Delta

## Purpose

Mantém o `abrir.ps1` e o `hook.ps1` em `C:\Painel` sempre na versão do painel instalado, sem cópia manual, e sem nunca gravar numa pasta que outra conta, como a do usuário `ia`, possa controlar.

## ADDED Requirements

### Requirement: Instalação dos scripts ao abrir
Ao abrir, o painel SHALL garantir que `C:\Painel\abrir.ps1` e `C:\Painel\hook.ps1` tenham exatamente o conteúdo da versão do painel. Se `C:\Painel` não existir, o painel SHALL criá-la sem herdar permissões da pasta pai, dando controle total ao sistema e aos administradores, gravação ao usuário que usa o painel e só leitura e execução aos demais usuários.

#### Scenario: Primeira execução
- **WHEN** o painel é aberto e `C:\Painel` não existe
- **THEN** a pasta é criada com essas permissões e contém o `abrir.ps1` e o `hook.ps1` da versão do painel

#### Scenario: Usuário ia depois da criação
- **WHEN** o painel criou `C:\Painel` e um processo do usuário `ia` tenta alterar `C:\Painel\hook.ps1`
- **THEN** a operação é negada

### Requirement: Scripts mantidos atualizados
A cada abertura, o painel SHALL comparar o conteúdo de cada script com o da sua versão e SHALL substituir só o que faltar ou estiver diferente. Um script igual SHALL NOT ser regravado. Outros arquivos e pastas de `C:\Painel` SHALL NOT ser tocados.

#### Scenario: Versão nova do painel
- **WHEN** o painel é aberto e o `abrir.ps1` em `C:\Painel` difere do embutido
- **THEN** o `abrir.ps1` é substituído e o `hook.ps1`, igual, não é regravado

#### Scenario: Outros arquivos
- **WHEN** `C:\Painel` tem as pastas `exemplos` e `testes`
- **THEN** elas continuam como estavam

### Requirement: Pasta não confiável é recusada
O painel SHALL NOT gravar em `C:\Painel` quando a pasta ou um dos scripts for atalho ou junção, quando o dono da pasta não for o sistema, os administradores ou o usuário que usa o painel, ou quando uma permissão da pasta der direito de gravar a qualquer outra conta. Nesses casos o painel SHALL mostrar um aviso que explica o motivo.

#### Scenario: Pasta que outra conta pode gravar
- **WHEN** `C:\Painel` dá permissão de modificar a "Usuários autenticados"
- **THEN** nenhum script é gravado e o painel avisa que a pasta não é confiável

#### Scenario: Pasta que é junção
- **WHEN** `C:\Painel` é uma junção para outra pasta
- **THEN** nenhum script é gravado e o painel avisa que a pasta não é confiável

### Requirement: Falha na instalação não impede o uso
Se não conseguir criar a pasta ou gravar um script, o painel SHALL mostrar um aviso com o motivo e SHALL continuar funcionando, usando os scripts que já existirem.

#### Scenario: Sem permissão de gravação
- **WHEN** o `hook.ps1` em `C:\Painel` está desatualizado e o usuário que usa o painel não pode gravar na pasta
- **THEN** o painel avisa que não conseguiu atualizar os scripts, e a lista, abrir e retomar continuam funcionando
