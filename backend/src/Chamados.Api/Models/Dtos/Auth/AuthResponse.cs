namespace Chamados.Api.Models.Dtos.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }

    public UsuarioDto Usuario { get; set; } = null!;
}
