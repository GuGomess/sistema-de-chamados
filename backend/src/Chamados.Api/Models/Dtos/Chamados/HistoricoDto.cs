using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Dtos.Departamentos;
using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Chamados;

public class HistoricoDto
{
    public long Id { get; set; }

    public long IdChamado { get; set; }

    public UsuarioDto Autor { get; set; } = null!;

    public StatusDto? StatusAnterior { get; set; }

    public StatusDto? StatusNovo { get; set; }

    public DepartamentoDto? DepartamentoAnterior { get; set; }

    public DepartamentoDto? DepartamentoNovo { get; set; }

    public string Acao { get; set; } = string.Empty;

    public string? Detalhe { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public static HistoricoDto FromEntity(Historico historico) => new()
    {
        Id = historico.Id,
        IdChamado = historico.ChamadoId,
        Autor = UsuarioDto.FromEntity(historico.Autor),
        StatusAnterior = historico.StatusAnterior is null ? null : StatusDto.FromEntity(historico.StatusAnterior),
        StatusNovo = historico.StatusNovo is null ? null : StatusDto.FromEntity(historico.StatusNovo),
        DepartamentoAnterior = historico.DepartamentoAnterior is null ? null : DepartamentoDto.FromEntity(historico.DepartamentoAnterior),
        DepartamentoNovo = historico.DepartamentoNovo is null ? null : DepartamentoDto.FromEntity(historico.DepartamentoNovo),
        Acao = historico.Acao,
        Detalhe = historico.Detalhe,
        CriadoEm = historico.CriadoEm
    };
}
