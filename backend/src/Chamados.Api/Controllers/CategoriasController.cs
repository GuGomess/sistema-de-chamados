using Chamados.Api.Data;
using Chamados.Api.Models.Dtos.Chamados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;

    public CategoriasController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoriaDto>>> Listar()
    {
        var categorias = await _dbContext.Categorias
            .Where(c => c.Ativa)
            .OrderBy(c => c.Nome)
            .ToListAsync();

        return Ok(categorias.Select(CategoriaDto.FromEntity).ToList());
    }
}
