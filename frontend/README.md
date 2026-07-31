# Frontend — Aplicação Angular

Aplicação SPA (Single Page Application) responsável por toda a interface do Sistema de Chamados: autenticação, autocadastro, abertura e listagem de chamados com filtros, detalhe do chamado (comentários, anexos, histórico, avaliação), dashboard com gráfico e indicadores, telas administrativas (usuários, departamentos, categorias) e autoatendimento de perfil. Consome a API REST do backend e recebe atualizações em tempo real via SignalR (notificações e mudanças no chamado refletem na tela sem recarregar).

Gerado com [Angular CLI](https://github.com/angular/angular-cli) **v22** (componentes standalone + signals).

## Stack

- **Angular 22** (SPA, standalone components, signals)
- **SCSS** para estilos
- **Chart.js** + **ng2-charts** para o gráfico de chamados por status no dashboard
- **@microsoft/signalr** — cliente do hub em tempo real (`RealtimeService`)
- **Vitest** (via Angular CLI) para testes unitários/componentes
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
npm test           # testes unitários (Vitest via Angular CLI)
```

## Estrutura

```
src/app/
  app.ts / app.html / app.scss   # shell da aplicação: header com navegação por
                                  #   perfil, sino de notificações (badge de não
                                  #   lidas) e <router-outlet>
  app.routes.ts                  # rotas (lazy-loaded via loadComponent)
  core/
    guards/                      # authGuard (autenticação + roles por rota), guestGuard
    interceptors/                # auth.interceptor (anexa o JWT às requisições)
    services/                    # AuthService, ChamadoService, UsuarioService,
                                  #   DepartamentoService, MetricaService,
                                  #   NotificacaoService, RealtimeService (SignalR)
    models/                      # Tipos TS espelhando os DTOs da API
  pages/
    login/                       # Login                        → /login
    registrar/                   # Autocadastro (perfil Cliente) → /registrar
    perfil/                      # Autoatendimento (editar dados/senha) → /perfil
    dashboard/                   # Indicadores de SLA + gráfico  → /dashboard
    chamados/
      chamados-lista/            # Listagem com filtros          → /chamados
      chamado-novo/               # Abertura de chamado           → /chamados/novo
      chamado-detalhe/            # Detalhe (comentários, anexos, → /chamados/:id
                                  #   histórico, avaliação)
    admin/
      usuarios/                   # Gestão de usuários (usuarios-admin.ts)      → /admin/usuarios
      departamentos/              # Gestão de departamentos (departamentos-admin.ts) → /admin/departamentos
      categorias/                 # Gestão de categorias (categorias-admin.ts)  → /admin/categorias
src/environments/
  environment.ts                 # produção (padrão)
  environment.development.ts     # desenvolvimento
```

## Rotas

| Rota                    | Tela                                    | Acesso |
| ----------------------- | ---------------------------------------- | ------ |
| `/login`                | Login                                    | Não autenticado |
| `/registrar`            | Autocadastro de cliente                  | Não autenticado |
| `/perfil`               | Autoatendimento (nome/e-mail/senha)      | Qualquer perfil autenticado |
| `/dashboard`             | Dashboard (SLA + métricas + gráfico)     | Administrador, Técnico |
| `/chamados`              | Listagem de chamados                     | Qualquer perfil autenticado |
| `/chamados/novo`         | Abertura de chamado                      | Qualquer perfil autenticado |
| `/chamados/:id`          | Detalhe do chamado                       | Qualquer perfil autenticado (escopo aplicado pela API) |
| `/admin/usuarios`        | Gestão de usuários                       | Administrador |
| `/admin/departamentos`   | Gestão de departamentos                  | Administrador |
| `/admin/categorias`      | Gestão de categorias                     | Administrador |

O controle de acesso por papel é feito pelo `authGuard` (`data: { roles: [...] }` em cada rota) — ver [`src/app/core/guards/auth.guard.ts`](src/app/core/guards/auth.guard.ts). O menu no cabeçalho (`app.html`) também se adapta ao perfil logado (ex.: Cliente não vê o link do Dashboard nem os itens de administração).

## Ambiente e integração com a API

- A base da API fica em `environment.apiBaseUrl` (padrão `/api`).
- Em desenvolvimento, o `npm start` usa [`proxy.conf.json`](proxy.conf.json) para
  redirecionar `/api` ao backend em `http://localhost:5000`. Ajuste o `target`
  conforme a porta em que o backend ASP.NET Core estiver rodando.
- O hub SignalR fica em `environment.hubUrl` — em desenvolvimento aponta para a URL absoluta do backend (`http://localhost:5000/hubs/chamados`, já que o proxy do dev-server não cobre WebSocket); em produção usa caminho relativo (`/hubs/chamados`), servido pelo mesmo nginx do frontend.
- Todas as requisições HTTP autenticadas recebem o JWT via `auth.interceptor.ts`.

## Testes

```bash
npm test
```

Executa a suíte Vitest (via Angular CLI) em ambiente `jsdom`, cobrindo:
- Guards (`auth.guard.spec.ts`, `guest.guard.spec.ts`)
- Serviços HTTP (`auth.service.spec.ts`, `chamado.service.spec.ts`, `metrica.service.spec.ts`)
- Componentes de página (`login`, `dashboard`, `chamados-lista`, `chamado-detalhe`, `chamado-novo`, `app`)
