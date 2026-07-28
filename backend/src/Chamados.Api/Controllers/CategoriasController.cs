using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Chamados;
using Chamados.Api.Models.Entities;
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

    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Criar(CategoriaCreateRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem criar categorias."));
        }

        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            erros["nome"] = new[] { "Nome é obrigatório." };
        }
        else if (await _dbContext.Categorias.AnyAsync(c => c.Nome.ToLower() == request.Nome.Trim().ToLower()))
        {
            erros["nome"] = new[] { "Já existe uma categoria com este nome." };
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        var categoria = new Categoria
        {
            Nome = request.Nome.Trim(),
            Descricao = request.Descricao,
            Ativa = true
        };

        _dbContext.Categorias.Add(categoria);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, CategoriaDto.FromEntity(categoria));
    }
}
