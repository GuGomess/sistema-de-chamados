using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Chamados.Api.Swagger;

// Sem isso, a exigência de Bearer token no Swagger é global e também marca
// endpoints [AllowAnonymous] (ex.: login) como se precisassem de token,
// o que não reflete o comportamento real da API nem o contrato em
// docs/openapi.yaml (que já isenta esses endpoints via "security: []").
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata.Any(m => m is IAllowAnonymous);
        if (isAnonymous)
        {
            return;
        }

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                { new OpenApiSecuritySchemeReference("Bearer", context.Document), new List<string>() }
            }
        };
    }
}
