using Chamados.Api.Data;
using Chamados.Api.Models.Dtos.Chamados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/status")]
public class StatusController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;

    public StatusController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<StatusDto>>> Listar()
    {
        var status = await _dbContext.Status
            .OrderBy(s => s.Ordem)
            .ToListAsync();

        return Ok(status.Select(StatusDto.FromEntity).ToList());
    }
}
