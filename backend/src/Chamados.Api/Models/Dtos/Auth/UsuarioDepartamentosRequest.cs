namespace Chamados.Api.Models.Dtos.Auth;

public class UsuarioDepartamentosRequest
{
    public long[] IdsDepartamentos { get; set; } = Array.Empty<long>();
}
