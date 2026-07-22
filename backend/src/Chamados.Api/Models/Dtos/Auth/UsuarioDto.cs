using Chamados.Api.Constants;
using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Auth;

public class UsuarioDto
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public bool Ativo { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public static UsuarioDto FromEntity(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Email = usuario.Email,
        Perfil = Perfis.NormalizarCodigo(usuario.Perfil.Nome),
        Ativo = usuario.Ativo,
        CriadoEm = usuario.CriadoEm
    };
}
