namespace Chamados.Api.Models.Dtos.Metricas;

public class ProdutividadeTecnicoDto
{
    public long TecnicoId { get; set; }

    public string TecnicoNome { get; set; } = string.Empty;

    public int ChamadosAtribuidos { get; set; }

    public int ChamadosResolvidos { get; set; }

    // Média de horas entre CriadoEm e ResolvidoEm dos chamados já resolvidos.
    // Nulo quando o técnico ainda não resolveu nenhum chamado no período.
    public double? TempoMedioResolucaoHoras { get; set; }
}
