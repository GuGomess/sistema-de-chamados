---
name: next-task
description: >-
  Pega a próxima tarefa prioritária do board Trello deste projeto (sistema-chamados),
  analisa o que precisa ser feito, apresenta um plano e tira as dúvidas necessárias,
  e — somente após a confirmação do usuário — dá sequência ao desenvolvimento.
  Use quando o usuário pedir "próxima tarefa", "/next-task", "o que fazer agora"
  ou quiser continuar o desenvolvimento seguindo a ordem do Trello.
---

# next-task — próxima tarefa do Trello

Conduz o desenvolvimento deste projeto seguindo a **ordem de prioridade** do board no Trello:
lê a próxima tarefa → analisa → **relata e pergunta** → **aguarda o OK do usuário** → implementa → fecha o card.

## Contexto do board (fixo deste projeto)

- **Board:** `sistema-chamados` — https://trello.com/b/xkteidTo
- **Board ID:** `6a5e57f21f2730e5f2fc7665`
- **Workspace:** Dev
- **Lista de "feito":** `Concluido` — tarefas finalizadas ficam aqui e são **ignoradas** na priorização.

> **Prioridade** = listas da **esquerda → direita** e, dentro de cada lista, cards de **cima → baixo**.

## Passo a passo

### 1) Ler o board e achar a PRÓXIMA tarefa
- Use as ferramentas `mcp__trello__*`. Comece com `mcp__trello__set_active_board` (o Board ID acima) e depois `mcp__trello__get_lists`.
- Percorra as listas **na ordem retornada**, pulando a lista `Concluido`. Para cada lista, use `mcp__trello__get_cards_by_list_id` e ordene os cards por posição.
- **Próxima tarefa = o primeiro card** dessa varredura (primeira lista não-concluída, card do topo).
- Se as ferramentas `mcp__trello__*` **não existirem** nesta sessão, o MCP do Trello não está conectado → veja **Fallback** no fim.

### 2) Analisar (ainda sem codar)
- Leia o **nome e a descrição** do card.
- Inspecione o repositório (Read / Grep / Glob, `git log`, estrutura de pastas) para entender o que **já existe** e o que **falta** para essa tarefa.
- Verifique **dependências e decisões em aberto** (ex.: stack `.NET`/`Node.js`, SGBD `SQL Server`/`PostgreSQL`) que travem ou mudem a implementação.

### 3) Relatar e perguntar
Apresente ao usuário, de forma objetiva:
- **Qual é a tarefa** (nome do card + lista) e o **link** do card.
- **O que ela envolve** e um **plano concreto**: arquivos a criar/editar, abordagem e comandos.
- **Perguntas necessárias**: qualquer decisão que só o usuário pode tomar. Faça-as antes de começar (use a ferramenta de perguntas quando fizer sentido).
- Termine pedindo **confirmação explícita** para iniciar.

### 4) Aguardar confirmação — GATE OBRIGATÓRIO
- **Não altere nenhum arquivo antes de o usuário confirmar.**
- Se o usuário ajustar o plano, revise e confirme novamente antes de codar.

### 5) Desenvolver
- Depois do "ok", implemente a tarefa conforme o plano combinado.
- Siga o padrão do código existente.
- Rode build / lint / testes quando aplicável para validar.

### 6) Fechar a tarefa
Ao concluir, **pergunte ao usuário** se deve:
- Comentar no card o resumo do que foi feito (`mcp__trello__add_comment`).
- Mover o card para `Concluido` (`mcp__trello__move_card`).
- Commitar as mudanças — **só comite se o usuário pedir; nunca faça push sem autorização.**

## Fallback (MCP do Trello indisponível)
Se não houver ferramentas `mcp__trello__*`, o servidor MCP não conectou (após mudar as env vars é preciso **reiniciar o VSCode por completo**). Para apenas **ler** o board sem o MCP, rode via PowerShell com as credenciais das variáveis de ambiente:

```powershell
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$k=[Environment]::GetEnvironmentVariable("TRELLO_API_KEY","User").Trim()
$t=[Environment]::GetEnvironmentVariable("TRELLO_TOKEN","User").Trim()
$b="6a5e57f21f2730e5f2fc7665"
Invoke-RestMethod "https://api.trello.com/1/boards/$b/lists?cards=open&card_fields=name,desc,pos&fields=name,pos&key=$k&token=$t" |
  Where-Object { $_.name -ne 'Concluido' } |
  ForEach-Object { "== $($_.name) =="; $_.cards | Sort-Object pos | ForEach-Object { " - $($_.name)" } }
```
Isso mostra colunas e cards em ordem de prioridade. Avise que, para o fluxo completo (mover/comentar card), o ideal é reconectar o MCP.
