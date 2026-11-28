# Spec Delta

## MODIFIED Requirements

### Requirement: Vincular repositório pela pasta
"Vincular repositório" SHALL abrir a escolha de pasta do Windows na raiz dos fontes. Uma pasta filha direta da raiz cujo nome siga a regra de pasta SHALL ser gravada só pelo nome. Outra pasta SHALL ser aceita, com o caminho completo, apenas se estiver dentro de `C:\Fontes` ou de `C:\openspec\ERP`, ou for uma delas, e não for atalho nem junção. Qualquer outra pasta SHALL ser recusada com um erro que informe esses dois locais, permitindo escolher de novo.

#### Scenario: Pasta da raiz
- **WHEN** o usuário escolhe `C:\Fontes\Faturamento`
- **THEN** o repositório `Faturamento` é vinculado ao trabalho

#### Scenario: Subpasta da raiz dos fontes
- **WHEN** o usuário escolhe `C:\Fontes\Grupo\Faturamento`
- **THEN** o repositório `C:\Fontes\Grupo\Faturamento` é vinculado ao trabalho, com o caminho completo

#### Scenario: Pasta do OpenSpec
- **WHEN** o usuário escolhe `C:\openspec\ERP\Faturamento`
- **THEN** o repositório `C:\openspec\ERP\Faturamento` é vinculado ao trabalho, com o caminho completo

#### Scenario: Pasta fora da raiz
- **WHEN** o usuário escolhe `C:\Projetos\Faturamento` ou `D:\Faturamento`
- **THEN** o painel mostra que só pastas de `C:\Fontes` e `C:\openspec\ERP` podem ser usadas e não vincula nada

#### Scenario: Pasta que é junção
- **WHEN** o usuário escolhe, dentro de `C:\Fontes`, uma pasta que é atalho ou junção
- **THEN** o painel mostra o erro e não vincula nada

#### Scenario: Escolha cancelada
- **WHEN** o usuário cancela a escolha de pasta
- **THEN** nada é vinculado
