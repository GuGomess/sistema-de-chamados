using System.Net.Http.Json;
using Chamados.Api.Models.Dtos.Chamados;

namespace Chamados.Api.Tests.Integration.Support;

/// <summary>
/// Helpers para o ciclo de vida de chamados usados nos testes de integração:
/// criação (sempre como cliente, único jeito de abrir chamado hoje) e um
/// comentário mínimo (via multipart/form-data, exigido por
/// ChamadosController.CriarComentario) — necessário porque "Resolver" exige ao
/// menos um comentário desde a última reabertura.
/// </summary>
internal static class ChamadoHelper
{
    public static async Task<ChamadoDto> CriarComoClienteAsync(HttpClient clienteClient, string? titulo = null, string? descricao = null)
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var response = await clienteClient.PostAsJsonAsync("/api/v1/chamados", new ChamadoCreateRequest
        {
            Titulo = titulo ?? $"Chamado de teste {sufixo}",
            Descricao = descricao ?? "Descrição gerada pela suíte de testes automatizados."
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ChamadoDto>(ContaHelper.JsonOptions))!;
    }

    public static async Task<HttpResponseMessage> ComentarAsync(HttpClient client, long chamadoId, string mensagem, bool interno = false)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(mensagem), "Mensagem" },
            { new StringContent(interno.ToString()), "Interno" }
        };

        return await client.PostAsync($"/api/v1/chamados/{chamadoId}/comentarios", content);
    }
}
