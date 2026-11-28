## MODIFIED Requirements

### Requirement: Pasta de status definida na inicialização
O painel SHALL ler `C:\ProgramData\Painel\sessoes` por padrão e SHALL aceitar outra pasta informada pelo usuário na linha de comando ao iniciar. Se a pasta não existir, o painel SHALL criá-la antes de começar a ler; se não conseguir, SHALL mostrar o aviso de pasta indisponível. O painel SHALL NOT criar arquivos dentro dela.

#### Scenario: Início sem parâmetros
- **WHEN** o painel é iniciado sem parâmetros
- **THEN** ele lê `C:\ProgramData\Painel\sessoes`

#### Scenario: Pasta informada na linha de comando
- **WHEN** o painel é iniciado com `--pasta-status` seguido de um caminho
- **THEN** ele lê as sessões dessa pasta

#### Scenario: Pasta que não existe
- **WHEN** o painel é iniciado e a pasta de status não existe
- **THEN** a pasta é criada, vazia, e a janela indica que não há sessões

#### Scenario: Pasta que não pode ser criada
- **WHEN** a pasta de status não existe e o usuário não tem permissão para criá-la
- **THEN** a janela mostra o aviso de pasta indisponível
