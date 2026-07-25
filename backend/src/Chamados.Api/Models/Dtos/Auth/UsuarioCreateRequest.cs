using System.ComponentModel.DataAnnotations;

namespace Chamados.Api.Models.Dtos.Auth;

public class UsuarioCreateRequest
{
    [Required]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;

    [Required]
    public string Perfil { get; set; } = string.Empty;
}
