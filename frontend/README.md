# Frontend — Aplicação Angular

Aplicação SPA (Single Page Application) responsável por toda a interface do Sistema de Chamados: login, abertura e listagem de chamados, dashboard e painéis específicos por perfil. Consome a API REST do backend.

Gerado com [Angular CLI](https://github.com/angular/angular-cli) **v22** (componentes standalone + signals).

## Stack

- **Angular 22** (SPA, standalone components)
- **SCSS** para estilos
- **Vitest** para testes unitários
- Consumo da API REST em [`../backend/`](../backend/)
- Contrato da API: [`../docs/openapi.yaml`](../docs/openapi.yaml)
- Wireframes das telas: [`../docs/wireframes.html`](../docs/wireframes.html)

## Requisitos

- Node.js 20+ (validado com v24) e npm 10+

## Comandos

```bash
npm install        # instala as dependências
npm start          # servidor de desenvolvimento em http://localhost:4200
npm run build      # build de produção em dist/
npm test           # testes unitários (Vitest)
```

## Estrutura

```
src/app/
  app.ts / app.html / app.scss   # shell da aplicação (header + <router-outlet>)
  app.routes.ts                  # rotas (lazy-loaded)
  pages/
    login/                       # Login
    dashboard/                   # Dashboard
    chamados/
      chamados-lista/            # Listagem de chamados        → /chamados
      chamado-novo/              # Abertura de chamado         → /chamados/novo
      chamado-detalhe/           # Detalhe do chamado          → /chamados/:id
src/environments/
  environment.ts                 # produção (padrão)
  environment.development.ts     # desenvolvimento
```

> As telas são **placeholders** neste scaffold. A implementação de cada uma será feita em tarefas próprias (ver board no Trello).

## Rotas

| Rota              | Tela                 |
| ----------------- | -------------------- |
| `/login`          | Login (rota inicial) |
| `/dashboard`      | Dashboard            |
| `/chamados`       | Listagem de chamados |
| `/chamados/novo`  | Abertura de chamado  |
| `/chamados/:id`   | Detalhe do chamado   |

## Ambiente e integração com a API

- A base da API fica em `environment.apiBaseUrl` (padrão `/api`).
- Em desenvolvimento, o `npm start` usa [`proxy.conf.json`](proxy.conf.json) para
  redirecionar `/api` ao backend em `http://localhost:5000`. Ajuste o `target`
  conforme a porta em que o backend ASP.NET Core estiver rodando.
