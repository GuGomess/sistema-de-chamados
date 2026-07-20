# 🎫 Sistema de Chamados

Sistema web de **help desk / service desk** para registro, atendimento e acompanhamento de chamados de suporte. Clientes abrem solicitações, técnicos as atendem dentro de prazos de **SLA**, e administradores acompanham tudo por um **dashboard** — com histórico completo, comentários e anexos em cada chamado.

> ⚠️ Projeto em fase inicial de setup. Algumas decisões de stack ainda estão em aberto (marcadas como *a definir*).

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
| **.NET (ASP.NET Core Web API)** *ou* **Node.js** *(a definir)* | API REST que concentra as regras de negócio: autenticação, controle de perfis/permissões, cálculo e monitoramento de SLA, ciclo de vida dos chamados, comentários e uploads. |

### 🗄️ Banco de Dados
| Tecnologia | Papel |
|-----------|-------|
| **SQL Server** *ou* **PostgreSQL** *(a definir)* | Persistência dos dados: usuários, chamados, histórico de status, comentários, metadados dos arquivos e configurações de SLA. |

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
├── backend/            # API REST (.NET ou Node.js)
├── database/           # Scripts / migrations do banco
├── docker-compose.yml  # Orquestração dos containers
└── README.md
```
> Estrutura proposta — será ajustada conforme o projeto evolui.

---

## 🚧 Status do Projeto

Fase inicial: definição de stack e setup do repositório.
