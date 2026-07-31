using System.Net;
using System.Net.Http.Json;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Chamados;
using Chamados.Api.Tests.Integration.Support;

namespace Chamados.Api.Tests.Integration;

/// <summary>
/// Bateria de testes de segurança contra SQL injection e payloads adversariais
/// (XSS, strings patologicamente grandes) enviados via query params (q,
/// solicitante em GET /api/v1/chamados) e via campos de texto livre no corpo
/// (Titulo/Descricao ao criar chamado, Mensagem de comentário).
///
/// Contexto (auditoria feita antes desta suíte): não existe SQL cru neste
/// backend (nenhum FromSqlRaw/ExecuteSqlRaw/SqlQuery/NpgsqlCommand/Dapper) —
/// toda consulta passa por LINQ do EF Core contra ChamadosDbContext, inclusive
/// os dois pontos que usam EF.Functions.ILike com interpolação de string
/// (ChamadosController.Listar, filtros "q" e "solicitante"): a interpolação
/// ocorre dentro de uma expression tree do LINQ, então o provider Npgsql
/// traduz o valor para um parâmetro real da query (não concatenação de SQL).
/// Os testes abaixo verificam isso na prática (comportamento observado via
/// HTTP), não só leem o código-fonte.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class InjectionSafetyTests
{
    private readonly IntegrationTestFixture _fixture;

    public InjectionSafetyTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public static IEnumerable<object[]> PayloadsAdversariais()
    {
        yield return new object[] { "' OR '1'='1" };
        yield return new object[] { "'; DROP TABLE chamados; --" };
        yield return new object[] { "1) UNION SELECT * FROM usuarios--" };
        yield return new object[] { "<script>alert(1)</script>" };
    }

    private static string StringGigante(int tamanho = 10_000) => new string('a', tamanho);

    // ------------------------------------------------------------------
    // Query params (q, solicitante) — GET /api/v1/chamados
    // ------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PayloadsAdversariais))]
    public async Task Listar_ComPayloadAdversarialEmQ_NuncaRetorna500EFiltraComoTextoLiteral(string payload)
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);
        await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        var response = await clienteClient.GetAsync($"/api/v1/chamados?q={Uri.EscapeDataString(payload)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ChamadoPageDto>(ContaHelper.JsonOptions);
        Assert.NotNull(page);

        // Nenhum chamado deste cliente tem esse payload como substring literal
        // de título/descrição — resultado precisa ser vazio. Se a interpolação
        // virasse SQL concatenado (ex.: "OR '1'='1" quebrando a cláusula WHERE),
        // o resultado passaria a incluir o chamado mesmo sem o termo aparecer
        // de fato no título/descrição, ou a query quebraria com 500.
        Assert.Empty(page!.Items);
    }

    [Theory]
    [MemberData(nameof(PayloadsAdversariais))]
    public async Task Listar_ComPayloadAdversarialEmSolicitante_NuncaRetorna500EFiltraComoTextoLiteral(string payload)
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);
        await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        var response = await clienteClient.GetAsync($"/api/v1/chamados?solicitante={Uri.EscapeDataString(payload)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ChamadoPageDto>(ContaHelper.JsonOptions);
        Assert.NotNull(page);

        // O nome do próprio cliente (gerado com um sufixo aleatório) não contém
        // o payload — mesma garantia do teste acima, aplicada ao join com Usuarios.
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task Listar_ComQMaiorQueLimite_Retorna400SemChegarAoBanco()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);

        var response = await clienteClient.GetAsync($"/api/v1/chamados?q={Uri.EscapeDataString(StringGigante())}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var erro = await response.Content.ReadFromJsonAsync<ErrorResponse>(ContaHelper.JsonOptions);
        Assert.NotNull(erro);
        Assert.Contains("q", erro!.Errors!.Keys);
    }

    [Fact]
    public async Task Listar_ComSolicitanteMaiorQueLimite_Retorna400SemChegarAoBanco()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);

        var response = await clienteClient.GetAsync($"/api/v1/chamados?solicitante={Uri.EscapeDataString(StringGigante())}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var erro = await response.Content.ReadFromJsonAsync<ErrorResponse>(ContaHelper.JsonOptions);
        Assert.NotNull(erro);
        Assert.Contains("solicitante", erro!.Errors!.Keys);
    }

    [Fact]
    public async Task Listar_ComQNoLimiteExato_NaoRejeitaEContinuaFuncionando()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);
        await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        // Exatamente 200 caracteres (o limite adicionado) não deve ser rejeitado
        // — só o que excede o limite é bloqueado.
        var response = await clienteClient.GetAsync($"/api/v1/chamados?q={Uri.EscapeDataString(StringGigante(200))}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Prova de ausência real de injeção: cria dois chamados com títulos
    /// diferentes (só um deles contém o payload de injeção como substring real
    /// e legítima do título) e confirma que a busca por esse payload retorna
    /// exatamente o que uma busca ILIKE normal (contains, case-insensitive)
    /// retornaria — nem a mais (o outro chamado vazando), nem a menos (erro
    /// engolindo o resultado esperado), nem um 500.
    /// </summary>
    [Theory]
    [MemberData(nameof(PayloadsAdversariais))]
    public async Task Listar_ComPayloadDeInjecaoComoSubstringRealDoTitulo_RetornaExatamenteOEsperadoPelaIlike(string payload)
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);

        // Marcador único (Guid) garante que o termo de busca não colida com
        // nenhum outro chamado já existente no mesmo container Postgres
        // (compartilhado por toda a suíte de integração) — independentemente
        // de quantos outros testes já rodaram antes deste.
        var marcador = Guid.NewGuid().ToString("N");
        var trechoComPayload = $"{marcador}-marcador contendo {payload} no relato";

        using var clienteAlvoClient = _fixture.Factory.CreateClient();
        var contaAlvo = await ContaHelper.RegistrarClienteAsync(clienteAlvoClient);
        clienteAlvoClient.UsarToken(contaAlvo.AccessToken);
        var chamadoComPayload = await ChamadoHelper.CriarComoClienteAsync(
            clienteAlvoClient,
            titulo: $"Chamado {trechoComPayload} durante o login");

        using var clienteOutroClient = _fixture.Factory.CreateClient();
        var contaOutro = await ContaHelper.RegistrarClienteAsync(clienteOutroClient);
        clienteOutroClient.UsarToken(contaOutro.AccessToken);
        var chamadoSemPayload = await ChamadoHelper.CriarComoClienteAsync(
            clienteOutroClient,
            titulo: $"Chamado {marcador}-outro: problema completamente diferente, sem o termo pesquisado");

        // Busca (via administrador, que não tem escopo restrito) usando
        // exatamente o payload de injeção embutido como substring real do
        // primeiro título.
        var response = await adminClient.GetAsync($"/api/v1/chamados?q={Uri.EscapeDataString(trechoComPayload)}&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ChamadoPageDto>(ContaHelper.JsonOptions);
        Assert.NotNull(page);

        // Exatamente um resultado, e é o chamado certo — nada do outro chamado
        // (nem qualquer outro registro da suíte) vazou, e a query não quebrou.
        var chamadoUnico = Assert.Single(page!.Items);
        Assert.Equal(chamadoComPayload.Id, chamadoUnico.Id);
        Assert.DoesNotContain(page.Items, c => c.Id == chamadoSemPayload.Id);
    }

    /// <summary>
    /// Mesma prova de ausência de injeção do teste acima, mas para o filtro
    /// "solicitante" (que faz join com Usuarios.Nome).
    /// </summary>
    [Theory]
    [MemberData(nameof(PayloadsAdversariais))]
    public async Task Listar_ComPayloadDeInjecaoComoSubstringRealDoNomeDoSolicitante_RetornaExatamenteOEsperadoPelaIlike(string payload)
    {
        using var adminClient = _fixture.Factory.CreateClient();
        var admin = await ContaHelper.LoginComoAdminSeedAsync(adminClient);
        adminClient.UsarToken(admin.AccessToken);

        var marcador = Guid.NewGuid().ToString("N");
        var nomeComPayload = $"{marcador}-marcador {payload} Cliente";

        using var clienteAlvoClient = _fixture.Factory.CreateClient();
        var contaAlvo = await ContaHelper.RegistrarClienteAsync(clienteAlvoClient, nome: nomeComPayload);
        clienteAlvoClient.UsarToken(contaAlvo.AccessToken);
        var chamadoDoAlvo = await ChamadoHelper.CriarComoClienteAsync(clienteAlvoClient);

        using var clienteOutroClient = _fixture.Factory.CreateClient();
        var contaOutro = await ContaHelper.RegistrarClienteAsync(clienteOutroClient, nome: $"{marcador}-outro Cliente Comum");
        clienteOutroClient.UsarToken(contaOutro.AccessToken);
        var chamadoDoOutro = await ChamadoHelper.CriarComoClienteAsync(clienteOutroClient);

        var response = await adminClient.GetAsync($"/api/v1/chamados?solicitante={Uri.EscapeDataString(nomeComPayload)}&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ChamadoPageDto>(ContaHelper.JsonOptions);
        Assert.NotNull(page);

        var chamadoUnico = Assert.Single(page!.Items);
        Assert.Equal(chamadoDoAlvo.Id, chamadoUnico.Id);
        Assert.DoesNotContain(page.Items, c => c.Id == chamadoDoOutro.Id);
    }

    // ------------------------------------------------------------------
    // Corpo da requisição: Titulo/Descricao (criar chamado) e Mensagem
    // (comentário) — texto livre enviado como JSON/form, nunca interpolado
    // em SQL algum (não há SQL cru no projeto).
    // ------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PayloadsAdversariais))]
    public async Task Criar_ComPayloadAdversarialNoTituloEDescricao_ArmazenaComoTextoInerteRecuperavelSemAlteracao(string payload)
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);

        // Titulo tem [MaxLength(160)] — os payloads de injeção/XSS testados são
        // curtos o bastante para caber nesse limite.
        var titulo = $"Chamado com payload: {payload}";
        var descricao = $"Descrição contendo o payload de teste: {payload}";

        var response = await clienteClient.PostAsJsonAsync("/api/v1/chamados", new ChamadoCreateRequest
        {
            Titulo = titulo,
            Descricao = descricao
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var chamado = await response.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.NotNull(chamado);

        // O payload volta exatamente como enviado — tratado como texto inerte,
        // nunca interpretado/executado, e sem truncamento/escaping indevido.
        Assert.Equal(titulo, chamado!.Titulo);
        Assert.Equal(descricao, chamado.Descricao);

        // Confirma que persistiu de fato (não é só o eco da resposta de
        // criação): busca de volta via GET /{id}, outro caminho de leitura.
        var detalheResponse = await clienteClient.GetAsync($"/api/v1/chamados/{chamado.Id}");
        Assert.Equal(HttpStatusCode.OK, detalheResponse.StatusCode);
        var detalhe = await detalheResponse.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.Equal(titulo, detalhe!.Titulo);
        Assert.Equal(descricao, detalhe.Descricao);
    }

    [Fact]
    public async Task Criar_ComTituloMaiorQueLimite_Retorna400PorValidacaoDeModelo()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);

        var response = await clienteClient.PostAsJsonAsync("/api/v1/chamados", new ChamadoCreateRequest
        {
            Titulo = StringGigante(),
            Descricao = "Descrição válida."
        });

        // Titulo já tem [Required, MaxLength(160)] — [ApiController] rejeita
        // automaticamente via ModelState antes de a ação ser executada.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criar_ComDescricaoGigante_NuncaRetorna500EArmazenaTextoIntacto()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);

        // Descricao não tem MaxLength hoje — o objetivo aqui não é validar
        // limite (fora do escopo pedido), e sim provar que uma string muito
        // grande (10 mil+ caracteres) é processada com segurança como texto
        // inerte, sem 500 e sem corrupção, via parâmetro do EF Core.
        var descricaoGigante = StringGigante(15_000);

        var response = await clienteClient.PostAsJsonAsync("/api/v1/chamados", new ChamadoCreateRequest
        {
            Titulo = "Chamado com descrição gigante",
            Descricao = descricaoGigante
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var chamado = await response.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions);
        Assert.NotNull(chamado);
        Assert.Equal(descricaoGigante, chamado!.Descricao);
        Assert.Equal(15_000, chamado.Descricao.Length);
    }

    [Theory]
    [MemberData(nameof(PayloadsAdversariais))]
    public async Task Comentar_ComPayloadAdversarialNaMensagem_ArmazenaComoTextoInerteRecuperavelSemAlteracao(string payload)
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        var mensagem = $"Retorno do cliente contendo o payload: {payload}";
        var response = await ChamadoHelper.ComentarAsync(clienteClient, chamado.Id, mensagem);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comentariosResponse = await clienteClient.GetAsync($"/api/v1/chamados/{chamado.Id}/comentarios");
        Assert.Equal(HttpStatusCode.OK, comentariosResponse.StatusCode);
        var comentarios = await comentariosResponse.Content.ReadFromJsonAsync<List<ComentarioDto>>(ContaHelper.JsonOptions);
        Assert.NotNull(comentarios);
        Assert.Contains(comentarios!, c => c.Mensagem == mensagem);
    }

    [Fact]
    public async Task Comentar_ComMensagemGigante_NuncaRetorna500EArmazenaTextoIntacto()
    {
        using var clienteClient = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(clienteClient);
        clienteClient.UsarToken(conta.AccessToken);
        var chamado = await ChamadoHelper.CriarComoClienteAsync(clienteClient);

        var mensagemGigante = StringGigante(15_000);
        var response = await ChamadoHelper.ComentarAsync(clienteClient, chamado.Id, mensagemGigante);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comentariosResponse = await clienteClient.GetAsync($"/api/v1/chamados/{chamado.Id}/comentarios");
        var comentarios = await comentariosResponse.Content.ReadFromJsonAsync<List<ComentarioDto>>(ContaHelper.JsonOptions);
        Assert.NotNull(comentarios);
        Assert.Contains(comentarios!, c => c.Mensagem == mensagemGigante);
    }
}
