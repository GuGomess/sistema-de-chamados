using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// --- Serviços ---
builder.Services.AddControllers();

// Health checks (liveness). Checagens de dependências (ex.: PostgreSQL)
// serão adicionadas quando o EF Core / migrations entrarem no projeto.
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
});

app.MapControllers();

app.Run();
