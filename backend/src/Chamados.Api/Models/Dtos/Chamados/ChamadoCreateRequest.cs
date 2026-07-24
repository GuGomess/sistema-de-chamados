using System.ComponentModel.DataAnnotations;

namespace Chamados.Api.Models.Dtos.Chamados;

public class ChamadoCreateRequest
{
    [Required, MaxLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public string Descricao { get; set; } = string.Empty;

    // Opcionais: clientes não escolhem categoria/prioridade ao abrir um chamado
    // (ver ChamadosController.Criar) — nesse caso o servidor atribui a categoria
    // "A Triar" e prioridade "Média" por padrão, ignorando qualquer valor enviado.
    // Para técnico/administrador seguem obrigatórios.
    public long? IdCategoria { get; set; }

    public long? IdPrioridade { get; set; }
}
