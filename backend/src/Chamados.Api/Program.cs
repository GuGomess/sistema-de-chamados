using System.Text;
using System.Text.Json;
using Chamados.Api.Data;
using Chamados.Api.Options;
using Chamados.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Serviços ---
builder.Services.AddControllers();

builder.Services.AddDbContext<ChamadosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<ITokenService, TokenService>();

// Autenticação JWT: valida o token emitido pelo TokenService (mesma chave,
// issuer e audience) nas rotas protegidas.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Autorização: por padrão toda rota exige usuário autenticado (alinhado ao
// contrato em docs/openapi.yaml); endpoints públicos usam [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Health checks (liveness). Checagem de dependência com o PostgreSQL
// fica para quando o healthcheck precisar refletir o estado do banco.
builder.Services.AddHealthChecks();

// Swagger / OpenAPI (habilitado apenas em Development).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Sistema de Chamados — API",
        Version = "v1",
        Description = "API REST do Sistema de Chamados (help desk). Contrato: docs/openapi.yaml."
    });
});

var app = builder.Build();

// --- Pipeline HTTP ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema de Chamados — API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Healthcheck: GET /health -> JSON { status, timestamp }
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow
        };
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}).AllowAnonymous();

app.MapControllers();

app.Run();
