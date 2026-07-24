using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Options;
using Chamados.Api.Services;
using Chamados.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// --- Serviços ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<ChamadosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.Configure<SlaMonitorOptions>(builder.Configuration.GetSection(SlaMonitorOptions.SectionName));
builder.Services.AddHostedService<SlaMonitorService>();

builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));

// Kestrel recusa por padrão corpos de requisição maiores que ~30 MB; o limite
// precisa acompanhar o MaxFileSizeBytes configurado para upload de anexos.
var uploadOptions = builder.Configuration.GetSection(UploadOptions.SectionName).Get<UploadOptions>() ?? new UploadOptions();
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = uploadOptions.MaxFileSizeBytes + 1024 * 1024);

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
// Policies nomeadas por papel ficam disponíveis para os controllers que
// ainda serão criados (ex.: [Authorize(Policy = "Administrador")]).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(Perfis.Administrador, policy => policy.RequireRole(Perfis.Administrador))
    .AddPolicy(Perfis.Tecnico, policy => policy.RequireRole(Perfis.Tecnico))
    .AddPolicy(Perfis.Cliente, policy => policy.RequireRole(Perfis.Cliente));

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

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token obtido em POST /api/v1/auth/login (colar só o token, sem o prefixo 'Bearer ')."
    });
    options.OperationFilter<AuthorizeCheckOperationFilter>();
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
