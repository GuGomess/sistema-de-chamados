namespace Chamados.Api.Models.Dtos.Chamados;

public class CategoriaCreateRequest
{
    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }
}
