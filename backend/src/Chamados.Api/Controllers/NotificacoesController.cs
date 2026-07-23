using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Notificacoes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/notificacoes")]
public class NotificacoesController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;

    public NotificacoesController(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificacaoDto>>> Listar([FromQuery] bool apenasNaoLidas = false)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var query = _dbContext.Notificacoes.Where(n => n.DestinatarioId == usuarioId.Value);
        if (apenasNaoLidas)
        {
            query = query.Where(n => !n.Lida);
        }

        var notificacoes = await query
            .OrderByDescending(n => n.CriadoEm)
            .Take(50)
            .ToListAsync();

        return Ok(notificacoes.Select(NotificacaoDto.FromEntity).ToList());
    }

    [HttpGet("nao-lidas/contagem")]
    public async Task<ActionResult<NotificacaoContagemDto>> ContarNaoLidas()
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var naoLidas = await _dbContext.Notificacoes
            .CountAsync(n => n.DestinatarioId == usuarioId.Value && !n.Lida);

        return Ok(new NotificacaoContagemDto { NaoLidas = naoLidas });
    }

    [HttpPatch("{id:long}/lida")]
    public async Task<IActionResult> MarcarComoLida(long id)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var notificacao = await _dbContext.Notificacoes.FirstOrDefaultAsync(n => n.Id == id);
        if (notificacao is null)
        {
            return NotFound(ErrorResponse.Create(404, "Notificação não encontrada."));
        }

        if (notificacao.DestinatarioId != usuarioId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para acessar esta notificação."));
        }

        if (!notificacao.Lida)
        {
            notificacao.Lida = true;
            await _dbContext.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPatch("lidas")]
    public async Task<IActionResult> MarcarTodasComoLidas()
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        await _dbContext.Notificacoes
            .Where(n => n.DestinatarioId == usuarioId.Value && !n.Lida)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.Lida, true));

        return NoContent();
    }

    private long? ObterUsuarioId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub) ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}
