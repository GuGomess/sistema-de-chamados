# 🎫 Sistema de Chamados

Sistema web de **help desk / service desk** para registro, atendimento e acompanhamento de chamados de suporte. Clientes abrem solicitações, técnicos as atendem dentro de prazos de **SLA**, e administradores acompanham tudo por um **dashboard** — com histórico completo, comentários e anexos em cada chamado.

> ⚠️ Projeto em fase inicial de setup. A stack já está definida (ver seção **Decisões de Arquitetura**); a implementação começa na Fase 1.

---

## 🧱 Stack Tecnológica

O projeto é organizado em camadas bem separadas — cada tecnologia tem um papel claro:

### 🎨 Frontend
| Tecnologia | Papel |
|-----------|-------|
| **Angular** | Aplicação SPA (Single Page Application) responsável por toda a interface: login, abertura e listagem de chamados, dashboard e painéis específicos por perfil. Consome a API REST do backend. |

### ⚙️ Backend
| Tecnologia | Papel |
|-----------|-------|
| **.NET (ASP.NET Core Web API)** | API REST que concentra as regras de negócio: autenticação, controle de perfis/permissões, cálculo e monitoramento de SLA, ciclo de vida dos chamados, comentários e uploads. |

### 🗄️ Banco de Dados
| Tecnologia | Papel |
|-----------|-------|
| **PostgreSQL** | Persistência dos dados: usuários, chamados, histórico de status, comentários, metadados dos arquivos e configurações de SLA. |

### 🐳 Infraestrutura / DevOps
| Tecnologia | Papel |
|-----------|-------|
| **Docker** | Containerização dos serviços (frontend, backend e banco) para padronizar ambientes e facilitar o deploy. Orquestração via `docker-compose`. |

---

## ✨ Funcionalidades

| Funcionalidade | Descrição | Camada principal |
|---|---|---|
| 🔐 **Login** | Autenticação de usuários (ex.: JWT) e controle de sessão. | Frontend + Backend |
| 👥 **Perfis de acesso** | Três papéis com permissões distintas: **Administrador**, **Técnico** e **Cliente**. | Backend (autorização) |
| 📝 **Abertura de chamados** | Cliente registra uma solicitação com categoria, descrição e prioridade. | Frontend + Backend |
| ⏱️ **SLA** | Definição e monitoramento de prazos de atendimento/resolução por prioridade, com alerta de vencimento. | Backend |
| 📜 **Histórico** | Registro cronológico de todas as mudanças de status e ações em cada chamado. | Backend + Database |
| 💬 **Comentários** | Troca de mensagens entre cliente e técnico dentro do chamado. | Frontend + Backend |
| 📎 **Upload de arquivos** | Anexo de imagens/documentos ao chamado (ex.: prints de erro). | Frontend + Backend |
| 📊 **Dashboard** | Painéis com indicadores: chamados por status, SLA em risco, produtividade dos técnicos, etc. | Frontend + Backend |

---

## 👥 Perfis de Usuário

- **Administrador** — gerencia usuários, configura SLAs e categorias, e enxerga todos os chamados e indicadores.
- **Técnico** — atende os chamados atribuídos, atualiza status, comenta e resolve dentro do SLA.
- **Cliente** — abre chamados, acompanha o andamento, comenta e anexa arquivos.

---

## 📂 Estrutura Planejada do Repositório

```
sistema-de-chamados/
├── frontend/           # Aplicação Angular
├── backend/            # API REST em .NET (ASP.NET Core)
├── database/           # Modelagem (ER) e, futuramente, migrations do banco
├── docker-compose.yml  # Orquestração dos containers
└── README.md
```
> Estrutura proposta — será ajustada conforme o projeto evolui.

📐 **Modelagem de dados:** ver [`database/MODELO-ER.md`](database/MODELO-ER.md) — diagrama ER e dicionário de dados das entidades.

🔌 **Contrato da API:** ver [`docs/openapi.yaml`](docs/openapi.yaml) — especificação OpenAPI 3.1 (endpoints, payloads e códigos de status). Abre em qualquer Swagger UI / Redoc.

🖼️ **Wireframes:** ver [`docs/wireframes.html`](docs/wireframes.html) — esboços low-fidelity das 5 telas principais (login, abertura, listagem, detalhe e dashboard). Abra no navegador.

---

## 🧭 Decisões de Arquitetura

Registro das decisões técnicas relevantes do projeto (ADR simplificado).

| # | Decisão | Escolha | Justificativa |
|---|---------|---------|---------------|
| 1 | **Stack do backend** | **.NET (ASP.NET Core Web API)** | Tipagem forte do C# favorece regras de negócio bem definidas (SLA, perfis/permissões, ciclo de vida do chamado); **Entity Framework Core** para persistência e migrations; autenticação **JWT** e documentação **Swagger/OpenAPI** com suporte de primeira classe no ecossistema. |
| 2 | **SGBD** | **PostgreSQL** | Banco relacional gratuito e open-source, sem custo de licença; imagem Docker leve (`postgres:16-alpine`) que facilita padronizar o ambiente via `docker-compose`; integra-se ao EF Core pelo provider **Npgsql**. Atende bem à modelagem relacional dos chamados (histórico, comentários, SLA) e é portável para qualquer ambiente de deploy. |

---

## 🚧 Status do Projeto

Fase inicial de planejamento. **Backend definido: .NET (ASP.NET Core Web API)** · **SGBD definido: PostgreSQL.** Próxima decisão em aberto: contrato da API REST. Já há um `docker-compose.yml` com o serviço de banco e um `.env.example` de referência.
