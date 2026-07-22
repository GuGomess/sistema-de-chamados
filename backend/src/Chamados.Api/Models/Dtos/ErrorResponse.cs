namespace Chamados.Api.Models.Dtos;

public class ErrorResponse
{
    public int Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public Dictionary<string, string[]>? Errors { get; set; }

    public static ErrorResponse Create(int status, string title, string? detail = null) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}
