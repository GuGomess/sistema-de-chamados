# Backend — API REST (.NET / ASP.NET Core)

API REST que concentra as regras de negócio do Sistema de Chamados: autenticação (JWT), controle de perfis/permissões, cálculo e monitoramento de SLA, ciclo de vida dos chamados, comentários e uploads.

## Stack

- **.NET 9 / ASP.NET Core Web API** (controllers)
- **Entity Framework Core** (persistência e migrations) com provider **Npgsql** — _a integrar em tarefa própria_
- **PostgreSQL** como SGBD — ver modelagem em [`../database/MODELO-ER.md`](../database/MODELO-ER.md)
- Autenticação **JWT** — _a integrar em tarefa própria_
- Documentação **Swagger/OpenAPI** (habilitada em Development)
- Contrato da API: [`../docs/openapi.yaml`](../docs/openapi.yaml)

## Estrutura

```
backend/
  Chamados.sln
  src/
    Chamados.Api/            # Projeto Web API
      Controllers/           # Controllers REST (crescem por tarefa)
      Program.cs             # Bootstrap, pipeline e healthcheck
      appsettings.json
```

## Como rodar

Pré-requisito: **.NET SDK 9** (`dotnet --version`).

```bash
cd backend

# restaurar + compilar
dotnet build

# subir a API em modo desenvolvimento (porta 5000)
dotnet run --project src/Chamados.Api
```

A API sobe em **http://localhost:5000** — a mesma porta que o proxy do frontend
([`../frontend/proxy.conf.json`](../frontend/proxy.conf.json)) espera.

### Endpoints úteis

| Método | Rota                        | Descrição                              |
| ------ | --------------------------- | -------------------------------------- |
| GET    | `/health`                   | Healthcheck (liveness) em JSON         |
| GET    | `/swagger`                  | Swagger UI (apenas em Development)      |
| GET    | `/swagger/v1/swagger.json`  | Documento OpenAPI gerado               |

Exemplo de resposta do healthcheck:

```json
{ "status": "Healthy", "timestamp": "2026-07-20T21:57:55.6884172+00:00" }
```

> O healthcheck atual é apenas _liveness_. A verificação de dependências
> (conexão com o PostgreSQL) será adicionada junto com o EF Core / migrations.

## Configuração

Nenhum segredo é versionado. A connection string e demais configs são lidas via
variáveis de ambiente (ex.: `ConnectionStrings__DefaultConnection`) — ver
[`../.env.example`](../.env.example).
