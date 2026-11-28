# Spec Delta

## REMOVED Requirements

### Requirement: Instalação dos scripts ao abrir
**Reason**: O `abrir.ps1` e o `hook.ps1` só existiam para o modo Windows, que foi removido. No container, os scripts vêm na imagem, em `/opt/painel`.
**Migration**: Nenhuma ação no painel. A pasta `C:\Painel` deixa de ser usada e pode ser apagada à mão, como descreve o guia do container.

### Requirement: Scripts mantidos atualizados
**Reason**: Não há mais scripts do Windows para manter. Os do container são atualizados ao reconstruir a imagem.
**Migration**: Para atualizar os scripts do container, use `docker compose up -d --build`.

### Requirement: Pasta não confiável é recusada
**Reason**: O painel deixa de gravar em `C:\Painel`.
**Migration**: Nenhuma.

### Requirement: Falha na instalação não impede o uso
**Reason**: Não há mais instalação de scripts ao abrir o painel, nem aviso de falha dela.
**Migration**: Nenhuma.
