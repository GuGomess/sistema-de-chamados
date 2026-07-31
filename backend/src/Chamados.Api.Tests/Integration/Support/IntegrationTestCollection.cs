namespace Chamados.Api.Tests.Integration.Support;

/// <summary>
/// Agrupa todas as classes de teste de integração numa única coleção do xUnit:
/// isso garante (a) que elas compartilham a mesma <see cref="IntegrationTestFixture"/>
/// (um container Postgres + uma WebApplicationFactory para toda a suíte) e
/// (b) que os testes rodam sequencialmente entre si (xUnit não paraleliza
/// testes dentro da mesma coleção), evitando corrida entre escritas
/// concorrentes no mesmo banco.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integração com Postgres (Testcontainers)";
}
