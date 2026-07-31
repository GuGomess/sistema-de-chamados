using Chamados.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Chamados.Api.Tests.Integration.Support;

// ---------------------------------------------------------------------------
// AVISO DE AMBIENTE: os testes de integração desta suíte dependem de um daemon
// Docker alcançável a partir do processo que roda "dotnet test" (Testcontainers
// sobe um Postgres real e efêmero — necessário porque vários endpoints do
// ChamadosController usam EF.Functions.ILike, não suportado pelo provider EF
// Core InMemory).
//
// Nesta máquina de desenvolvimento (Windows), o Docker não roda localmente: ele
// roda numa máquina remota Arch Linux, acessada via um Docker context SSH
// (`docker context ls` mostra "arch-remoto", endpoint ssh://...). CONFIRMADO
// nesta sessão: o client Docker.DotNet usado pelo Testcontainers.NET (mesmo na
// versão 4.2.0, que já traz SSH.NET como dependência) NÃO sabe resolver esse
// endpoint — tanto lendo o context via ~/.docker/config.json quanto com
// DOCKER_HOST=ssh://... explícito, a falha é a mesma:
// "System.Exception: Unknown URL scheme ssh" (DockerClientConfiguration.CreateClient).
// Ou seja, isso não é um problema de configuração corrigível daqui — o
// Docker.DotNet não implementa o esquema ssh:// (diferente do Docker CLI, que
// delega a conexão para um subprocesso `ssh`). Se o container não subir neste
// shell, rode esta suíte via CI — os runners ubuntu-latest do GitHub Actions
// têm Docker nativo (ver .github/workflows/ci.yml, step "dotnet test") — ou em
// qualquer ambiente com acesso direto (TCP ou unix socket) ao daemon Docker.
// Os testes unitários (Unit/) não têm essa dependência e devem rodar em
// qualquer ambiente.
// ---------------------------------------------------------------------------

/// <summary>
/// Fixture compartilhada (via <see cref="IntegrationTestCollection"/>) por toda
/// a suíte de integração: sobe UM único container Postgres e UMA única
/// WebApplicationFactory para todos os testes, rodando as migrations reais do
/// projeto (Database.Migrate) contra o container — em vez de um container por
/// classe/teste, o que seria proibitivamente lento.
/// </summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public ChamadosApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("chamados_test")
            .WithUsername("chamados_test")
            .WithPassword("chamados_test")
            .Build();

        await _container.StartAsync();

        Factory = new ChamadosApiFactory(_container.GetConnectionString());

        // Acessar Factory.Services força a criação do host de teste; a partir
        // daí conseguimos um ChamadosDbContext apontando para o container e
        // aplicamos as migrations reais (mesmo schema usado em produção).
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChamadosDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Factory pode nunca ter sido atribuída se InitializeAsync falhou antes
        // (ex.: Docker inalcançável — ver aviso no topo do arquivo) — sem essa
        // checagem, a falha real fica mascarada por um NullReferenceException
        // no cleanup da coleção.
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
