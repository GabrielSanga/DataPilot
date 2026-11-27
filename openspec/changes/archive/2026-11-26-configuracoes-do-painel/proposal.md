# Proposal

## Why

O uso do SSH e o usuário do SSH estão fixos no código (`ia@localhost`). Quem instala o painel numa máquina com outro nome de usuário isolado, ou sem o usuário isolado, precisa recompilar. Além disso, se a pasta de status ainda não existe, o painel só mostra "pasta indisponível" até alguém criá-la à mão.

## What Changes

- **Tela de configurações**, aberta a partir da janela principal, com três campos:
  - a pasta de status das sessões de IA, só para leitura, mostrando a pasta em uso;
  - "Abrir pelo SSH", ligado por padrão;
  - o usuário do SSH, `ia` por padrão.
- **A pasta de status é criada ao abrir o painel** quando ainda não existe. A pasta continua sendo a padrão (`C:\ProgramData\Painel\sessoes`) ou a informada em `--pasta-status`. Ela não é configurável.
- **Execução sem SSH.** Com "Abrir pelo SSH" desligado, a aba do Windows Terminal chama o `abrir.ps1` direto, como o usuário que usa o painel. Com ele ligado, usa `ssh -t <usuário>@localhost`. O host continua fixo em `localhost`. As opções de SSH valem já na próxima sessão aberta.
- **O usuário do SSH é validado** ao gravar e de novo ao montar o comando, porque entra como argumento do `ssh`.
- As configurações são gravadas no arquivo de dados do painel, no qual o usuário `ia` não tem acesso, com a mesma gravação segura dos trabalhos.
- **BREAKING (segurança):** com o SSH desligado, o agente roda sem o isolamento do usuário `ia`, com os acessos de quem usa o painel. O PRD e a regra RS-7 passam a tratar o SSH como padrão, não como obrigação.

## Capabilities

### New Capabilities

- `configuracoes`: tela de configurações do painel, valores padrão, validação, onde são gravadas e quando passam a valer.

### Modified Capabilities

- `lista-de-sessoes`: a pasta de status é criada ao abrir o painel se não existir.
- `lancamento-de-sessao`: a composição do comando passa a depender de "Abrir pelo SSH" e do usuário configurado.
- `abertura-de-sessao`: o `abrir.ps1` também é chamado diretamente, sem SSH.

## Impact

- **Código:**
  - `Constantes`, `MontadorComando` e `AberturaSessao`;
  - `DadosPainel`, para ler e gravar as configurações;
  - `App.xaml.cs`, para criar a pasta de status;
  - `MainWindow`, para o botão de configurações, e um diálogo novo.
- **Testes:** montador do comando com e sem SSH, validação do usuário, leitura e gravação das configurações e criação da pasta de status.
- **Documentação:** PRD (princípio do isolamento e RS-7), guia de instalação (pasta criada pelo painel) e README.
- **Dados:** o `demandas.json` ganha uma seção opcional `configuracoes`. Arquivos sem ela continuam válidos e usam os padrões.
- **Ordem:** implementar depois da change `refatorar-estrutura`, que mexe nos mesmos arquivos.
