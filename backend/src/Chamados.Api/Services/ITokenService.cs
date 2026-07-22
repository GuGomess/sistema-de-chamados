using Chamados.Api.Models.Entities;

namespace Chamados.Api.Services;

public interface ITokenService
{
    (string AccessToken, int ExpiresIn) GerarAccessToken(Usuario usuario);

    string GerarRefreshToken();
}
