using Chamados.Api.Data;
using Chamados.Api.Models.Dtos.Chamados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/prioridades")]
public class PrioridadesController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;

    public PrioridadesController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<PrioridadeDto>>> Listar()
    {
        var prioridades = await _dbContext.Prioridades
            .OrderBy(p => p.Nivel)
            .ToListAsync();

        return Ok(prioridades.Select(PrioridadeDto.FromEntity).ToList());
    }
}
