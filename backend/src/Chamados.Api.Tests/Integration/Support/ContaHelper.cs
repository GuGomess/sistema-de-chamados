using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chamados.Api.Models.Dtos.Auth;

namespace Chamados.Api.Tests.Integration.Support;

/// <summary>
/// Helpers para criar contas (cliente/técnico) e autenticar via HTTP real
/// contra os endpoints de Auth/Usuarios — evita manipular o banco diretamente
/// nos testes de integração, exercitando o mesmo caminho que o frontend usa.
/// Cada conta criada usa um sufixo aleatório (e-mail único) para não colidir
/// com dados de outros testes que rodam contra o mesmo container Postgres.
/// </summary>
internal static class ContaHelper
{
    public const string SenhaPadrao = "Senha@123";

    // Semeado pela migration SeedUsuarioAdmin/AtualizaEmailSenhaUsuarioAdmin —
    // usado só para bootstrapar (criar técnicos/atribuir departamentos), nunca
    // alterado pelos testes.
    public const string AdminSeedEmail = "gus@admin.com";
    public const string AdminSeedSenha = "admin@123";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static void UsarToken(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<AuthResponse> LoginAsync(HttpClient client, string email, string senha = SenhaPadrao)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = email, Senha = senha });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    public static Task<AuthResponse> LoginComoAdminSeedAsync(HttpClient client) =>
        LoginAsync(client, AdminSeedEmail, AdminSeedSenha);

    public static async Task<AuthResponse> RegistrarClienteAsync(HttpClient client, string? nome = null)
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync("/api/v1/auth/registrar", new RegistrarClienteRequest
        {
            Nome = nome ?? $"Cliente Teste {sufixo}",
            Email = $"cliente-{sufixo}@teste.local",
            Senha = SenhaPadrao
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    /// <summary>
    /// Cria um técnico (via POST /usuarios, que exige admin) e opcionalmente já
    /// o vincula aos departamentos informados (via PUT /usuarios/{id}/departamentos).
    /// </summary>
    public static async Task<UsuarioDto> CriarTecnicoAsync(HttpClient adminClient, IReadOnlyCollection<long>? idsDepartamentos = null, string? nome = null)
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var response = await adminClient.PostAsJsonAsync("/api/v1/usuarios", new UsuarioCreateRequest
        {
            Nome = nome ?? $"Técnico Teste {sufixo}",
            Email = $"tecnico-{sufixo}@teste.local",
            Senha = SenhaPadrao,
            Perfil = "TECNICO"
        });
        response.EnsureSuccessStatusCode();
        var usuario = (await response.Content.ReadFromJsonAsync<UsuarioDto>(JsonOptions))!;

        if (idsDepartamentos is { Count: > 0 })
        {
            var deptResponse = await adminClient.PutAsJsonAsync(
                $"/api/v1/usuarios/{usuario.Id}/departamentos",
                new UsuarioDepartamentosRequest { IdsDepartamentos = idsDepartamentos.ToArray() });
            deptResponse.EnsureSuccessStatusCode();
        }

        return usuario;
    }
}
