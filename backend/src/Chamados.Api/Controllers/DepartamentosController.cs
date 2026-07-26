using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Departamentos;
using Chamados.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/departamentos")]
public class DepartamentosController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;

    public DepartamentosController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartamentoDto>>> Listar([FromQuery] bool apenasAtivos = false)
    {
        var query = _dbContext.Departamentos.AsQueryable();

        if (apenasAtivos)
        {
            query = query.Where(d => d.Ativo);
        }

        var departamentos = await query
            .OrderBy(d => d.Nome)
            .ToListAsync();

        return Ok(departamentos.Select(DepartamentoDto.FromEntity).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<DepartamentoDto>> Criar(DepartamentoCreateRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem criar departamentos."));
        }

        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            erros["nome"] = new[] { "Nome é obrigatório." };
        }
        else if (await _dbContext.Departamentos.AnyAsync(d => d.Nome.ToLower() == request.Nome.Trim().ToLower()))
        {
            erros["nome"] = new[] { "Já existe um departamento com este nome." };
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        var departamento = new Departamento
        {
            Nome = request.Nome.Trim(),
            Descricao = request.Descricao,
            Ativo = true,
            CriadoEm = DateTimeOffset.UtcNow
        };

        _dbContext.Departamentos.Add(departamento);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, DepartamentoDto.FromEntity(departamento));
    }

    [HttpPatch("{id:long}")]
    public async Task<ActionResult<DepartamentoDto>> Atualizar(long id, DepartamentoUpdateRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem editar departamentos."));
        }

        var departamento = await _dbContext.Departamentos.FirstOrDefaultAsync(d => d.Id == id);
        if (departamento is null)
        {
            return NotFound(ErrorResponse.Create(404, "Departamento não encontrado."));
        }

        var erros = new Dictionary<string, string[]>();

        if (request.Nome is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Nome))
            {
                erros["nome"] = new[] { "Nome é obrigatório." };
            }
            else if (await _dbContext.Departamentos.AnyAsync(d => d.Id != id && d.Nome.ToLower() == request.Nome.Trim().ToLower()))
            {
                erros["nome"] = new[] { "Já existe um departamento com este nome." };
            }
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        if (request.Nome is not null)
        {
            departamento.Nome = request.Nome.Trim();
        }

        if (request.Descricao is not null)
        {
            departamento.Descricao = request.Descricao;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(DepartamentoDto.FromEntity(departamento));
    }

    [HttpPatch("{id:long}/status")]
    public async Task<ActionResult<DepartamentoDto>> AlterarStatus(long id, DepartamentoStatusRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem ativar ou inativar departamentos."));
        }

        var departamento = await _dbContext.Departamentos.FirstOrDefaultAsync(d => d.Id == id);
        if (departamento is null)
        {
            return NotFound(ErrorResponse.Create(404, "Departamento não encontrado."));
        }

        departamento.Ativo = request.Ativo;
        await _dbContext.SaveChangesAsync();

        return Ok(DepartamentoDto.FromEntity(departamento));
    }
}
