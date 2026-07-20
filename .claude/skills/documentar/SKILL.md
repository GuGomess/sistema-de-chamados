---
name: documentar
description: >-
  Mantém o arquivo documentacao.html na raiz do repositório: um guia visual,
  didático e autocontido (HTML+CSS inline) que (1) documenta o que é o projeto
  Sistema de Chamados e o passo a passo de "how to run" por área da stack, e
  (2) mantém uma DOCUMENTAÇÃO VIVA do que já foi feito em cada tecnologia —
  uma linha do tempo por segmento da stack. Use após qualquer alteração no
  projeto, ou quando o usuário pedir "/documentar", "documenta o que foi feito",
  "atualiza a documentação" ou algo equivalente. A cada execução, ACRESCENTA um
  registro na aba pertinente (sem apagar os anteriores).
---

# documentar — documentação viva do projeto

Mantém um único arquivo **`documentacao.html`** na **raiz do repositório** — um documento
**autocontido** (todo o CSS inline, **sem** dependências externas / CDN) que qualquer
pessoa abre com duplo clique no navegador. Ele tem **duas responsabilidades**:

1. **Guia de referência** — o que o projeto é e **como rodá-lo** passo a passo, segmentado
   por área da stack (é atualizado para refletir o estado atual a cada execução).
2. **Documentação viva ("O que foi feito")** — uma **linha do tempo por segmento da stack**
   (🎨 Frontend · ⚙️ Backend · 🗄️ Banco · 🐳 Infra · 📄 Docs & Planejamento), onde cada
   alteração vira uma **entrada datada, didática e passo a passo**. Esta parte é
   **incremental**: a cada execução você **acrescenta** uma nova entrada na aba pertinente,
   **preservando todas as anteriores**.

> O `documentacao.html` é um artefato **pessoal do desenvolvedor**, não faz parte do
> código do projeto → deve ficar **no `.gitignore`** (ver passo 6). Nunca o versione.

## Princípios-guia

- **Não invente.** Todo o conteúdo sai da inspeção real do repositório e do histórico.
  Se algo ainda não existe, diga isso explicitamente.
- **A documentação é append-only.** Nunca apague nem sobrescreva entradas já existentes
  na seção "O que foi feito". Só **adicione** novas (mais recente no topo de cada aba).
- **Sem duplicar.** Antes de adicionar, verifique se aquela alteração já tem entrada no HTML.
- **Didático e apresentável.** Cada entrada explica **o que** foi feito, **como** (passo a
  passo) e **o resultado** — como se ensinasse outro dev.

## Passo a passo da skill

### 1) Descobrir o que mudou desde a última documentação
- Rode `git log --pretty=format:'%h | %ad | %s' --date=format:'%d/%m/%Y'` e compare com as
  entradas já presentes no `documentacao.html` para achar o que ainda **não** foi documentado.
- Rode `git status` e `git diff` para captar trabalho **não commitado** (também documentável).
- Se o que mudou não estiver claro, **pergunte ao usuário** o que foi alterado (arquivos,
  intenção, comandos executados) antes de escrever.

### 2) Classificar por segmento da stack
Para cada alteração nova, identifique a **aba pertinente** (a "página" dentro do HTML):
🎨 Frontend · ⚙️ Backend · 🗄️ Banco · 🐳 Infra · 📄 Docs & Planejamento.
Uma alteração pode gerar entradas em mais de uma aba se cruzar segmentos.

### 3) Redigir a(s) nova(s) entrada(s)
Cada entrada, adicionada **no topo** da aba correspondente, contém:
- **Data** (use a data atual do contexto) e um **título** curto do que foi feito.
- **O que**: 1–2 frases de contexto.
- **Como (passo a passo)**: lista ordenada e didática dos passos/decisões, com os comandos
  ou arquivos-chave em blocos de código quando fizer sentido.
- **Resultado / como verificar**: o que passou a funcionar (ex.: URL, porta, teste que passa).

### 4) Atualizar o guia de referência
Reflita o **estado atual** nas partes de referência (não incremental — pode regenerar):
selo de status de cada área da stack, "how to run", estrutura de pastas e links.
Fontes: `README.md` (raiz), `frontend/README.md` + `frontend/package.json`,
`backend/README.md`, `database/MODELO-ER.md`, `docker-compose.yml`, `.env.example`,
`docs/openapi.yaml`, `docs/wireframes.html`.

### 5) Escrever o arquivo (preservando o histórico)
- Se **`documentacao.html` não existir**, crie-o na raiz já com a seção "O que foi feito"
  **semeada** a partir do `git log` (uma entrada por marco relevante, na aba certa).
- Se **já existir**, **leia-o** e edite preservando **todas** as entradas da seção
  "O que foi feito"; insira as novas no topo da aba pertinente e atualize só o guia de referência.

### 6) Requisitos do HTML/CSS
- **Autocontido:** um único `.html` com **todo o CSS em `<style>` inline**; **nada** de
  `<link>`/fontes/imagens externas. `<script>` inline é permitido apenas para as **abas**
  ("páginas" por segmento) e para a persistência dos checkboxes em `localStorage`.
  Precisa funcionar via `file://`.
- **Didático, responsivo**, com bom tema claro **e** escuro
  (`@media (prefers-color-scheme: dark)`), badges de status por área e cor de destaque
  própria por segmento da stack. Idioma: **português (pt-BR)**.

### 7) Garantir o `.gitignore`
- Confirme que a raiz do `.gitignore` ignora `documentacao.html` (seção "Artefatos locais
  do desenvolvedor"). Verifique com `git check-ignore documentacao.html`.

### 8) Fechar
- Informe o caminho do arquivo e que basta abri-lo no navegador (duplo clique).
- Resuma quais entradas foram **acrescentadas** e em quais abas.
- Não faça commit (o arquivo é ignorado por design).
