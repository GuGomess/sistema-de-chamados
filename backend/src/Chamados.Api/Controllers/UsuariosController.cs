using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public UsuariosController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar()
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem listar usuários."));
        }

        var usuarios = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .OrderBy(u => u.Nome)
            .ToListAsync();

        return Ok(usuarios.Select(UsuarioDto.FromEntity).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(UsuarioCreateRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem criar usuários."));
        }

        var erros = new Dictionary<string, string[]>();

        var perfilCodigo = request.Perfil?.Trim().ToUpperInvariant() ?? string.Empty;
        if (perfilCodigo != Perfis.Tecnico && perfilCodigo != Perfis.Administrador)
        {
            erros["perfil"] = new[] { "Perfil deve ser TECNICO ou ADMINISTRADOR." };
        }

        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 6)
        {
            erros["senha"] = new[] { "Senha deve ter no mínimo 6 caracteres." };
        }

        var emailExiste = await _dbContext.Usuarios
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExiste)
        {
            erros["email"] = new[] { "Já existe um usuário cadastrado com este e-mail." };
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        var perfilId = perfilCodigo == Perfis.Administrador ? 1L : 2L;
        var perfil = await _dbContext.Perfis.FindAsync(perfilId);
        if (perfil is null)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = new Dictionary<string, string[]> { ["perfil"] = new[] { "Perfil não encontrado." } } });
        }

        var usuario = new Usuario
        {
            Nome = request.Nome,
            Email = request.Email,
            PerfilId = perfil.Id,
            Ativo = true,
            CriadoEm = DateTimeOffset.UtcNow
        };
        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.Senha);

        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        usuario.Perfil = perfil;

        return StatusCode(StatusCodes.Status201Created, UsuarioDto.FromEntity(usuario));
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

    [HttpGet("atribuiveis")]
    public async Task<ActionResult<List<UsuarioDto>>> ListarAtribuiveis()
    {
        var usuarios = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Where(u => u.Ativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();

        var dtos = usuarios.Select(UsuarioDto.FromEntity);

        var atribuiveis = User.IsInRole(Perfis.Administrador)
            ? dtos.Where(u => u.Perfil == Perfis.Tecnico || u.Perfil == Perfis.Administrador)
            : dtos.Where(u => u.Perfil == Perfis.Tecnico);

        return Ok(atribuiveis.ToList());
    }
}
