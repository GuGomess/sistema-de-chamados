using System.ComponentModel.DataAnnotations;

namespace Chamados.Api.Models.Dtos.Chamados;

public class ComentarioCreateRequest
{
    [Required]
    public string Mensagem { get; set; } = string.Empty;

    public bool Interno { get; set; }
}
