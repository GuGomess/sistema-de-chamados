using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Hubs;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    // Departamento "HelpDesk" (ver ChamadosController.DepartamentoHelpDeskId) —
    // usado só para saber se quem pede a lista de atribuíveis é do HelpDesk.
    private const long DepartamentoHelpDeskId = 1;

    private readonly ChamadosDbContext _dbContext;
    private readonly IHubContext<ChamadosHub> _hubContext;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public UsuariosController(ChamadosDbContext dbContext, IHubContext<ChamadosHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
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
            .Include(u => u.Departamentos)
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
            .Include(u => u.Departamentos)
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
    public async Task<ActionResult<List<UsuarioDto>>> ListarAtribuiveis([FromQuery] long? idDepartamento = null)
    {
        var usuarioId = ObterUsuarioId();

        var usuarios = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Include(u => u.Departamentos)
            .Where(u => u.Ativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();

        var dtos = usuarios.Select(UsuarioDto.FromEntity);

        var atribuiveis = User.IsInRole(Perfis.Administrador)
            ? dtos.Where(u => u.Perfil == Perfis.Tecnico || u.Perfil == Perfis.Administrador)
            : dtos.Where(u => u.Perfil == Perfis.Tecnico);

        // HelpDesk faz a triagem inicial e pode atribuir a qualquer técnico ativo,
        // de qualquer departamento (o chamado migra pro departamento do técnico
        // escolhido — ver ChamadosController.Atribuir); administradores têm a
        // mesma liberdade. Só técnicos de outros departamentos seguem restritos a
        // colegas do próprio departamento (mesma regra de Assumir).
        var chamadorEhHelpDesk = usuarioId.HasValue
            && User.IsInRole(Perfis.Tecnico)
            && await EstaNoDepartamentoAsync(usuarioId.Value, DepartamentoHelpDeskId);

        if (idDepartamento.HasValue && !User.IsInRole(Perfis.Administrador) && !chamadorEhHelpDesk)
        {
            atribuiveis = atribuiveis.Where(u => u.Departamentos.Any(d => d.Id == idDepartamento.Value));
        }

        return Ok(atribuiveis.ToList());
    }

    [HttpPut("{id:long}/departamentos")]
    public async Task<ActionResult<UsuarioDto>> AtualizarDepartamentos(long id, UsuarioDepartamentosRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem alterar departamentos de usuários."));
        }

        var usuario = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Include(u => u.Departamentos)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
        {
            return NotFound(ErrorResponse.Create(404, "Usuário não encontrado."));
        }

        if (Perfis.NormalizarCodigo(usuario.Perfil.Nome) != Perfis.Tecnico)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = new Dictionary<string, string[]> { ["id"] = new[] { "Somente usuários com perfil Técnico podem ter departamentos vinculados." } } });
        }

        var departamentos = await _dbContext.Departamentos
            .Where(d => request.IdsDepartamentos.Contains(d.Id))
            .ToListAsync();

        if (departamentos.Count != request.IdsDepartamentos.Distinct().Count())
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = new Dictionary<string, string[]> { ["idsDepartamentos"] = new[] { "Um ou mais departamentos informados não foram encontrados." } } });
        }

        usuario.Departamentos.Clear();
        foreach (var departamento in departamentos)
        {
            usuario.Departamentos.Add(departamento);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(UsuarioDto.FromEntity(usuario));
    }

    [HttpPatch("{id:long}/ativo")]
    public async Task<ActionResult<UsuarioDto>> AlterarAtivo(long id, UsuarioAtivoRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem ativar ou desativar usuários."));
        }

        // Impede que o administrador desative a própria conta (isso o deixaria
        // sem acesso pra reverter, já que login exige Usuario.Ativo).
        if (!request.Ativo && ObterUsuarioId() == id)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["id"] = new[] { "Você não pode desativar a própria conta." } }
            });
        }

        var usuario = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Include(u => u.Departamentos)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
        {
            return NotFound(ErrorResponse.Create(404, "Usuário não encontrado."));
        }

        usuario.Ativo = request.Ativo;
        await _dbContext.SaveChangesAsync();

        // Desconecta imediatamente qualquer sessão em tempo real do usuário
        // desativado — sem isso, o JWT dele continua valendo (ver OnTokenValidated
        // em Program.cs) e a UI continuaria normalmente até a próxima requisição.
        if (!request.Ativo)
        {
            await _hubContext.Clients.Group($"user-{id}").SendAsync("ContaDesativada");
        }

        return Ok(UsuarioDto.FromEntity(usuario));
    }

    [HttpPatch("{id:long}/senha")]
    public async Task<ActionResult<UsuarioDto>> AlterarSenha(long id, UsuarioSenhaRequest request)
    {
        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem alterar a senha de usuários."));
        }

        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 6)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["novaSenha"] = new[] { "Senha deve ter no mínimo 6 caracteres." } }
            });
        }

        var usuario = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Include(u => u.Departamentos)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
        {
            return NotFound(ErrorResponse.Create(404, "Usuário não encontrado."));
        }

        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.NovaSenha);
        await _dbContext.SaveChangesAsync();

        return Ok(UsuarioDto.FromEntity(usuario));
    }

    // Autoatendimento: qualquer usuário autenticado (cliente, técnico ou
    // administrador) pode editar os próprios nome/e-mail — sem exigir papel
    // algum, diferente dos endpoints acima que são restritos a administrador.
    [HttpPatch("me")]
    public async Task<ActionResult<UsuarioDto>> AtualizarMeuPerfil(MeuPerfilUpdateRequest request)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var usuario = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Include(u => u.Departamentos)
            .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);

        if (usuario is null)
        {
            return Unauthorized();
        }

        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            erros["nome"] = new[] { "Nome é obrigatório." };
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            erros["email"] = new[] { "E-mail é obrigatório." };
        }
        else
        {
            var emailExiste = await _dbContext.Usuarios
                .AnyAsync(u => u.Id != usuario.Id && u.Email.ToLower() == request.Email.Trim().ToLower());
            if (emailExiste)
            {
                erros["email"] = new[] { "Já existe um usuário cadastrado com este e-mail." };
            }
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        usuario.Nome = request.Nome.Trim();
        usuario.Email = request.Email.Trim();
        await _dbContext.SaveChangesAsync();

        return Ok(UsuarioDto.FromEntity(usuario));
    }

    // Diferente de AlterarSenha (admin alterando a senha de terceiros sem
    // precisar da senha atual), autoatendimento exige a senha atual — sem
    // isso, uma sessão aberta esquecida em outro computador poderia trocar a
    // senha (e travar o dono de fora) sem nenhuma confirmação.
    [HttpPatch("me/senha")]
    public async Task<IActionResult> AlterarMinhaSenha(AlterarMinhaSenhaRequest request)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var usuario = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId.Value);
        if (usuario is null)
        {
            return Unauthorized();
        }

        var erros = new Dictionary<string, string[]>();

        var senhaAtualConfere = !string.IsNullOrWhiteSpace(request.SenhaAtual)
            && _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.SenhaAtual) != PasswordVerificationResult.Failed;
        if (!senhaAtualConfere)
        {
            erros["senhaAtual"] = new[] { "Senha atual incorreta." };
        }

        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 6)
        {
            erros["novaSenha"] = new[] { "Senha deve ter no mínimo 6 caracteres." };
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.NovaSenha);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> EstaNoDepartamentoAsync(long usuarioId, long idDepartamento) =>
        await _dbContext.Usuarios
            .Where(u => u.Id == usuarioId)
            .SelectMany(u => u.Departamentos)
            .AnyAsync(d => d.Id == idDepartamento);

    private long? ObterUsuarioId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub) ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}
