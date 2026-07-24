using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Chamados;
using Chamados.Api.Models.Entities;
using Chamados.Api.Options;
using Chamados.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/chamados")]
public class ChamadosController : ControllerBase
{
    private const long StatusAbertoId = 1;
    private const long StatusResolvidoId = 4;
    private const long StatusFechadoId = 5;

    private readonly ChamadosDbContext _dbContext;
    private readonly UploadOptions _uploadOptions;

    public ChamadosController(ChamadosDbContext dbContext, IOptions<UploadOptions> uploadOptions)
    {
        _dbContext = dbContext;
        _uploadOptions = uploadOptions.Value;
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
        [FromQuery] DateTimeOffset? dataFim = null,
        [FromQuery] SituacaoSla? situacaoSla = null)
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

        if (situacaoSla.HasValue)
        {
            query = query.Where(c => c.SituacaoSlaResposta == situacaoSla.Value || c.SituacaoSlaResolucao == situacaoSla.Value);
        }

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

    [HttpGet("resumo-sla")]
    public async Task<ActionResult<ResumoSlaDto>> ResumoSla()
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var query = _dbContext.Chamados.Where(c => !c.Status.Final);

        if (User.IsInRole(Perfis.Cliente))
        {
            query = query.Where(c => c.SolicitanteId == usuarioId.Value);
        }
        else if (User.IsInRole(Perfis.Tecnico))
        {
            query = query.Where(c => c.TecnicoId == usuarioId.Value || c.TecnicoId == null);
        }

        var emRisco = await query.CountAsync(c => c.SituacaoSlaResposta == SituacaoSla.EmRisco || c.SituacaoSlaResolucao == SituacaoSla.EmRisco);
        var vencidos = await query.CountAsync(c => c.SituacaoSlaResposta == SituacaoSla.Vencido || c.SituacaoSlaResolucao == SituacaoSla.Vencido);

        return Ok(new ResumoSlaDto { EmRisco = emRisco, Vencidos = vencidos });
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

        var sla = await _dbContext.Slas.FirstOrDefaultAsync(s => s.PrioridadeId == request.IdPrioridade && s.Ativo);

        var criadoEm = DateTimeOffset.UtcNow;
        var chamado = new Chamado
        {
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            SolicitanteId = usuarioId.Value,
            StatusId = StatusAbertoId,
            CategoriaId = request.IdCategoria,
            PrioridadeId = request.IdPrioridade,
            PrazoResposta = sla is null ? null : criadoEm.AddMinutes(sla.TempoRespostaMin),
            PrazoResolucao = sla is null ? null : criadoEm.AddMinutes(sla.TempoResolucaoMin)
        };

        _dbContext.Chamados.Add(chamado);
        await _dbContext.SaveChangesAsync();

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            StatusAnteriorId = null,
            StatusNovoId = chamado.StatusId,
            Acao = "Abertura"
        });
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
        long? statusAnteriorId = null;

        if (request.IdStatus.HasValue)
        {
            if (!await _dbContext.Status.AnyAsync(s => s.Id == request.IdStatus.Value))
                erros["idStatus"] = new[] { "Status não encontrado." };
            else if (request.IdStatus.Value != chamado.StatusId)
            {
                statusAnteriorId = chamado.StatusId;
                chamado.StatusId = request.IdStatus.Value;

                if (chamado.StatusId == StatusResolvidoId)
                {
                    chamado.ResolvidoEm = DateTimeOffset.UtcNow;
                }
                else if (chamado.StatusId == StatusFechadoId)
                {
                    chamado.FechadoEm = DateTimeOffset.UtcNow;
                    chamado.ResolvidoEm ??= DateTimeOffset.UtcNow;
                }
                else
                {
                    chamado.ResolvidoEm = null;
                    chamado.FechadoEm = null;
                }
            }
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

        if (statusAnteriorId.HasValue)
        {
            _dbContext.Historicos.Add(new Historico
            {
                ChamadoId = chamado.Id,
                AutorId = usuarioId.Value,
                StatusAnteriorId = statusAnteriorId.Value,
                StatusNovoId = chamado.StatusId,
                Acao = "Mudança de status"
            });
            await _dbContext.SaveChangesAsync();
        }

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

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Atribuição",
            Detalhe = $"Atribuído a {tecnico.Nome}."
        });
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

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Chamado assumido"
        });
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

        var tecnicoLiberadoNome = chamado.Tecnico?.Nome;

        chamado.TecnicoId = null;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Liberação",
            Detalhe = tecnicoLiberadoNome is null ? null : $"Liberado por {tecnicoLiberadoNome}."
        });
        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpPatch("{id:long}/prazo-resolucao")]
    public async Task<ActionResult<ChamadoDto>> AjustarPrazoResolucao(long id, [FromBody] PrazoResolucaoUpdateRequest request)
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

        var podeAjustar = User.IsInRole(Perfis.Administrador)
            || (User.IsInRole(Perfis.Tecnico) && chamado.TecnicoId == usuarioId.Value);

        if (!podeAjustar)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para ajustar o prazo de resolução deste chamado."));
        }

        if (chamado.Status.Final)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["status"] = new[] { "Chamado está em status final e não pode ter o prazo ajustado." } }
            });
        }

        if (string.IsNullOrWhiteSpace(request.Justificativa))
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["justificativa"] = new[] { "Justificativa é obrigatória." } }
            });
        }

        var prazoAnterior = chamado.PrazoResolucao;
        chamado.PrazoResolucao = request.PrazoResolucao;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        chamado.SituacaoSlaResolucao = SlaSituacaoCalculator.Calcular(chamado.CriadoEm, request.PrazoResolucao, chamado.AtualizadoEm);
        await _dbContext.SaveChangesAsync();

        var prazoAnteriorTexto = prazoAnterior is null ? "não definido" : prazoAnterior.Value.UtcDateTime.ToString("dd/MM/yyyy HH:mm") + " UTC";
        var prazoNovoTexto = request.PrazoResolucao.UtcDateTime.ToString("dd/MM/yyyy HH:mm") + " UTC";

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Ajuste de prazo de resolução",
            Detalhe = $"Prazo alterado de {prazoAnteriorTexto} para {prazoNovoTexto}. Justificativa: {request.Justificativa}"
        });
        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpPatch("{id:long}/prazo-resposta")]
    public async Task<ActionResult<ChamadoDto>> AjustarPrazoResposta(long id, [FromBody] PrazoRespostaUpdateRequest request)
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

        var podeAjustar = User.IsInRole(Perfis.Administrador)
            || (User.IsInRole(Perfis.Tecnico) && chamado.TecnicoId == usuarioId.Value);

        if (!podeAjustar)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para ajustar o prazo de resposta deste chamado."));
        }

        if (chamado.Status.Final)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["status"] = new[] { "Chamado está em status final e não pode ter o prazo ajustado." } }
            });
        }

        var prazoAnterior = chamado.PrazoResposta;
        chamado.PrazoResposta = request.PrazoResposta;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        chamado.SituacaoSlaResposta = SlaSituacaoCalculator.Calcular(chamado.CriadoEm, request.PrazoResposta, chamado.AtualizadoEm);
        await _dbContext.SaveChangesAsync();

        var prazoAnteriorTexto = prazoAnterior is null ? "não definido" : prazoAnterior.Value.UtcDateTime.ToString("dd/MM/yyyy HH:mm") + " UTC";
        var prazoNovoTexto = request.PrazoResposta.UtcDateTime.ToString("dd/MM/yyyy HH:mm") + " UTC";

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Ajuste de prazo de resposta",
            Detalhe = $"Prazo alterado de {prazoAnteriorTexto} para {prazoNovoTexto}."
        });
        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpGet("{id:long}/historico")]
    public async Task<ActionResult<List<HistoricoDto>>> Historico(long id)
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para acessar este chamado."));
        }

        var historico = await _dbContext.Historicos
            .Where(h => h.ChamadoId == id)
            .Include(h => h.Autor).ThenInclude(u => u.Perfil)
            .Include(h => h.StatusAnterior)
            .Include(h => h.StatusNovo)
            .OrderBy(h => h.CriadoEm)
            .ToListAsync();

        return Ok(historico.Select(HistoricoDto.FromEntity).ToList());
    }

    [HttpGet("{id:long}/comentarios")]
    public async Task<ActionResult<List<ComentarioDto>>> ListarComentarios(long id)
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para acessar este chamado."));
        }

        var query = _dbContext.Comentarios.Where(c => c.ChamadoId == id);

        if (User.IsInRole(Perfis.Cliente))
        {
            query = query.Where(c => !c.Interno);
        }

        var comentarios = await query
            .Include(c => c.Autor).ThenInclude(u => u.Perfil)
            .OrderBy(c => c.CriadoEm)
            .ToListAsync();

        return Ok(comentarios.Select(ComentarioDto.FromEntity).ToList());
    }

    [HttpPost("{id:long}/comentarios")]
    public async Task<ActionResult<ComentarioDto>> CriarComentario(long id, [FromBody] ComentarioCreateRequest request)
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para comentar neste chamado."));
        }

        var interno = request.Interno && !User.IsInRole(Perfis.Cliente);

        var comentario = new Comentario
        {
            ChamadoId = id,
            AutorId = usuarioId.Value,
            Mensagem = request.Mensagem,
            Interno = interno
        };

        _dbContext.Comentarios.Add(comentario);
        await _dbContext.SaveChangesAsync();

        var comentarioCriado = await _dbContext.Comentarios
            .Include(c => c.Autor).ThenInclude(u => u.Perfil)
            .FirstAsync(c => c.Id == comentario.Id);

        return CreatedAtAction(nameof(ListarComentarios), new { id }, ComentarioDto.FromEntity(comentarioCriado));
    }

    [HttpGet("{id:long}/anexos")]
    public async Task<ActionResult<List<AnexoDto>>> ListarAnexos(long id)
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para acessar este chamado."));
        }

        var anexos = await _dbContext.Anexos
            .Where(a => a.ChamadoId == id)
            .Include(a => a.Autor).ThenInclude(u => u.Perfil)
            .OrderBy(a => a.CriadoEm)
            .ToListAsync();

        return Ok(anexos.Select(AnexoDto.FromEntity).ToList());
    }

    [HttpPost("{id:long}/anexos")]
    public async Task<ActionResult<AnexoDto>> EnviarAnexo(long id, IFormFile? arquivo)
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para enviar anexos neste chamado."));
        }

        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(ErrorResponse.Create(400, "Nenhum arquivo enviado."));
        }

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!_uploadOptions.AllowedExtensions.Contains(extensao))
        {
            return UnprocessableEntity(ErrorResponse.Create(422, "Tipo de arquivo não permitido.", $"Extensões aceitas: {string.Join(", ", _uploadOptions.AllowedExtensions)}"));
        }

        if (arquivo.Length > _uploadOptions.MaxFileSizeBytes)
        {
            return UnprocessableEntity(ErrorResponse.Create(422, "Arquivo excede o tamanho máximo permitido.", $"Tamanho máximo: {_uploadOptions.MaxFileSizeBytes} bytes."));
        }

        var nomeArmazenado = $"{Guid.NewGuid()}{extensao}";
        var caminhoRelativo = Path.Combine(id.ToString(), nomeArmazenado);
        var caminhoCompleto = Path.Combine(_uploadOptions.StoragePath, caminhoRelativo);

        Directory.CreateDirectory(Path.GetDirectoryName(caminhoCompleto)!);

        await using (var destino = System.IO.File.Create(caminhoCompleto))
        {
            await arquivo.CopyToAsync(destino);
        }

        var anexo = new Anexo
        {
            ChamadoId = id,
            AutorId = usuarioId.Value,
            NomeArquivo = Path.GetFileName(arquivo.FileName),
            Caminho = caminhoRelativo,
            TipoMime = string.IsNullOrWhiteSpace(arquivo.ContentType) ? "application/octet-stream" : arquivo.ContentType,
            TamanhoBytes = arquivo.Length
        };

        _dbContext.Anexos.Add(anexo);
        await _dbContext.SaveChangesAsync();

        var anexoCriado = await _dbContext.Anexos
            .Include(a => a.Autor).ThenInclude(u => u.Perfil)
            .FirstAsync(a => a.Id == anexo.Id);

        return CreatedAtAction(nameof(ListarAnexos), new { id }, AnexoDto.FromEntity(anexoCriado));
    }

    [HttpGet("{id:long}/anexos/{anexoId:long}/download")]
    public async Task<IActionResult> BaixarAnexo(long id, long anexoId)
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para acessar este chamado."));
        }

        var anexo = await _dbContext.Anexos.FirstOrDefaultAsync(a => a.Id == anexoId && a.ChamadoId == id);
        if (anexo is null)
        {
            return NotFound(ErrorResponse.Create(404, "Anexo não encontrado."));
        }

        var caminhoCompleto = Path.Combine(_uploadOptions.StoragePath, anexo.Caminho);
        if (!System.IO.File.Exists(caminhoCompleto))
        {
            return NotFound(ErrorResponse.Create(404, "Arquivo do anexo não encontrado no armazenamento."));
        }

        return PhysicalFile(Path.GetFullPath(caminhoCompleto), anexo.TipoMime, anexo.NomeArquivo);
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
