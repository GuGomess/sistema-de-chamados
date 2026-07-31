# Backend — API REST (.NET / ASP.NET Core)

API REST que concentra as regras de negócio do Sistema de Chamados: autenticação (JWT), perfis/permissões, departamentos, ciclo de vida completo dos chamados, cálculo e monitoramento automático de SLA, comentários, anexos, avaliações, notificações e métricas — além de um hub **SignalR** para atualizações em tempo real.

## Stack

- **.NET 9 / ASP.NET Core Web API** (controllers)
- **Entity Framework Core** (persistência e migrations) com provider **Npgsql**
- **PostgreSQL** como SGBD — ver modelagem em [`../database/MODELO-ER.md`](../database/MODELO-ER.md)
- Autenticação **JWT** — emissão do token em `POST /api/v1/auth/login` (e `POST /api/v1/auth/registrar`, autocadastro de cliente)
- **ASP.NET Core Identity** (`PasswordHasher`) para hash de senha
- **SignalR** (`/hubs/chamados`) para notificações e atualizações de chamado em tempo real
- Serviço em background (`IHostedService`) que reavalia periodicamente a situação de SLA dos chamados abertos
- Documentação **Swagger/OpenAPI** via Swashbuckle (habilitada em Development)
- Contrato estático da API: [`../docs/openapi.yaml`](../docs/openapi.yaml)
- Testes automatizados: [`src/Chamados.Api.Tests`](src/Chamados.Api.Tests) (xUnit)

## Estrutura

```
backend/
  Chamados.sln
  src/
    Chamados.Api/                  # Projeto Web API
      Controllers/                 # Auth, Chamados, Usuarios, Departamentos,
                                    #   Categorias, Prioridades, Slas, Status,
                                    #   Notificacoes, Metricas
      Services/                    # TokenService, EscopoChamadoService,
                                    #   SlaMonitorService (background), SlaSituacaoCalculator
      Hubs/                        # ChamadosHub (SignalR)
      Models/                      # Entities e Dtos
      Migrations/                  # Migrations do EF Core (schema versionado)
      Program.cs                   # Bootstrap, pipeline, JWT, Swagger, healthcheck
      appsettings.json
    Chamados.Api.Tests/            # Testes automatizados (xUnit)
      Unit/                        # Sem dependências externas (ex.: cálculo de SLA)
      Integration/                 # WebApplicationFactory + Testcontainers.PostgreSql
```

## Como rodar

Pré-requisito: **.NET SDK 9** (`dotnet --version`) e um **PostgreSQL** acessível (local, remoto ou via `docker compose up -d db` a partir da raiz do repositório).

```bash
cd backend

# restaurar + compilar
dotnet build

# subir a API em modo desenvolvimento (porta 5000, ver Properties/launchSettings.json)
dotnet run --project src/Chamados.Api
```

A API sobe em **http://localhost:5000** — a mesma porta que o proxy do frontend
([`../frontend/proxy.conf.json`](../frontend/proxy.conf.json)) espera.

### Endpoints úteis

| Método | Rota                        | Descrição                              |
| ------ | --------------------------- | --------------------------------------- |
| GET    | `/health`                   | Healthcheck (liveness) em JSON          |
| POST   | `/api/v1/auth/login`        | Autentica (`email`, `senha`) e retorna JWT |
| POST   | `/api/v1/auth/registrar`    | Autocadastro de um usuário Cliente      |
| GET    | `/swagger`                  | Swagger UI (apenas em Development)      |
| GET    | `/swagger/v1/swagger.json`  | Documento OpenAPI gerado a partir dos controllers |

O contrato completo (todos os endpoints de chamados, comentários, anexos, avaliações, usuários, departamentos, domínios, notificações e métricas) está documentado em [`../docs/openapi.yaml`](../docs/openapi.yaml) e, de forma interativa, no Swagger UI acima.

### Usuário de desenvolvimento (seed)

As migrations `SeedUsuarioAdmin`/`AtualizaEmailSenhaUsuarioAdmin` criam um usuário Administrador para permitir testar o login sem precisar de outro usuário já cadastrado:

```
email: gus@admin.com
senha: admin@123
```

> Apenas para desenvolvimento. Demais usuários (Técnico/Administrador) são criados pela tela de administração (`POST /api/v1/usuarios`, restrito a Administrador); um Cliente pode se autocadastrar em `POST /api/v1/auth/registrar`.

Exemplo de resposta do healthcheck:

```json
{ "status": "Healthy", "timestamp": "2026-07-20T21:57:55.6884172+00:00" }
```

> O healthcheck atual é apenas _liveness_ (não verifica a conexão com o PostgreSQL).

## Configuração

Nenhum segredo é versionado. A connection string e demais configs são lidas via
variáveis de ambiente (ex.: `ConnectionStrings__DefaultConnection`) — ver
[`../.env.example`](../.env.example).

A seção `Jwt` em `appsettings.json` (chave, issuer, audience, expiração) vem com placeholders vazios e é sobrescrita via `Jwt__Key` / `Jwt__Issuer` / `Jwt__Audience` / `Jwt__ExpiresMinutes`. `POST /api/v1/auth/login` e `POST /api/v1/auth/registrar` usam essa configuração para emitir o access token. As demais rotas exigem esse token por padrão (política de autorização _fallback_ configurada em `Program.cs`); apenas endpoints marcados com `[AllowAnonymous]` (login, registrar) e o `/health` ficam públicos. Um usuário desativado (`Ativo = false`) tem o token invalidado na primeira requisição/reconexão seguinte (`OnTokenValidated` em `Program.cs`) e é desconectado do hub SignalR imediatamente se estiver com a sessão aberta.

Outras seções configuráveis: `SlaMonitor:IntervalSeconds` (intervalo do serviço em background que reavalia a situação de SLA) e `Upload` (`StoragePath`, `MaxFileSizeBytes`, `AllowedExtensions` — validações de anexo).

## Testes

```bash
cd backend
dotnet test
```

O projeto [`Chamados.Api.Tests`](src/Chamados.Api.Tests) tem duas suítes:

- **`Unit/`** — sem dependências externas (ex.: `SlaSituacaoCalculatorTests`). Sempre rodam, em qualquer ambiente.
- **`Integration/`** — sobem a API real via `WebApplicationFactory<Program>` contra um PostgreSQL efêmero criado pelo **Testcontainers.PostgreSql**, aplicando as migrations reais. Cobrem autenticação, o ciclo de vida completo de um chamado, os endpoints de métricas e uma bateria de testes de segurança (`InjectionSafetyTests`) contra SQL injection/XSS nos filtros de texto livre (`q`, `solicitante`) e campos de texto do chamado/comentário.

Para rodar só os testes unitários (sem precisar de Docker):

```bash
dotnet test --filter FullyQualifiedName~Unit
```

> **Testcontainers e Docker remoto:** a suíte de integração precisa de um daemon Docker diretamente alcançável pelo processo `dotnet test`. Se o seu Docker estiver configurado como um *context* remoto via SSH (`docker context ls` mostrando `ssh://...`), o cliente usado pelo Testcontainers.NET não sabe resolver esse esquema (`Unknown URL scheme ssh`) e a suíte falha ao subir o container — ver o comentário no topo de `Integration/Support/IntegrationTestFixture.cs`. Nesse caso, use o filtro acima localmente e deixe a suíte completa para a CI ([`../.github/workflows/ci.yml`](../.github/workflows/ci.yml), que roda em `ubuntu-latest` com Docker nativo) ou para um ambiente com acesso direto ao daemon.
