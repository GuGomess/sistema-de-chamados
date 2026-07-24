using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Chamados.Api.Models.Dtos.Chamados;

public class ComentarioCreateRequest
{
    [Required]
    public string Mensagem { get; set; } = string.Empty;

    public bool Interno { get; set; }

    public List<IFormFile>? Arquivos { get; set; }
}
