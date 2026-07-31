using System.Net;
using System.Net.Http.Json;
using Chamados.Api.Models.Dtos.Chamados;
using Chamados.Api.Tests.Integration.Support;

namespace Chamados.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class ChamadosLifecycleTests
{
    // Departamentos semeados em ChamadosDbContext.OnModelCreating.
    private const long DepartamentoHelpDeskId = 1;
    private const long DepartamentoDesenvolvimentoId = 2;
    private const long DepartamentoInfraestruturaId = 3;

    // Status semeados em ChamadosDbContext.OnModelCreating.
    private const long StatusNovoId = 6;
    private const long StatusAbertoId = 1;
    private const long StatusResolvidoId = 4;
    private const long StatusFechadoId = 5;

    private readonly IntegrationTestFixture _fixture;

    public ChamadosLifecycleTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Criar_ComoCliente_AplicaDefaultsDeCategoriaEPrioridadeIndependenteDoPayload()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);

        // Cliente envia idCategoria/idPrioridade explicitamente (ex.: tentando
        // escolher "Hardware"/"Crítica") — o servidor deve ignorar e aplicar os
        // defaults de triagem de qualquer forma (ver ChamadosController.Criar).
        var response = await clienteClient.PostAsJsonAsync("/api/v1/chamados", new ChamadoCreateRequest
        {
            Titulo = "Chamado com payload tentando burlar defaults",
            Descricao = "Descrição de teste.",
            IdCategoria = 1, // Hardware
            IdPrioridade = 4 // Crítica
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var chamado = await response.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);

        Assert.NotNull(chamado);
        Assert.Equal(5, chamado!.Categoria.Id); // "A Triar"
        Assert.Equal(2, chamado.Prioridade.Id); // "Média"
        Assert.Equal(DepartamentoHelpDeskId, chamado.Departamento.Id);
        Assert.Equal(StatusNovoId, chamado.Status.Id);
        Assert.Null(chamado.Tecnico);
    }

    [Fact]
    public async Task Listar_ComoCliente_SoVeOsProprios()
    {
        using var clienteAClient = _fixture.Factory.CreateClient();
        var contaA = await ContaHelper.RegistrarClienteAsync(clienteAClient);
        clienteAClient.UsarToken(contaA.AccessToken);
        var chamadoA = await ChamadoHelper.CriarComoClienteAsync(clienteAClient);

        using var clienteBClient = _fixture.Factory.CreateClient();
        var contaB = await ContaHelper.RegistrarClienteAsync(clienteBClient);
        clienteBClient.UsarToken(contaB.AccessToken);
        var chamadoB = await ChamadoHelper.CriarComoClienteAsync(clienteBClient);

        var pageA = await clienteAClient.GetFromJsonAsync<ChamadoPageDto>("/api/v1/chamados?pageSize=100", ContaHelper.JsonOptions);
        Assert.NotNull(pageA);
        Assert.Contains(pageA!.Items, c => c.Id == chamadoA.Id);
        Assert.DoesNotContain(pageA.Items, c => c.Id == chamadoB.Id);

        var pageB = await clienteBClient.GetFromJsonAsync<ChamadoPageDto>("/api/v1/chamados?pageSize=100", ContaHelper.JsonOptions);
        Assert.NotNull(pageB);
        Assert.Contains(pageB!.Items, c => c.Id == chamadoB.Id);
        Assert.DoesNotContain(pageB.Items, c => c.Id == chamadoA.Id);
    }

    [Fact]
    public async Task Listar_ComoTecnicoDeOutroDepartamento_SoVeEscopoDoProprioDepartamento()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);

        var tecnicoDev = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoDesenvolvimentoId]);
        var tecnicoInfra = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoInfraestruturaId]);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        // Admin atribui o chamado ao técnico de Desenvolvimento — isso move o
        // chamado do HelpDesk para o departamento do técnico (ver ChamadosController.Atribuir).
        var atribuirResponse = await adminClient.PostAsJsonAsync($"/api/v1/chamados/{chamado.Id}/atribuir", new AtribuirTecnicoRequest { IdTecnico = tecnicoDev.Id });
        Assert.Equal(HttpStatusCode.OK, atribuirResponse.StatusCode);
        var chamadoAtribuido = await atribuirResponse.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.Equal(DepartamentoDesenvolvimentoId, chamadoAtribuido!.Departamento.Id);

        using var tecnicoDevClient = _fixture.Factory.CreateClient();
        var loginDev = await ContaHelper.LoginAsync(tecnicoDevClient, tecnicoDev.Email);
        tecnicoDevClient.UsarToken(loginDev.AccessToken);
        var pageDev = await tecnicoDevClient.GetFromJsonAsync<ChamadoPageDto>("/api/v1/chamados?pageSize=100", ContaHelper.JsonOptions);
        Assert.Contains(pageDev!.Items, c => c.Id == chamado.Id);

        using var tecnicoInfraClient = _fixture.Factory.CreateClient();
        var loginInfra = await ContaHelper.LoginAsync(tecnicoInfraClient, tecnicoInfra.Email);
        tecnicoInfraClient.UsarToken(loginInfra.AccessToken);
        var pageInfra = await tecnicoInfraClient.GetFromJsonAsync<ChamadoPageDto>("/api/v1/chamados?pageSize=100", ContaHelper.JsonOptions);
        Assert.DoesNotContain(pageInfra!.Items, c => c.Id == chamado.Id);
    }

    [Fact]
    public async Task Listar_ComoAdministrador_VeChamadosDeQualquerCliente()
    {
        using var clienteAClient = _fixture.Factory.CreateClient();
        var contaA = await ContaHelper.RegistrarClienteAsync(clienteAClient);
        clienteAClient.UsarToken(contaA.AccessToken);
        var chamadoA = await ChamadoHelper.CriarComoClienteAsync(clienteAClient);

        using var clienteBClient = _fixture.Factory.CreateClient();
        var contaB = await ContaHelper.RegistrarClienteAsync(clienteBClient);
        clienteBClient.UsarToken(contaB.AccessToken);
        var chamadoB = await ChamadoHelper.CriarComoClienteAsync(clienteBClient);

        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);

        // Diferente de cliente/técnico, administrador não tem escopo restrito —
        // consegue localizar (filtrando por id) o chamado de qualquer solicitante.
        var pageA = await adminClient.GetFromJsonAsync<ChamadoPageDto>($"/api/v1/chamados?id={chamadoA.Id}", ContaHelper.JsonOptions);
        Assert.Single(pageA!.Items);
        Assert.Equal(chamadoA.Id, pageA.Items[0].Id);

        var pageB = await adminClient.GetFromJsonAsync<ChamadoPageDto>($"/api/v1/chamados?id={chamadoB.Id}", ContaHelper.JsonOptions);
        Assert.Single(pageB!.Items);
        Assert.Equal(chamadoB.Id, pageB.Items[0].Id);
    }

    [Fact]
    public async Task Atribuir_ComoAdministrador_DefineTecnicoResponsavelPeloChamado()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);
        var tecnico = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        var response = await adminClient.PostAsJsonAsync($"/api/v1/chamados/{chamado.Id}/atribuir", new AtribuirTecnicoRequest { IdTecnico = tecnico.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chamadoAtualizado = await response.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.Equal(tecnico.Id, chamadoAtualizado!.Tecnico!.Id);
    }

    [Fact]
    public async Task Atribuir_ComoCliente_Retorna403()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        var response = await clienteClient.PostAsJsonAsync($"/api/v1/chamados/{chamado.Id}/atribuir", new AtribuirTecnicoRequest { IdTecnico = 999 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assumir_ComoTecnicoDoDepartamentoResponsavel_DefineSeComoTecnicoDoChamado()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);
        var tecnico = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        using var tecnicoClient = _fixture.Factory.CreateClient();
        var loginTecnico = await ContaHelper.LoginAsync(tecnicoClient, tecnico.Email);
        tecnicoClient.UsarToken(loginTecnico.AccessToken);

        var response = await tecnicoClient.PostAsync($"/api/v1/chamados/{chamado.Id}/assumir", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chamadoAtualizado = await response.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.Equal(tecnico.Id, chamadoAtualizado!.Tecnico!.Id);
    }

    [Fact]
    public async Task CicloCompleto_AssumirResolverFecharReabrir_TransicionamStatusCorretamente()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);
        var tecnico = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        using var tecnicoClient = _fixture.Factory.CreateClient();
        var loginTecnico = await ContaHelper.LoginAsync(tecnicoClient, tecnico.Email);
        tecnicoClient.UsarToken(loginTecnico.AccessToken);

        var assumirResponse = await tecnicoClient.PostAsync($"/api/v1/chamados/{chamado.Id}/assumir", null);
        Assert.Equal(HttpStatusCode.OK, assumirResponse.StatusCode);

        // Resolver exige ao menos um comentário desde a abertura (ver
        // ChamadosController.Atualizar) — sem isso a transição para Resolvido é bloqueada.
        var comentarioResponse = await ChamadoHelper.ComentarAsync(tecnicoClient, chamado.Id, "Problema investigado e corrigido.");
        Assert.Equal(HttpStatusCode.Created, comentarioResponse.StatusCode);

        var resolverResponse = await tecnicoClient.PatchAsJsonAsync($"/api/v1/chamados/{chamado.Id}", new ChamadoUpdateRequest { IdStatus = StatusResolvidoId });
        Assert.Equal(HttpStatusCode.OK, resolverResponse.StatusCode);
        var chamadoResolvido = await resolverResponse.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.Equal(StatusResolvidoId, chamadoResolvido!.Status.Id);
        Assert.NotNull(chamadoResolvido.ResolvidoEm);

        var fecharResponse = await clienteClient.PostAsJsonAsync($"/api/v1/chamados/{chamado.Id}/fechar-cliente", new FecharClienteRequest { Motivo = "Confirmado, obrigado." });
        Assert.Equal(HttpStatusCode.OK, fecharResponse.StatusCode);
        var chamadoFechado = await fecharResponse.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.Equal(StatusFechadoId, chamadoFechado!.Status.Id);
        Assert.NotNull(chamadoFechado.FechadoEm);

        var reabrirResponse = await clienteClient.PostAsync($"/api/v1/chamados/{chamado.Id}/reabrir", null);
        Assert.Equal(HttpStatusCode.OK, reabrirResponse.StatusCode);
        var chamadoReaberto = await reabrirResponse.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.Equal(StatusAbertoId, chamadoReaberto!.Status.Id);
        Assert.Null(chamadoReaberto.ResolvidoEm);
        Assert.Null(chamadoReaberto.FechadoEm);
    }

    [Fact]
    public async Task Resolver_SemComentarioDesdeAAbertura_Retorna422()
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);
        var tecnico = await ContaHelper.CriarTecnicoAsync(adminClient, [DepartamentoHelpDeskId]);

        using var clienteClient = _fixture.Factory.CreateClient();
        var cliente = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(cliente.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        using var tecnicoClient = _fixture.Factory.CreateClient();
        var loginTecnico = await ContaHelper.LoginAsync(tecnicoClient, tecnico.Email);
        tecnicoClient.UsarToken(loginTecnico.AccessToken);

        await tecnicoClient.PostAsync($"/api/v1/chamados/{chamado.Id}/assumir", null);

        var resolverResponse = await tecnicoClient.PatchAsJsonAsync($"/api/v1/chamados/{chamado.Id}", new ChamadoUpdateRequest { IdStatus = StatusResolvidoId });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resolverResponse.StatusCode);
    }
}
