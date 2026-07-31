using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
using Chamados.Api.Models.Dtos.Metricas;
using Chamados.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/metricas")]
public class MetricasController : ControllerBase
{
    // Todo chamado é criado em triagem no HelpDesk (ver
    // ChamadosController.DepartamentoHelpDeskId) — usado aqui para decidir se o
    // técnico chamador tem visão ampla (HelpDesk) ou restrita ao(s) próprio(s)
    // departamento(s), mesmo critério usado em Listar/ResumoSla.
    private const long DepartamentoHelpDeskId = 1;

    private readonly ChamadosDbContext _dbContext;
    private readonly IEscopoChamadoService _escopoChamadoService;

    public MetricasController(ChamadosDbContext dbContext, IEscopoChamadoService escopoChamadoService)
    {
        _dbContext = dbContext;
        _escopoChamadoService = escopoChamadoService;
    }

    [HttpGet("chamados-por-status")]
    public async Task<ActionResult<List<ChamadoPorStatusDto>>> ChamadosPorStatus(
        [FromQuery] DateTimeOffset? dataInicio = null,
        [FromQuery] DateTimeOffset? dataFim = null)
    {
        var usuarioId = _escopoChamadoService.ObterUsuarioId(User);
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        // Métricas são uma visão de staff — cliente não tem motivo para ver a
        // distribuição de chamados por status de toda a base (só enxerga os
        // próprios chamados de qualquer forma).
        if (!User.IsInRole(Perfis.Administrador) && !User.IsInRole(Perfis.Tecnico))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores ou técnicos podem acessar métricas."));
        }

        var query = _dbContext.Chamados.AsQueryable();

        // Mesmo escopo por departamento usado em ChamadosController.Listar/ResumoSla:
        // administrador e técnico do HelpDesk (triagem) têm visão ampla; técnico de
        // outro departamento só vê os chamados dos departamentos aos quais pertence.
        if (User.IsInRole(Perfis.Tecnico) && !await _escopoChamadoService.EstaNoDepartamentoAsync(usuarioId.Value, DepartamentoHelpDeskId))
        {
            var idsDepartamentos = await _escopoChamadoService.IdsDepartamentosDoUsuarioAsync(usuarioId.Value);
            query = query.Where(c => idsDepartamentos.Contains(c.DepartamentoId));
        }

        if (dataInicio.HasValue) query = query.Where(c => c.CriadoEm >= dataInicio.Value);
        if (dataFim.HasValue) query = query.Where(c => c.CriadoEm <= dataFim.Value);

        // Conta todos os status (aberto ou final) — diferente de ResumoSla, que só
        // olha chamados em aberto; aqui o objetivo é a distribuição completa.
        var agregados = await query
            .GroupBy(c => new { c.StatusId, c.Status.Nome, c.Status.Ordem })
            .OrderBy(g => g.Key.Ordem)
            .Select(g => new ChamadoPorStatusDto
            {
                StatusId = g.Key.StatusId,
                StatusNome = g.Key.Nome,
                Quantidade = g.Count()
            })
            .ToListAsync();

        return Ok(agregados);
    }

    [HttpGet("produtividade-tecnicos")]
    public async Task<ActionResult<List<ProdutividadeTecnicoDto>>> ProdutividadeTecnicos(
        [FromQuery] DateTimeOffset? dataInicio = null,
        [FromQuery] DateTimeOffset? dataFim = null)
    {
        var usuarioId = _escopoChamadoService.ObterUsuarioId(User);
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (!User.IsInRole(Perfis.Administrador) && !User.IsInRole(Perfis.Tecnico))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores ou técnicos podem acessar métricas."));
        }

        // Mesmo filtro de "quem é técnico" usado em UsuariosController.ListarTecnicos:
        // Perfis.NormalizarCodigo não traduz para SQL (mapeamento manual sem
        // globalização de ICU), então a filtragem por perfil é feita em memória.
        var usuariosAtivos = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .Where(u => u.Ativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();

        var tecnicos = usuariosAtivos
            .Where(u => Perfis.NormalizarCodigo(u.Perfil.Nome) == Perfis.Tecnico)
            .ToList();

        // Produtividade entre colegas é um dado sensível: técnico (que não seja
        // administrador) só enxerga a própria linha nesta métrica.
        if (!User.IsInRole(Perfis.Administrador))
        {
            tecnicos = tecnicos.Where(u => u.Id == usuarioId.Value).ToList();
        }

        var idsTecnicos = tecnicos.Select(u => u.Id).ToList();

        var chamadosQuery = _dbContext.Chamados
            .Where(c => c.TecnicoId != null && idsTecnicos.Contains(c.TecnicoId.Value));

        if (dataInicio.HasValue) chamadosQuery = chamadosQuery.Where(c => c.CriadoEm >= dataInicio.Value);
        if (dataFim.HasValue) chamadosQuery = chamadosQuery.Where(c => c.CriadoEm <= dataFim.Value);

        var chamadosDosTecnicos = await chamadosQuery
            .Select(c => new { c.TecnicoId, c.CriadoEm, c.ResolvidoEm })
            .ToListAsync();

        // ILookup: indexador retorna sequência vazia para uma chave ausente, então
        // um técnico sem nenhum chamado no período não precisa de tratamento à parte.
        var chamadosPorTecnico = chamadosDosTecnicos.ToLookup(c => c.TecnicoId!.Value);

        var resultado = tecnicos.Select(tecnico =>
        {
            var chamadosDoTecnico = chamadosPorTecnico[tecnico.Id].ToList();
            var resolvidos = chamadosDoTecnico.Where(c => c.ResolvidoEm.HasValue).ToList();

            return new ProdutividadeTecnicoDto
            {
                TecnicoId = tecnico.Id,
                TecnicoNome = tecnico.Nome,
                ChamadosAtribuidos = chamadosDoTecnico.Count,
                ChamadosResolvidos = resolvidos.Count,
                TempoMedioResolucaoHoras = resolvidos.Count == 0
                    ? null
                    : resolvidos.Average(c => (c.ResolvidoEm!.Value - c.CriadoEm).TotalHours)
            };
        }).ToList();

        return Ok(resultado);
    }
}
