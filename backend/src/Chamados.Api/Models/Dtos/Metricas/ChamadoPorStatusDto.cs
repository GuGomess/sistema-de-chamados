namespace Chamados.Api.Models.Dtos.Metricas;

public class ChamadoPorStatusDto
{
    public long StatusId { get; set; }

    public string StatusNome { get; set; } = string.Empty;

    public int Quantidade { get; set; }
}
