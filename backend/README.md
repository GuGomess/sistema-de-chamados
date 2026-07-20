# Backend — API REST (.NET / ASP.NET Core)

API REST que concentra as regras de negócio do Sistema de Chamados: autenticação (JWT), controle de perfis/permissões, cálculo e monitoramento de SLA, ciclo de vida dos chamados, comentários e uploads.

> 📌 Pasta ainda sem código. O scaffold da API (.NET) será feito em tarefa própria (ver board no Trello). Este README é um placeholder da estrutura do repositório.

## Stack prevista

- **.NET (ASP.NET Core Web API)**
- **Entity Framework Core** (persistência e migrations) com provider **Npgsql**
- **PostgreSQL** como SGBD — ver modelagem em [`../database/MODELO-ER.md`](../database/MODELO-ER.md)
- Autenticação **JWT** e documentação **Swagger/OpenAPI**
- Contrato da API: [`../docs/openapi.yaml`](../docs/openapi.yaml)
