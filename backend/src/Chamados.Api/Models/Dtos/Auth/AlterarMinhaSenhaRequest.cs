namespace Chamados.Api.Models.Dtos.Auth;

public class AlterarMinhaSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;

    public string NovaSenha { get; set; } = string.Empty;
}
