using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;

    public UsuariosController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("tecnicos")]
    public async Task<ActionResult<List<UsuarioDto>>> ListarTecnicos()
    {
        var usuarios = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Where(u => u.Ativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();

        var tecnicos = usuarios
            .Select(UsuarioDto.FromEntity)
            .Where(u => u.Perfil == Perfis.Tecnico)
            .ToList();

        return Ok(tecnicos);
    }
}
