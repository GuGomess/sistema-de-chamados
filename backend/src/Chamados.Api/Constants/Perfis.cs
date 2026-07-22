using System.Globalization;
using System.Text;

namespace Chamados.Api.Constants;

/// <summary>
/// Códigos de papel usados em claims de autorização e no contrato da API
/// (docs/openapi.yaml), independentes da grafia armazenada em Perfil.Nome.
/// </summary>
public static class Perfis
{
    public const string Administrador = "ADMINISTRADOR";
    public const string Tecnico = "TECNICO";
    public const string Cliente = "CLIENTE";

    public static string NormalizarCodigo(string perfilNome)
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
