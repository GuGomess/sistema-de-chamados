using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Tests.Integration.Support;

namespace Chamados.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class AuthTests
{
    private readonly IntegrationTestFixture _fixture;

    public AuthTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Registrar_ComCredenciaisValidas_RetornaTokenJwtValidoEUsuarioCliente()
    {
        using var client = _fixture.Factory.CreateClient();
        var sufixo = Guid.NewGuid().ToString("N");
        var email = $"novo-cliente-{sufixo}@teste.local";

        var response = await client.PostAsJsonAsync("/api/v1/auth/registrar", new RegistrarClienteRequest
        {
            Nome = "Cliente Novo",
            Email = email,
            Senha = ContaHelper.SenhaPadrao
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(ContaHelper.JsonOptions);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));
        Assert.Equal("CLIENTE", auth.Usuario.Perfil);
        Assert.Equal(email, auth.Usuario.Email);

        AssertarTokenJwtValido(auth.AccessToken, auth.Usuario.Id);

        // Token deve efetivamente autenticar em uma rota protegida.
        client.UsarToken(auth.AccessToken);
        var chamadosResponse = await client.GetAsync("/api/v1/chamados");
        Assert.Equal(HttpStatusCode.OK, chamadosResponse.StatusCode);
    }

    [Fact]
    public async Task Registrar_ComEmailJaCadastrado_Retorna422()
    {
        using var client = _fixture.Factory.CreateClient();
        var primeiraConta = await ContaHelper.RegistrarClienteAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/registrar", new RegistrarClienteRequest
        {
            Nome = "Outro Nome",
            Email = primeiraConta.Usuario.Email,
            Senha = ContaHelper.SenhaPadrao
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var erro = await response.Content.ReadFromJsonAsync<ErrorResponse>(ContaHelper.JsonOptions);
        Assert.NotNull(erro);
        Assert.Contains("email", erro!.Errors!.Keys);
    }

    [Fact]
    public async Task Registrar_ComSenhaCurta_Retorna422()
    {
        using var client = _fixture.Factory.CreateClient();
        var sufixo = Guid.NewGuid().ToString("N");

        var response = await client.PostAsJsonAsync("/api/v1/auth/registrar", new RegistrarClienteRequest
        {
            Nome = "Cliente Senha Curta",
            Email = $"senha-curta-{sufixo}@teste.local",
            Senha = "123"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var erro = await response.Content.ReadFromJsonAsync<ErrorResponse>(ContaHelper.JsonOptions);
        Assert.NotNull(erro);
        Assert.Contains("senha", erro!.Errors!.Keys);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_RetornaTokenJwtValido()
    {
        using var client = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = conta.Usuario.Email,
            Senha = ContaHelper.SenhaPadrao
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(ContaHelper.JsonOptions);
        Assert.NotNull(auth);
        AssertarTokenJwtValido(auth!.AccessToken, conta.Usuario.Id);
    }

    [Fact]
    public async Task Login_ComSenhaInvalida_Retorna401()
    {
        using var client = _fixture.Factory.CreateClient();
        var conta = await ContaHelper.RegistrarClienteAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = conta.Usuario.Email,
            Senha = "SenhaErrada@999"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_Retorna401()
    {
        using var client = _fixture.Factory.CreateClient();
        var sufixo = Guid.NewGuid().ToString("N");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = $"nao-existe-{sufixo}@teste.local",
            Senha = ContaHelper.SenhaPadrao
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_SemToken_Retorna401()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/chamados");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static void AssertarTokenJwtValido(string accessToken, long usuarioIdEsperado)
    {
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(accessToken));

        var token = handler.ReadJwtToken(accessToken);
        Assert.Equal("chamados-tests", token.Issuer);
        Assert.Contains("chamados-tests", token.Audiences);
        Assert.True(token.ValidTo > DateTime.UtcNow);

        var subClaim = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.Equal(usuarioIdEsperado.ToString(), subClaim.Value);
    }
}
