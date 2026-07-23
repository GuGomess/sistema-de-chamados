namespace Chamados.Api.Models.Entities;

public class Sla
{
    public long Id { get; set; }

    public long PrioridadeId { get; set; }

    public Prioridade Prioridade { get; set; } = null!;

    public int TempoRespostaMin { get; set; }

    public int TempoResolucaoMin { get; set; }

    public bool Ativo { get; set; } = true;
}
