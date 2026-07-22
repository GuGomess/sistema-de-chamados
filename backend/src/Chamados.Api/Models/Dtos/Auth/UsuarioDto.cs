using System.Globalization;
using System.Text;
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
        Perfil = ToPerfilCode(usuario.Perfil.Nome),
        Ativo = usuario.Ativo,
        CriadoEm = usuario.CriadoEm
    };

    private static string ToPerfilCode(string perfilNome)
    {
        var normalized = perfilNome.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().ToUpperInvariant();
    }
}
