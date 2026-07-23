using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Chamados;
using Chamados.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/slas")]
public class SlasController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;

    public SlasController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<SlaDto>>> Listar()
    {
        var slas = await _dbContext.Slas
            .Include(s => s.Prioridade)
            .OrderBy(s => s.Prioridade.Nivel)
            .ToListAsync();

        return Ok(slas.Select(SlaDto.FromEntity).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<SlaDto>> CriarOuAtualizar(SlaInput request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem configurar SLA."));
        }

        var erros = new Dictionary<string, string[]>();

        if (!await _dbContext.Prioridades.AnyAsync(p => p.Id == request.IdPrioridade))
            erros["idPrioridade"] = new[] { "Prioridade não encontrada." };

        if (request.TempoRespostaMin < 1)
            erros["tempoRespostaMin"] = new[] { "Tempo de resposta deve ser maior ou igual a 1 minuto." };

        if (request.TempoResolucaoMin < 1)
            erros["tempoResolucaoMin"] = new[] { "Tempo de resolução deve ser maior ou igual a 1 minuto." };

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        var sla = await _dbContext.Slas.FirstOrDefaultAsync(s => s.PrioridadeId == request.IdPrioridade);
        var criando = sla is null;

        if (sla is null)
        {
            sla = new Sla { PrioridadeId = request.IdPrioridade };
            _dbContext.Slas.Add(sla);
        }

        sla.TempoRespostaMin = request.TempoRespostaMin;
        sla.TempoResolucaoMin = request.TempoResolucaoMin;
        sla.Ativo = request.Ativo;

        await _dbContext.SaveChangesAsync();

        var dto = SlaDto.FromEntity(sla);
        return criando ? StatusCode(StatusCodes.Status201Created, dto) : Ok(dto);
    }
}
