using System.ComponentModel.DataAnnotations;

namespace Chamados.Api.Models.Dtos.Chamados;

public class ChamadoCreateRequest
{
    [Required, MaxLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    public long IdCategoria { get; set; }

    [Required]
    public long IdPrioridade { get; set; }
}
