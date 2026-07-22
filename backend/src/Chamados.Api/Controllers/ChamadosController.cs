using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Chamados;
using Chamados.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/chamados")]
public class ChamadosController : ControllerBase
{
    private const long StatusAbertoId = 1;

    private readonly ChamadosDbContext _dbContext;

    public ChamadosController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<ChamadoPageDto>> Listar(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        [FromQuery] long? idStatus = null,
        [FromQuery] long? idCategoria = null,
        [FromQuery] long? idPrioridade = null,
        [FromQuery] long? idTecnico = null,
        [FromQuery] string? q = null,
        [FromQuery] DateTimeOffset? dataInicio = null,
        [FromQuery] DateTimeOffset? dataFim = null)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = ChamadosComIncludes();

        if (User.IsInRole(Perfis.Cliente))
        {
            query = query.Where(c => c.SolicitanteId == usuarioId.Value);
        }
        else if (User.IsInRole(Perfis.Tecnico))
        {
            query = query.Where(c => c.TecnicoId == usuarioId.Value || c.TecnicoId == null);
        }

        if (idStatus.HasValue) query = query.Where(c => c.StatusId == idStatus.Value);
        if (idCategoria.HasValue) query = query.Where(c => c.CategoriaId == idCategoria.Value);
        if (idPrioridade.HasValue) query = query.Where(c => c.PrioridadeId == idPrioridade.Value);
        if (idTecnico.HasValue) query = query.Where(c => c.TecnicoId == idTecnico.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c => EF.Functions.ILike(c.Titulo, $"%{q}%") || EF.Functions.ILike(c.Descricao, $"%{q}%"));
        }

        if (dataInicio.HasValue) query = query.Where(c => c.CriadoEm >= dataInicio.Value);
        if (dataFim.HasValue) query = query.Where(c => c.CriadoEm <= dataFim.Value);

        var descending = sort is null || sort.StartsWith('-');
        var campo = sort?.TrimStart('-');
        query = campo switch
        {
            "titulo" => descending ? query.OrderByDescending(c => c.Titulo) : query.OrderBy(c => c.Titulo),
            "atualizadoEm" => descending ? query.OrderByDescending(c => c.AtualizadoEm) : query.OrderBy(c => c.AtualizadoEm),
            _ => descending ? query.OrderByDescending(c => c.CriadoEm) : query.OrderBy(c => c.CriadoEm)
        };

        var totalItems = await query.CountAsync();
        var chamados = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new ChamadoPageDto
        {
            Items = chamados.Select(ChamadoDto.FromEntity).ToList(),
            Meta = new PageMetaDto
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        });
    }

    [HttpPost]
    public async Task<ActionResult<ChamadoDto>> Criar(ChamadoCreateRequest request)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var erros = new Dictionary<string, string[]>();

        var categoriaExiste = await _dbContext.Categorias.AnyAsync(c => c.Id == request.IdCategoria);
        if (!categoriaExiste) erros["idCategoria"] = new[] { "Categoria não encontrada." };

        var prioridadeExiste = await _dbContext.Prioridades.AnyAsync(p => p.Id == request.IdPrioridade);
        if (!prioridadeExiste) erros["idPrioridade"] = new[] { "Prioridade não encontrada." };

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        var chamado = new Chamado
        {
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            SolicitanteId = usuarioId.Value,
            StatusId = StatusAbertoId,
            CategoriaId = request.IdCategoria,
            PrioridadeId = request.IdPrioridade
        };

        _dbContext.Chamados.Add(chamado);
        await _dbContext.SaveChangesAsync();

        var chamadoCriado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return CreatedAtAction(nameof(Detalhar), new { id = chamado.Id }, ChamadoDto.FromEntity(chamadoCriado));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ChamadoDto>> Detalhar(long id)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var chamado = await ChamadosComIncludes().FirstOrDefaultAsync(c => c.Id == id);
        if (chamado is null)
        {
            return NotFound(ErrorResponse.Create(404, "Chamado não encontrado."));
        }

        if (!PodeAcessar(chamado, usuarioId.Value))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para acessar este chamado."));
        }

        return Ok(ChamadoDto.FromEntity(chamado));
    }

    [HttpPatch("{id:long}")]
    public async Task<ActionResult<ChamadoDto>> Atualizar(long id, [FromBody] ChamadoUpdateRequest request)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var chamado = await _dbContext.Chamados.FirstOrDefaultAsync(c => c.Id == id);
        if (chamado is null)
        {
            return NotFound(ErrorResponse.Create(404, "Chamado não encontrado."));
        }

        if (!PodeAcessar(chamado, usuarioId.Value))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para atualizar este chamado."));
        }

        var erros = new Dictionary<string, string[]>();

        if (request.IdStatus.HasValue)
        {
            if (!await _dbContext.Status.AnyAsync(s => s.Id == request.IdStatus.Value))
                erros["idStatus"] = new[] { "Status não encontrado." };
            else
                chamado.StatusId = request.IdStatus.Value;
        }

        if (request.IdCategoria.HasValue)
        {
            if (!await _dbContext.Categorias.AnyAsync(c => c.Id == request.IdCategoria.Value))
                erros["idCategoria"] = new[] { "Categoria não encontrada." };
            else
                chamado.CategoriaId = request.IdCategoria.Value;
        }

        if (request.IdPrioridade.HasValue)
        {
            if (!await _dbContext.Prioridades.AnyAsync(p => p.Id == request.IdPrioridade.Value))
                erros["idPrioridade"] = new[] { "Prioridade não encontrada." };
            else
                chamado.PrioridadeId = request.IdPrioridade.Value;
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpPost("{id:long}/atribuir")]
    public async Task<ActionResult<ChamadoDto>> Atribuir(long id, [FromBody] AtribuirTecnicoRequest request)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem atribuir um chamado a um técnico."));
        }

        var chamado = await ChamadosComIncludes().FirstOrDefaultAsync(c => c.Id == id);
        if (chamado is null)
        {
            return NotFound(ErrorResponse.Create(404, "Chamado não encontrado."));
        }

        if (chamado.Status.Final)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["status"] = new[] { "Chamado está em status final e não pode ser atribuído." } }
            });
        }

        var tecnico = await _dbContext.Usuarios.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == request.IdTecnico);
        if (tecnico is null || !tecnico.Ativo || Perfis.NormalizarCodigo(tecnico.Perfil.Nome) != Perfis.Tecnico)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["idTecnico"] = new[] { "Usuário técnico não encontrado." } }
            });
        }

        chamado.TecnicoId = tecnico.Id;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpPost("{id:long}/assumir")]
    public async Task<ActionResult<ChamadoDto>> Assumir(long id)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (!User.IsInRole(Perfis.Tecnico))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas técnicos podem assumir um chamado."));
        }

        var chamado = await ChamadosComIncludes().FirstOrDefaultAsync(c => c.Id == id);
        if (chamado is null)
        {
            return NotFound(ErrorResponse.Create(404, "Chamado não encontrado."));
        }

        if (chamado.Status.Final)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["status"] = new[] { "Chamado está em status final e não pode ser assumido." } }
            });
        }

        if (chamado.TecnicoId is not null)
        {
            return Conflict(ErrorResponse.Create(409, "Chamado já está atribuído a um técnico."));
        }

        chamado.TecnicoId = usuarioId.Value;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpPost("{id:long}/liberar")]
    public async Task<ActionResult<ChamadoDto>> Liberar(long id)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var chamado = await ChamadosComIncludes().FirstOrDefaultAsync(c => c.Id == id);
        if (chamado is null)
        {
            return NotFound(ErrorResponse.Create(404, "Chamado não encontrado."));
        }

        var podeLiberar = User.IsInRole(Perfis.Administrador)
            || (User.IsInRole(Perfis.Tecnico) && chamado.TecnicoId == usuarioId.Value);

        if (!podeLiberar)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para liberar este chamado."));
        }

        if (chamado.TecnicoId is null)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["idTecnico"] = new[] { "Chamado não possui técnico atribuído." } }
            });
        }

        chamado.TecnicoId = null;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    private IQueryable<Chamado> ChamadosComIncludes() =>
        _dbContext.Chamados
            .Include(c => c.Solicitante).ThenInclude(u => u.Perfil)
            .Include(c => c.Tecnico!).ThenInclude(u => u.Perfil)
            .Include(c => c.Status)
            .Include(c => c.Categoria)
            .Include(c => c.Prioridade);

    private bool PodeAcessar(Chamado chamado, long usuarioId)
    {
        if (User.IsInRole(Perfis.Administrador)) return true;
        if (User.IsInRole(Perfis.Tecnico)) return chamado.TecnicoId == usuarioId || chamado.TecnicoId is null;
        if (User.IsInRole(Perfis.Cliente)) return chamado.SolicitanteId == usuarioId;
        return false;
    }

    private long? ObterUsuarioId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub) ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}
