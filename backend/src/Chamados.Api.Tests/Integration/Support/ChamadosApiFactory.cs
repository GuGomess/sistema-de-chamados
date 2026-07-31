using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Chamados.Api.Tests.Integration.Support;

/// <summary>
/// WebApplicationFactory que substitui a connection string "DefaultConnection"
/// (e as opções de Jwt, que em appsettings.json vêm vazias e são preenchidas só
/// em runtime/user-secrets) para apontar para o Postgres efêmero subido pelo
/// Testcontainers em <see cref="IntegrationTestFixture"/>.
/// </summary>
public class ChamadosApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ChamadosApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,

                // Chave de teste com tamanho suficiente para HMAC-SHA256 (>= 256 bits).
                ["Jwt:Key"] = "chave-de-teste-para-a-suite-de-integracao-nao-usar-em-producao-0123456789",
                ["Jwt:Issuer"] = "chamados-tests",
                ["Jwt:Audience"] = "chamados-tests",
                ["Jwt:ExpiresMinutes"] = "60"
            };

            configBuilder.AddInMemoryCollection(overrides);
        });
    }
}
