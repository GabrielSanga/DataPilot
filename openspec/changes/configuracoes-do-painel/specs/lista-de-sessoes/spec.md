## MODIFIED Requirements

### Requirement: Pasta de status definida na inicialização
Ao iniciar, o painel SHALL ler a pasta informada com `--pasta-status` na linha de comando; sem ela, a pasta das configurações; sem configuração, `C:\ProgramData\Painel\sessoes`. Se a pasta escolhida não existir, o painel SHALL criá-la antes de começar a ler; se não conseguir, SHALL mostrar o aviso de pasta indisponível. O painel SHALL NOT criar arquivos dentro dela.

#### Scenario: Início sem parâmetros
- **WHEN** o painel é iniciado sem parâmetros e sem pasta de status configurada
- **THEN** ele lê `C:\ProgramData\Painel\sessoes`

#### Scenario: Pasta informada na linha de comando
- **WHEN** o painel é iniciado com `--pasta-status` seguido de um caminho
- **THEN** ele lê as sessões dessa pasta, mesmo com outra pasta nas configurações

#### Scenario: Pasta configurada
- **WHEN** o painel é iniciado sem parâmetros e com `D:\Painel\sessoes` configurada
- **THEN** ele lê as sessões de `D:\Painel\sessoes`

#### Scenario: Pasta configurada que não existe
- **WHEN** o painel é iniciado com `D:\Painel\sessoes` configurada e a pasta não existe
- **THEN** a pasta é criada, vazia, e a janela indica que não há sessões

#### Scenario: Pasta que não pode ser criada
- **WHEN** a pasta configurada não existe e o usuário não tem permissão para criá-la
- **THEN** a janela mostra o aviso de pasta indisponível
