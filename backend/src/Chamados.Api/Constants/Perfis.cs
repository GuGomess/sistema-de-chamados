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

    // Mapeamento manual (sem depender de string.Normalize/ICU) porque o runtime
    // roda em modo globalization-invariant (imagem Alpine sem icu-libs), onde
    // Normalize/CharUnicodeInfo não removem acentos corretamente.
    private const string ComAcento = "áàãâäÁÀÃÂÄéèêëÉÈÊËíìîïÍÌÎÏóòõôöÓÒÕÔÖúùûüÚÙÛÜçÇ";
    private const string SemAcento = "aaaaaAAAAAeeeeEEEEiiiiIIIIooooooOOOOOOuuuuUUUUcC";

    public static string NormalizarCodigo(string perfilNome)
    {
        var builder = new StringBuilder(perfilNome.Length);
        foreach (var c in perfilNome)
        {
            var indice = ComAcento.IndexOf(c);
            builder.Append(indice >= 0 ? SemAcento[indice] : c);
        }

        return builder.ToString().ToUpperInvariant();
    }
}
