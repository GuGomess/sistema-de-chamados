# 🎫 Sistema de Chamados

Sistema web de **help desk / service desk** para registro, atendimento e acompanhamento de chamados de suporte. Clientes abrem solicitações, técnicos as atendem dentro de prazos de **SLA**, e administradores acompanham tudo por um **dashboard** com gráficos e métricas — com histórico completo, comentários, anexos, avaliações e notificações em tempo real em cada chamado.

O projeto está funcional de ponta a ponta: backend (.NET/ASP.NET Core) e frontend (Angular) implementados, com suíte de testes automatizados em ambos e deploy containerizado via Docker Compose.

---

## 🧱 Stack Tecnológica

O projeto é organizado em camadas bem separadas — cada tecnologia tem um papel claro:

### 🎨 Frontend
| Tecnologia | Papel |
|-----------|-------|
| **Angular 22** (standalone components + signals) | SPA responsável por toda a interface: autenticação, autoatendimento (cadastro/perfil), abertura e listagem de chamados com filtros, detalhe do chamado (comentários, anexos, avaliação), dashboard com gráfico (Chart.js/ng2-charts) e telas administrativas (usuários, departamentos, categorias). Consome a API REST do backend e recebe atualizações em tempo real via SignalR. |

### ⚙️ Backend
| Tecnologia | Papel |
|-----------|-------|
| **.NET 9 / ASP.NET Core Web API** | API REST que concentra as regras de negócio: autenticação JWT, perfis/permissões, departamentos, ciclo de vida dos chamados, cálculo e monitoramento automático de SLA, comentários, anexos, avaliações, notificações e métricas. Expõe também um hub **SignalR** para eventos em tempo real. |

### 🗄️ Banco de Dados
| Tecnologia | Papel |
|-----------|-------|
| **PostgreSQL** (via EF Core / Npgsql) | Persistência dos dados: usuários, departamentos, chamados, histórico, comentários, anexos, avaliações, notificações e configurações de SLA. Schema versionado via *migrations* do Entity Framework Core. |

### 🐳 Infraestrutura / DevOps
| Tecnologia | Papel |
|-----------|-------|
| **Docker** | Containerização dos três serviços (`db`, `backend`, `frontend`) para padronizar ambientes e facilitar o deploy. Orquestração via `docker-compose.yml`. |

---

## ✨ Funcionalidades

| Funcionalidade | Descrição | Camada principal |
|---|---|---|
| 🔐 **Autenticação** | Login com JWT (access token + refresh token emitidos) e autocadastro de cliente (`/registrar`). | Frontend + Backend |
| 👥 **Perfis de acesso** | Três papéis com permissões distintas: **Administrador**, **Técnico** e **Cliente**, aplicados em toda a API via policies de autorização. | Backend (autorização) |
| 🏢 **Departamentos** | Técnicos são segmentados por departamento (ex.: HelpDesk, Dev, Infra); o HelpDesk faz a triagem inicial e tem visão ampla, os demais só veem os próprios chamados. Chamados podem ser encaminhados entre departamentos ou devolvidos ao HelpDesk. | Backend |
| 📝 **Ciclo de vida do chamado** | Abertura, atribuição/auto-atribuição de técnico, mudança de status, edição de conteúdo, reabertura, encaminhamento entre departamentos, fechamento pelo cliente — cada ação gera um evento de histórico. | Frontend + Backend |
| ⏱️ **SLA** | Prazos de resposta/resolução calculados por prioridade, com um serviço em background (`SlaMonitorService`) que reavalia periodicamente a situação (em dia/em risco/vencido) e dispara notificações. Prazos podem ser ajustados manualmente com justificativa. | Backend |
| 📜 **Histórico** | Trilha de auditoria cronológica de todas as mudanças de status, departamento, atribuição e ajustes de prazo do chamado. | Backend + Database |
| 💬 **Comentários** | Mensagens (públicas ou notas internas visíveis só a técnico/administrador) dentro do chamado, com edição pelo autor e ocultação por administrador; suporta anexos junto do comentário. | Frontend + Backend |
| 📎 **Upload de anexos** | Anexo de arquivos ao chamado ou a um comentário, com validação de extensão/tamanho, substituição e download. | Frontend + Backend |
| ⭐ **Avaliações** | Cliente avalia (nota + comentário, pública ou só para administradores) o atendimento de um chamado resolvido; suporta uma avaliação por ciclo de resolução (reabrir e resolver de novo libera nova avaliação). | Frontend + Backend |
| 🔔 **Notificações** | Notificações automáticas (SLA em risco/vencido, mudança de status, atribuição, reabertura, fechamento pelo cliente, nova avaliação) entregues via API e em tempo real via SignalR. | Backend |
| ⚡ **Tempo real** | Hub SignalR (`/hubs/chamados`) transmite atualizações de chamado e notificações para quem está com a tela aberta, sem precisar recarregar. | Frontend + Backend |
| 📊 **Dashboard e métricas** | Indicadores de SLA (em risco/vencidos), distribuição de chamados por status (com gráfico) e produtividade por técnico (chamados atribuídos/resolvidos, tempo médio de resolução). | Frontend + Backend |
| 🛠️ **Administração** | Gestão de usuários (criar, ativar/desativar, redefinir senha, vincular departamentos), departamentos e categorias diretamente pela interface. | Frontend + Backend |
| 👤 **Autoatendimento** | Qualquer usuário autenticado edita o próprio nome/e-mail e troca a própria senha (com confirmação da senha atual). | Frontend + Backend |

---

## 👥 Perfis de Usuário

- **Administrador** — gerencia usuários, departamentos e categorias; vê e movimenta todos os chamados; acompanha SLA e métricas globais.
- **Técnico** — atende chamados do(s) departamento(s) a que pertence (ou todos, se for do HelpDesk), assume/recebe atribuições, comenta, ajusta prazos e resolve dentro do SLA.
- **Cliente** — abre chamados, acompanha o andamento, comenta, anexa arquivos, fecha o próprio chamado e avalia o atendimento.

---

## 📂 Estrutura do Repositório

```
sistema-de-chamados/
├── frontend/                      # Aplicação Angular
│   └── src/app/
│       ├── core/                  # Guards, interceptors, services, models
│       └── pages/                 # Login, registrar, perfil, dashboard, chamados/*, admin/*
├── backend/                       # API REST em .NET (ASP.NET Core)
│   └── src/
│       ├── Chamados.Api/          # Projeto Web API (controllers, services, migrations)
│       └── Chamados.Api.Tests/    # Testes automatizados (xUnit): integração + unitários
├── database/                      # Modelagem (ER) do banco — ver MODELO-ER.md
├── docs/
│   ├── openapi.yaml               # Contrato da API REST (OpenAPI 3.1)
│   └── wireframes.html            # Wireframes das telas
├── docker-compose.yml             # Orquestração dos containers (db, backend, frontend)
├── .env.example                   # Variáveis de ambiente de referência
└── README.md
```

📐 **Modelagem de dados:** ver [`database/MODELO-ER.md`](database/MODELO-ER.md) — diagrama ER e dicionário de dados das entidades.

🔌 **Contrato da API:** ver [`docs/openapi.yaml`](docs/openapi.yaml) — especificação OpenAPI 3.1 (endpoints, payloads e códigos de status). Abre em qualquer Swagger UI / Redoc. Em desenvolvimento, o backend também expõe documentação interativa em `/swagger` (ver [`backend/README.md`](backend/README.md)).

🖼️ **Wireframes:** ver [`docs/wireframes.html`](docs/wireframes.html) — esboços low-fidelity das telas principais. Abra no navegador.

---

## 🚀 Como rodar

### Opção 1 — Docker Compose (recomendado)

Pré-requisito: **Docker** e **Docker Compose**.

```bash
cp .env.example .env
# edite .env — em especial POSTGRES_PASSWORD e Jwt__Key

docker compose up -d --build
```

Isso sobe três serviços:

| Serviço | Porta padrão (host) | Descrição |
|---|---|---|
| `db` | `5432` | PostgreSQL 16 |
| `backend` | `5000` | API ASP.NET Core (`ASPNETCORE_ENVIRONMENT=Development` por padrão em `.env.example`, habilita `/swagger`) |
| `frontend` | `4200` | Angular buildado, servido por nginx (que também faz proxy de `/api` e `/hubs` para o backend) |

As portas expostas no host, credenciais do banco, chave JWT e limite de upload são configuráveis via `.env` — ver comentários em [`.env.example`](.env.example). Os dados do Postgres e os arquivos enviados persistem em volumes nomeados (`db-data`, `uploads-data`).

> Ao acessar de outra máquina da rede (ex.: Docker rodando num host remoto), troque `localhost` pelo IP dessa máquina nas URLs abaixo — o `docker compose` não faz isso automaticamente.

### Opção 2 — Desenvolvimento local (sem Docker)

Pré-requisitos: **.NET SDK 9**, **Node.js 20+** (validado com v24) e **PostgreSQL** acessível (local ou remoto).

**Backend:**

```bash
cd backend
dotnet run --project src/Chamados.Api
```

A API sobe em `http://localhost:5000` (ver `Properties/launchSettings.json`/variáveis de ambiente para a connection string, `Jwt__Key`, etc. — mesmas chaves do `.env.example`). Em modo `Development`, o Swagger UI fica em `http://localhost:5000/swagger`.

**Frontend:**

```bash
cd frontend
npm install
npm start
```

A aplicação sobe em `http://localhost:4200` e usa [`proxy.conf.json`](frontend/proxy.conf.json) para redirecionar `/api` ao backend em `http://localhost:5000`.

Detalhes específicos de cada camada (variáveis de ambiente, seed de usuário admin, estrutura de pastas) estão em [`backend/README.md`](backend/README.md) e [`frontend/README.md`](frontend/README.md).

---

## ✅ Como rodar os testes

### Backend (xUnit)

```bash
cd backend
dotnet test
```

O projeto [`Chamados.Api.Tests`](backend/src/Chamados.Api.Tests) cobre:
- **Testes unitários** (`Unit/`) — ex.: cálculo de situação de SLA. Não têm dependências externas e rodam em qualquer ambiente.
- **Testes de integração** (`Integration/`) — sobem a API real (`WebApplicationFactory`) contra um PostgreSQL efêmero via **Testcontainers**, cobrindo autenticação, ciclo de vida completo do chamado, métricas e uma bateria de testes de segurança contra SQL injection/XSS nos filtros de texto livre.

> **Ressalva sobre Docker remoto:** os testes de integração precisam de um daemon Docker **diretamente alcançável** pelo processo `dotnet test` (Testcontainers cria e conecta a um container Postgres real). Se o Docker do seu ambiente estiver configurado como um *context* remoto via SSH (`docker context ls` mostrando um endpoint `ssh://...`), o cliente usado pelo Testcontainers.NET não sabe resolver esse esquema e a suíte de integração falha ao subir o container — isso está documentado no próprio código (`Chamados.Api.Tests/Integration/Support/IntegrationTestFixture.cs`). Nesse caso, rode só os testes unitários localmente (`dotnet test --filter FullyQualifiedName~Unit`) e deixe a suíte completa para a CI (GitHub Actions, que roda em `ubuntu-latest` com Docker nativo — ver [`.github/workflows/ci.yml`](.github/workflows/ci.yml)) ou para uma máquina com acesso direto (TCP/socket local) ao daemon Docker.

### Frontend (Vitest via Angular CLI)

```bash
cd frontend
npm test
```

Cobre guards de rota, serviços HTTP (auth, chamados, métricas) e componentes de página (login, dashboard, listagem/detalhe de chamado), usando `jsdom`.

---

## 📦 Deploy

O deploy de referência é **containerizado**: `docker-compose.yml` builda as imagens de `backend/Dockerfile` (multi-stage, SDK → runtime `aspnet:9.0-alpine`) e `frontend/Dockerfile` (multi-stage, Node → `nginx:alpine` servindo o build de produção do Angular), além de subir um container `postgres:16-alpine`. O `nginx` do frontend serve os arquivos estáticos e faz proxy reverso de `/api/*` (REST) e `/hubs/*` (SignalR, com upgrade de conexão para WebSocket) para o backend — ver [`frontend/nginx.conf`](frontend/nginx.conf). Todos os três serviços declaram `HEALTHCHECK`, e o backend só sobe depois que o `db` estiver saudável (`depends_on: condition: service_healthy`).

Para "subir" o ambiente (local ou em qualquer host com Docker), o fluxo é o mesmo da seção **Como rodar** acima: configurar `.env` a partir de `.env.example` e `docker compose up -d --build`.
