namespace Chamados.Api.Models.Dtos.Chamados;

public class ChamadoPageDto
{
    public IReadOnlyList<ChamadoDto> Items { get; set; } = Array.Empty<ChamadoDto>();

    public PageMetaDto Meta { get; set; } = null!;
}

public class PageMetaDto
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
