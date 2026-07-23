using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Chamados;

public class SlaDto
{
    public long Id { get; set; }

    public long IdPrioridade { get; set; }

    public int TempoRespostaMin { get; set; }

    public int TempoResolucaoMin { get; set; }

    public bool Ativo { get; set; }

    public static SlaDto FromEntity(Sla sla) => new()
    {
        Id = sla.Id,
        IdPrioridade = sla.PrioridadeId,
        TempoRespostaMin = sla.TempoRespostaMin,
        TempoResolucaoMin = sla.TempoResolucaoMin,
        Ativo = sla.Ativo
    };
}

public class SlaInput
{
    public long IdPrioridade { get; set; }

    public int TempoRespostaMin { get; set; }

    public int TempoResolucaoMin { get; set; }

    public bool Ativo { get; set; } = true;
}
