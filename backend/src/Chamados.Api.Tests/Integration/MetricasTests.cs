using System.Net;
using System.Net.Http.Json;
using Chamados.Api.Models.Dtos.Chamados;
using Chamados.Api.Models.Dtos.Metricas;
using Chamados.Api.Tests.Integration.Support;

namespace Chamados.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class MetricasTests
{
    private const long DepartamentoHelpDeskId = 1;
    private const long StatusAbertoId = 1;
    private const long StatusNovoId = 6;
    private const long StatusResolvidoId = 4;

    private readonly IntegrationTestFixture _fixture;

    public MetricasTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ChamadosPorStatus_ComoCliente_Retorna403()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);

        var response = await clienteClient.GetAsync("/api/v1/metricas/chamados-por-status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChamadosPorStatus_ComoTecnico_RetornaOk()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);
        var tecnico = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var tecnicoClient = _fixture.Factory.CreateClient();
        var loginTecnico = await ContaHelper.LoginAsync(tecnicoClient, tecnico.Email);
        tecnicoClient.UsarToken(loginTecnico.AccessToken);

        var response = await tecnicoClient.GetAsync("/api/v1/metricas/chamados-por-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChamadosPorStatus_ComoAdministrador_AgregaContagemCorretaPorStatus()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);
        var tecnico = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var tecnicoClient = _fixture.Factory.CreateClient();
        var loginTecnico = await ContaHelper.LoginAsync(tecnicoClient, tecnico.Email);
        tecnicoClient.UsarToken(loginTecnico.AccessToken);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);

        // Deltas antes/depois (em vez de valores absolutos ou filtro por data):
        // a suíte roda tudo sequencialmente contra o mesmo container, então "antes"
        // captura qualquer chamado de outros testes sem depender de os relógios do
        // processo de teste (Windows) e do container Postgres (roda numa máquina
        // Arch remota via Docker context SSH — ver IntegrationTestFixture) estarem
        // sincronizados.
        var antes = await ObterContagemPorStatusAsync(adminClient);

        // Um chamado permanece "Novo" (status inicial de todo chamado criado).
        await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        // Outro é assumido e movido para "Aberto" — para ter dois status distintos.
        var chamadoAberto = await ChamadoHelper.CriarComoClienteAsync(clienteClient);
        await tecnicoClient.PostAsync($"/api/v1/chamados/{chamadoAberto.Id}/assumir", null);
        var patchResponse = await tecnicoClient.PatchAsJsonAsync($"/api/v1/chamados/{chamadoAberto.Id}", new ChamadoUpdateRequest { IdStatus = StatusAbertoId });
        patchResponse.EnsureSuccessStatusCode();

        var depois = await ObterContagemPorStatusAsync(adminClient);

        Assert.Equal(antes.GetValueOrDefault(StatusNovoId) + 1, depois.GetValueOrDefault(StatusNovoId));
        Assert.Equal(antes.GetValueOrDefault(StatusAbertoId) + 1, depois.GetValueOrDefault(StatusAbertoId));
    }

    [Fact]
    public async Task ProdutividadeTecnicos_ComoCliente_Retorna403()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);

        var response = await clienteClient.GetAsync("/api/v1/metricas/produtividade-tecnicos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProdutividadeTecnicos_ComoTecnicoNaoAdmin_RetornaApenasAPropriaLinha()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);

        var tecnicoA = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);
        var tecnicoB = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);

        using var tecnicoAClient = _fixture.Factory.CreateClient();
        var loginA = await ContaHelper.LoginAsync(tecnicoAClient, tecnicoA.Email);
        tecnicoAClient.UsarToken(loginA.AccessToken);

        using var tecnicoBClient = _fixture.Factory.CreateClient();
        var loginB = await ContaHelper.LoginAsync(tecnicoBClient, tecnicoB.Email);
        tecnicoBClient.UsarToken(loginB.AccessToken);

        // Técnico A: assume, comenta (exigido p/ resolver) e resolve um chamado —
        // 1 atribuído, 1 resolvido.
        var chamadoA = await ChamadoHelper.CriarComoClienteAsync(clienteClient);
        await tecnicoAClient.PostAsync($"/api/v1/chamados/{chamadoA.Id}/assumir", null);
        await ChamadoHelper.ComentarAsync(tecnicoAClient, chamadoA.Id, "Investigando.");
        var resolverResponse = await tecnicoAClient.PatchAsJsonAsync($"/api/v1/chamados/{chamadoA.Id}", new ChamadoUpdateRequest { IdStatus = StatusResolvidoId });
        resolverResponse.EnsureSuccessStatusCode();

        // Técnico B: só assume (não resolve) — 1 atribuído, 0 resolvido. Serve
        // para provar que a linha de B não vaza na resposta de A.
        var chamadoB = await ChamadoHelper.CriarComoClienteAsync(clienteClient);
        await tecnicoBClient.PostAsync($"/api/v1/chamados/{chamadoB.Id}/assumir", null);

        var response = await tecnicoAClient.GetAsync("/api/v1/metricas/produtividade-tecnicos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var linhas = await response.Content.ReadFromJsonAsync<List<ProdutividadeTecnicoDto>>(ContaHelper.JsonOptions);
        Assert.NotNull(linhas);
        var linha = Assert.Single(linhas!);
        Assert.Equal(tecnicoA.Id, linha.TecnicoId);
        Assert.Equal(1, linha.ChamadosAtribuidos);
        Assert.Equal(1, linha.ChamadosResolvidos);
        Assert.NotNull(linha.TempoMedioResolucaoHoras);
    }

    [Fact]
    public async Task ProdutividadeTecnicos_ComoAdministrador_RetornaLinhasDeTodosOsTecnicos()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);

        var tecnicoA = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);
        var tecnicoB = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);

        using var tecnicoAClient = _fixture.Factory.CreateClient();
        var loginA = await ContaHelper.LoginAsync(tecnicoAClient, tecnicoA.Email);
        tecnicoAClient.UsarToken(loginA.AccessToken);

        using var tecnicoBClient = _fixture.Factory.CreateClient();
        var loginB = await ContaHelper.LoginAsync(tecnicoBClient, tecnicoB.Email);
        tecnicoBClient.UsarToken(loginB.AccessToken);

        var chamadoA = await ChamadoHelper.CriarComoClienteAsync(clienteClient);
        await tecnicoAClient.PostAsync($"/api/v1/chamados/{chamadoA.Id}/assumir", null);

        var chamadoB = await ChamadoHelper.CriarComoClienteAsync(clienteClient);
        await tecnicoBClient.PostAsync($"/api/v1/chamados/{chamadoB.Id}/assumir", null);

        var response = await adminClient.GetAsync("/api/v1/metricas/produtividade-tecnicos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var linhas = await response.Content.ReadFromJsonAsync<List<ProdutividadeTecnicoDto>>(ContaHelper.JsonOptions);
        Assert.NotNull(linhas);

        var linhaA = Assert.Single(linhas!, l => l.TecnicoId == tecnicoA.Id);
        Assert.Equal(1, linhaA.ChamadosAtribuidos);

        var linhaB = Assert.Single(linhas!, l => l.TecnicoId == tecnicoB.Id);
        Assert.Equal(1, linhaB.ChamadosAtribuidos);
    }

    private static async Task<Dictionary<long, int>> ObterContagemPorStatusAsync(HttpClient adminClient)
    {
        var lista = await adminClient.GetFromJsonAsync<List<ChamadoPorStatusDto>>("/api/v1/metricas/chamados-por-status", ContaHelper.JsonOptions);
        return lista!.ToDictionary(x => x.StatusId, x => x.Quantidade);
    }
}
