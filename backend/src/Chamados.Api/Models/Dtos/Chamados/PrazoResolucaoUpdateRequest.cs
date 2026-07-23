namespace Chamados.Api.Models.Dtos.Chamados;

public class PrazoResolucaoUpdateRequest
{
    public DateTimeOffset PrazoResolucao { get; set; }

    public string Justificativa { get; set; } = string.Empty;
}
