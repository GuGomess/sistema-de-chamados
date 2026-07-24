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

    // Perfil "Administrador" (ver Migrations/…_CriaPerfilEUsuario).
    private const long PerfilAdministradorId = 1;

    // Defaults aplicados quando um cliente abre um chamado sem escolher
    // categoria/prioridade (ver Criar) — "A Triar" (Migrations/…_AdicionaCategoriaTriagem)
    // e "Média" (seed em ChamadosDbContext).
    private const long CategoriaTriagemId = 5;
    private const long PrioridadeMediaId = 2;

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
        [FromQuery] SituacaoSla? situacaoSla = null,
        [FromQuery] bool meus = false,
        [FromQuery] bool ocultarFinalizados = false)
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
        else if (meus && (User.IsInRole(Perfis.Tecnico) || User.IsInRole(Perfis.Administrador)))
        {
            // Aba "Meus chamados": assumidos pelo técnico/admin OU abertos por ele
            // mesmo (ex.: administrador que abriu um chamado em nome próprio).
            query = query.Where(c => c.TecnicoId == usuarioId.Value || c.SolicitanteId == usuarioId.Value);
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

        if (ocultarFinalizados)
        {
            query = query.Where(c => !c.Status.Final);
        }

        var descending = sort is null || sort.StartsWith('-');
        var campo = sort?.TrimStart('-');
        IOrderedQueryable<Chamado> ordenado = campo switch
        {
            "id" => descending ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id),
            "titulo" => descending ? query.OrderByDescending(c => c.Titulo) : query.OrderBy(c => c.Titulo),
            // Status e prioridade usam os campos numéricos "ordem"/"nivel" (feitos
            // para representar a sequência de triagem) em vez do nome, para que
            // "crescente/decrescente" siga a ordem de negócio, não a alfabética.
            "status" => descending ? query.OrderByDescending(c => c.Status.Ordem) : query.OrderBy(c => c.Status.Ordem),
            "solicitante" => descending ? query.OrderByDescending(c => c.Solicitante.Nome) : query.OrderBy(c => c.Solicitante.Nome),
            "prioridade" => descending ? query.OrderByDescending(c => c.Prioridade.Nivel) : query.OrderBy(c => c.Prioridade.Nivel),
            "sla" => descending
                ? query.OrderByDescending(c =>
                    (c.SituacaoSlaResposta == SituacaoSla.Vencido || c.SituacaoSlaResolucao == SituacaoSla.Vencido) ? 2 :
                    (c.SituacaoSlaResposta == SituacaoSla.EmRisco || c.SituacaoSlaResolucao == SituacaoSla.EmRisco) ? 1 : 0)
                : query.OrderBy(c =>
                    (c.SituacaoSlaResposta == SituacaoSla.Vencido || c.SituacaoSlaResolucao == SituacaoSla.Vencido) ? 2 :
                    (c.SituacaoSlaResposta == SituacaoSla.EmRisco || c.SituacaoSlaResolucao == SituacaoSla.EmRisco) ? 1 : 0),
            "tecnico" => descending ? query.OrderByDescending(c => c.Tecnico!.Nome) : query.OrderBy(c => c.Tecnico!.Nome),
            // Chamado nunca resolvido (ResolvidoEm nulo) é tratado como "o mais
            // antigo possível": some pro fim tanto em ASC quanto em DESC, em vez de
            // depender do default do Postgres (NULLS FIRST em DESC, que colocaria
            // chamados ainda abertos acima dos resolvidos mais recentemente).
            "resolvidoEm" => descending
                ? query.OrderByDescending(c => c.ResolvidoEm ?? DateTimeOffset.MinValue)
                : query.OrderBy(c => c.ResolvidoEm ?? DateTimeOffset.MinValue),
            "atualizadoEm" => descending ? query.OrderByDescending(c => c.AtualizadoEm) : query.OrderBy(c => c.AtualizadoEm),
            _ => descending ? query.OrderByDescending(c => c.CriadoEm) : query.OrderBy(c => c.CriadoEm)
        };
        // Desempate estável por Id: sem isso, colunas de baixa cardinalidade
        // (status, prioridade, sla, técnico) não garantem ordem consistente entre
        // as requisições de páginas diferentes, podendo duplicar/pular um chamado.
        query = ordenado.ThenBy(c => c.Id);

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

        // Cliente não escolhe categoria/prioridade (não tem acesso a esses conceitos
        // na sua visão) — o servidor ignora qualquer valor enviado e aplica os
        // defaults de triagem, independentemente do que vier no corpo da requisição.
        long idCategoria;
        long idPrioridade;

        if (User.IsInRole(Perfis.Cliente))
        {
            idCategoria = CategoriaTriagemId;
            idPrioridade = PrioridadeMediaId;
        }
        else
        {
            if (request.IdCategoria is null) erros["idCategoria"] = new[] { "Selecione uma categoria." };
            if (request.IdPrioridade is null) erros["idPrioridade"] = new[] { "Selecione uma prioridade." };

            if (erros.Count > 0)
            {
                return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
            }

            idCategoria = request.IdCategoria!.Value;
            idPrioridade = request.IdPrioridade!.Value;
        }

        var categoriaExiste = await _dbContext.Categorias.AnyAsync(c => c.Id == idCategoria);
        if (!categoriaExiste) erros["idCategoria"] = new[] { "Categoria não encontrada." };

        var prioridadeExiste = await _dbContext.Prioridades.AnyAsync(p => p.Id == idPrioridade);
        if (!prioridadeExiste) erros["idPrioridade"] = new[] { "Prioridade não encontrada." };

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        var sla = await _dbContext.Slas.FirstOrDefaultAsync(s => s.PrioridadeId == idPrioridade && s.Ativo);

        var criadoEm = DateTimeOffset.UtcNow;
        var chamado = new Chamado
        {
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            SolicitanteId = usuarioId.Value,
            StatusId = StatusAbertoId,
            CategoriaId = idCategoria,
            PrioridadeId = idPrioridade,
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
        string? statusNovoNome = null;

        if (request.IdStatus.HasValue)
        {
            var novoStatus = await _dbContext.Status.FirstOrDefaultAsync(s => s.Id == request.IdStatus.Value);
            if (novoStatus is null)
                erros["idStatus"] = new[] { "Status não encontrado." };
            else if (request.IdStatus.Value != chamado.StatusId)
            {
                statusAnteriorId = chamado.StatusId;
                statusNovoNome = novoStatus.Nome;
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

            var mensagemStatus = $"Chamado #{chamado.Id} — {chamado.Titulo}: status alterado para {statusNovoNome}.";
            NotificarInteressados(chamado, usuarioId.Value, mensagemStatus, TipoNotificacao.MudancaStatus);

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

        var chamado = await ChamadosComIncludes().FirstOrDefaultAsync(c => c.Id == id);
        if (chamado is null)
        {
            return NotFound(ErrorResponse.Create(404, "Chamado não encontrado."));
        }

        var podeAtribuir = User.IsInRole(Perfis.Administrador)
            || (User.IsInRole(Perfis.Tecnico) && PodeAcessar(chamado, usuarioId.Value));

        if (!podeAtribuir)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores ou técnicos responsáveis podem atribuir um chamado a um técnico."));
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
        var perfilAlvo = tecnico is null ? null : Perfis.NormalizarCodigo(tecnico.Perfil.Nome);
        var chamadorEhAdministrador = User.IsInRole(Perfis.Administrador);

        var alvoValido = tecnico is not null
            && tecnico.Ativo
            && (chamadorEhAdministrador
                ? (perfilAlvo == Perfis.Tecnico || perfilAlvo == Perfis.Administrador)
                : perfilAlvo == Perfis.Tecnico);

        if (!alvoValido)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["idTecnico"] = new[] { "Usuário não pode receber a atribuição." } }
            });
        }

        chamado.TecnicoId = tecnico!.Id;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Atribuição",
            Detalhe = $"Atribuído a {tecnico.Nome}."
        });

        // Notifica o técnico recém-atribuído (ele passa a estar "vinculado" ao
        // chamado) e quem abriu o chamado — chamado.TecnicoId já é o novo
        // técnico neste ponto, então um único NotificarInteressados cobre os dois.
        var mensagemAtribuicao = $"Chamado #{chamado.Id} — {chamado.Titulo}: {tecnico.Nome} foi atribuído como técnico responsável.";
        NotificarInteressados(chamado, usuarioId.Value, mensagemAtribuicao, TipoNotificacao.TecnicoAtribuido);

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

        // Mesmo evento de negócio que /atribuir (chamado passa a ter um técnico
        // responsável) — quem abriu o chamado precisa ser notificado aqui também.
        var usuarioAtual = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId.Value);
        var mensagemAssumir = $"Chamado #{chamado.Id} — {chamado.Titulo}: {usuarioAtual?.Nome ?? "um técnico"} assumiu o chamado.";
        NotificarInteressados(chamado, usuarioId.Value, mensagemAssumir, TipoNotificacao.TecnicoAtribuido, notificarTecnico: false);

        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpPost("{id:long}/reabrir")]
    public async Task<ActionResult<ChamadoDto>> Reabrir(long id)
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para reabrir este chamado."));
        }

        if (!chamado.Status.Final)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["status"] = new[] { "Chamado não está finalizado e não pode ser reaberto." } }
            });
        }

        var usuarioAtual = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId.Value);
        var nomeReabertura = usuarioAtual?.Nome ?? "Usuário";

        chamado.StatusId = StatusAbertoId;
        chamado.ResolvidoEm = null;
        chamado.FechadoEm = null;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Reabertura",
            Detalhe = $"Reaberto por {nomeReabertura}."
        });

        var mensagem = $"Chamado #{chamado.Id} — {chamado.Titulo}: reaberto por {nomeReabertura}.";
        await NotificarAdministradoresAsync(chamado.Id, mensagem, TipoNotificacao.ChamadoReaberto);

        if (chamado.TecnicoId.HasValue && chamado.TecnicoId.Value != usuarioId.Value)
        {
            _dbContext.Notificacoes.Add(new Notificacao
            {
                DestinatarioId = chamado.TecnicoId.Value,
                ChamadoId = chamado.Id,
                Tipo = TipoNotificacao.ChamadoReaberto,
                Mensagem = mensagem
            });
        }

        await _dbContext.SaveChangesAsync();

        var chamadoAtualizado = await ChamadosComIncludes().FirstAsync(c => c.Id == chamado.Id);
        return Ok(ChamadoDto.FromEntity(chamadoAtualizado));
    }

    [HttpPost("{id:long}/fechar-cliente")]
    public async Task<ActionResult<ChamadoDto>> FecharComoCliente(long id, [FromBody] FecharClienteRequest request)
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

        if (!User.IsInRole(Perfis.Cliente) || chamado.SolicitanteId != usuarioId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para fechar este chamado."));
        }

        if (chamado.StatusId == StatusFechadoId)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["status"] = new[] { "Chamado já está fechado." } }
            });
        }

        var tecnicoNome = chamado.Tecnico?.Nome ?? "Nenhum";
        var motivo = string.IsNullOrWhiteSpace(request.Motivo) ? "Não informado" : request.Motivo;

        chamado.StatusId = StatusFechadoId;
        chamado.FechadoEm = DateTimeOffset.UtcNow;
        chamado.ResolvidoEm ??= chamado.FechadoEm;
        chamado.AtualizadoEm = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Fechamento pelo cliente",
            Detalhe = $"Fechado pelo cliente {chamado.Solicitante.Nome}. Técnico: {tecnicoNome}. Motivo: {motivo}."
        });

        // O motivo precisa aparecer na própria notificação — ele fica registrado
        // no histórico, mas antes disso não havia nenhuma superfície visível
        // para técnico/administrador verem o texto escrito pelo cliente.
        var mensagem = string.IsNullOrWhiteSpace(request.Motivo)
            ? $"Chamado #{chamado.Id} — {chamado.Titulo}: fechado pelo cliente {chamado.Solicitante.Nome}."
            : $"Chamado #{chamado.Id} — {chamado.Titulo}: fechado pelo cliente {chamado.Solicitante.Nome}. Motivo: {request.Motivo}";
        await NotificarAdministradoresAsync(chamado.Id, mensagem, TipoNotificacao.FechadoPorCliente);

        if (chamado.TecnicoId.HasValue)
        {
            _dbContext.Notificacoes.Add(new Notificacao
            {
                DestinatarioId = chamado.TecnicoId.Value,
                ChamadoId = chamado.Id,
                Tipo = TipoNotificacao.FechadoPorCliente,
                Mensagem = mensagem
            });
        }

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

        if (!User.IsInRole(Perfis.Administrador))
        {
            var mensagem = $"Chamado #{chamado.Id} — {chamado.Titulo}: prazo de resolução ajustado manualmente para {prazoNovoTexto}. Justificativa: {request.Justificativa}";
            await NotificarAdministradoresAsync(chamado.Id, mensagem);
        }

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

        if (!User.IsInRole(Perfis.Administrador))
        {
            var mensagem = $"Chamado #{chamado.Id} — {chamado.Titulo}: prazo de resposta ajustado manualmente para {prazoNovoTexto}.";
            await NotificarAdministradoresAsync(chamado.Id, mensagem);
        }

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

        if (User.IsInRole(Perfis.Tecnico))
        {
            // O técnico já vê as avaliações às quais tem direito através de
            // GET /avaliacoes (com a filtragem correta por avaliação). Aqui,
            // como o histórico não referencia qual avaliação foi registrada,
            // a entrada genérica "Avaliação registrada" é sempre omitida para
            // evitar vazar a existência de uma avaliação privada/oculta.
            historico = historico.Where(h => h.Acao != "Avaliação registrada").ToList();
        }

        return Ok(historico.Select(HistoricoDto.FromEntity).ToList());
    }

    [HttpGet("{id:long}/avaliacoes")]
    public async Task<ActionResult<List<AvaliacaoDto>>> ListarAvaliacoes(long id)
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

        // Mais recente primeiro: um chamado reaberto e resolvido de novo pode
        // acumular uma avaliação por ciclo, e a mais nova deve "empilhar acima".
        var avaliacoes = await _dbContext.Avaliacoes
            .Include(a => a.Autor).ThenInclude(u => u.Perfil)
            .Where(a => a.ChamadoId == id)
            .OrderByDescending(a => a.CriadoEm)
            .ToListAsync();

        bool PodeVer(Avaliacao a)
        {
            if (User.IsInRole(Perfis.Administrador)) return true;
            if (User.IsInRole(Perfis.Cliente)) return a.AutorId == usuarioId.Value;
            if (User.IsInRole(Perfis.Tecnico)) return a.Publica && !a.Oculta;
            return false;
        }

        return Ok(avaliacoes.Where(PodeVer).Select(AvaliacaoDto.FromEntity).ToList());
    }

    [HttpPost("{id:long}/avaliacao")]
    public async Task<ActionResult<AvaliacaoDto>> CriarAvaliacao(long id, [FromBody] AvaliacaoCreateRequest request)
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

        if (!User.IsInRole(Perfis.Cliente) || chamado.SolicitanteId != usuarioId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para avaliar este chamado."));
        }

        // Só se avalia um chamado "Resolvido" — se o cliente o fechou diretamente
        // (ou um técnico/admin o fechou sem passar por "Resolvido"), não há o que
        // avaliar. Isso também é o que reabre a possibilidade de avaliar de novo:
        // reabertura tira o chamado de "Resolvido" e uma nova resolução volta a habilitar.
        if (chamado.StatusId != StatusResolvidoId)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["status"] = new[] { "Chamado ainda não foi resolvido." } }
            });
        }

        // "Ciclo atual" = desde o último ResolvidoEm. Reabrir e resolver de novo
        // gera um ResolvidoEm mais recente, liberando uma nova avaliação que se
        // empilha sobre as anteriores (ver ChamadosDbContext: índice não é mais único).
        var resolvidoEm = chamado.ResolvidoEm ?? DateTimeOffset.MinValue;
        var cicloJaAvaliado = await _dbContext.Avaliacoes.AnyAsync(a => a.ChamadoId == id && a.CriadoEm >= resolvidoEm);
        if (cicloJaAvaliado)
        {
            return Conflict(ErrorResponse.Create(409, "Este atendimento já foi avaliado."));
        }

        if (request.Nota < 0 || request.Nota > 5)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["nota"] = new[] { "Nota deve estar entre 0 e 5." } }
            });
        }

        var avaliacao = new Avaliacao
        {
            ChamadoId = id,
            AutorId = usuarioId.Value,
            Nota = request.Nota,
            Comentario = request.Comentario,
            Publica = request.Publica
        };

        _dbContext.Avaliacoes.Add(avaliacao);
        await _dbContext.SaveChangesAsync();

        _dbContext.Historicos.Add(new Historico
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId.Value,
            Acao = "Avaliação registrada",
            Detalhe = $"Nota {avaliacao.Nota}/5."
        });

        var mensagem = $"Chamado #{chamado.Id} — {chamado.Titulo}: recebeu uma avaliação ({avaliacao.Nota}/5).";
        await NotificarAdministradoresAsync(chamado.Id, mensagem, TipoNotificacao.NovaAvaliacao);

        if (chamado.TecnicoId.HasValue && request.Publica)
        {
            _dbContext.Notificacoes.Add(new Notificacao
            {
                DestinatarioId = chamado.TecnicoId.Value,
                ChamadoId = chamado.Id,
                Tipo = TipoNotificacao.NovaAvaliacao,
                Mensagem = mensagem
            });
        }

        await _dbContext.SaveChangesAsync();

        var avaliacaoCriada = await _dbContext.Avaliacoes
            .Include(a => a.Autor).ThenInclude(u => u.Perfil)
            .FirstAsync(a => a.Id == avaliacao.Id);

        return CreatedAtAction(nameof(ListarAvaliacoes), new { id }, AvaliacaoDto.FromEntity(avaliacaoCriada));
    }

    [HttpPatch("{id:long}/avaliacoes/{avaliacaoId:long}")]
    public async Task<ActionResult<AvaliacaoDto>> AtualizarAvaliacao(long id, long avaliacaoId, [FromBody] AvaliacaoUpdateRequest request)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var avaliacao = await _dbContext.Avaliacoes
            .Include(a => a.Autor).ThenInclude(u => u.Perfil)
            .FirstOrDefaultAsync(a => a.Id == avaliacaoId && a.ChamadoId == id);
        if (avaliacao is null)
        {
            return NotFound(ErrorResponse.Create(404, "Avaliação não encontrada."));
        }

        // Só o próprio cliente autor pode editar a nota/comentário/visibilidade
        // que ele escreveu — nem administrador edita conteúdo (só oculta).
        if (!User.IsInRole(Perfis.Cliente) || avaliacao.AutorId != usuarioId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Sem permissão para editar esta avaliação."));
        }

        if (request.Nota < 0 || request.Nota > 5)
        {
            return UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Falha de validação",
                Errors = new Dictionary<string, string[]> { ["nota"] = new[] { "Nota deve estar entre 0 e 5." } }
            });
        }

        avaliacao.Nota = request.Nota;
        avaliacao.Comentario = request.Comentario;
        avaliacao.Publica = request.Publica;
        avaliacao.EditadoEm = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(AvaliacaoDto.FromEntity(avaliacao));
    }

    [HttpPatch("{id:long}/avaliacoes/{avaliacaoId:long}/ocultar")]
    public async Task<ActionResult<AvaliacaoDto>> OcultarAvaliacao(long id, long avaliacaoId, [FromBody] AvaliacaoOcultarRequest request)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (!User.IsInRole(Perfis.Administrador))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse.Create(403, "Apenas administradores podem ocultar avaliações."));
        }

        var avaliacao = await _dbContext.Avaliacoes
            .Include(a => a.Autor).ThenInclude(u => u.Perfil)
            .FirstOrDefaultAsync(a => a.Id == avaliacaoId && a.ChamadoId == id);
        if (avaliacao is null)
        {
            return NotFound(ErrorResponse.Create(404, "Avaliação não encontrada."));
        }

        avaliacao.Oculta = request.Oculta;
        await _dbContext.SaveChangesAsync();

        return Ok(AvaliacaoDto.FromEntity(avaliacao));
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
            .Include(c => c.Anexos).ThenInclude(a => a.Autor).ThenInclude(u => u.Perfil)
            .OrderBy(c => c.CriadoEm)
            .ToListAsync();

        return Ok(comentarios.Select(ComentarioDto.FromEntity).ToList());
    }

    [HttpPost("{id:long}/comentarios")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ComentarioDto>> CriarComentario(long id, [FromForm] ComentarioCreateRequest request)
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

        var arquivos = request.Arquivos ?? [];
        foreach (var arquivo in arquivos)
        {
            var (valido, titulo, detalhe) = ValidarArquivo(arquivo);
            if (!valido)
            {
                return UnprocessableEntity(ErrorResponse.Create(422, titulo!, detalhe));
            }
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

        // Primeiro comentário de técnico/administrador satisfaz o SLA de resposta:
        // a situação fica congelada a partir daqui e o monitoramento periódico
        // (SlaMonitorService) para de reavaliar/notificar sobre ela.
        if (!User.IsInRole(Perfis.Cliente) && chamado.PrimeiraRespostaEm is null)
        {
            var agora = DateTimeOffset.UtcNow;
            chamado.PrimeiraRespostaEm = agora;
            if (chamado.PrazoResposta.HasValue)
            {
                chamado.SituacaoSlaResposta = SlaSituacaoCalculator.Calcular(chamado.CriadoEm, chamado.PrazoResposta.Value, agora);
            }
            chamado.AtualizadoEm = agora;
        }

        foreach (var arquivo in arquivos)
        {
            var anexo = await CriarAnexoAsync(id, usuarioId.Value, arquivo);
            anexo.Comentario = comentario;
            _dbContext.Anexos.Add(anexo);
        }

        // Nota interna não é visível ao cliente, então ele não é notificado
        // sobre ela (evitaria expor que existe um comentário que ele não pode ver).
        var autorNome = await _dbContext.Usuarios.Where(u => u.Id == usuarioId.Value).Select(u => u.Nome).FirstOrDefaultAsync() ?? "Alguém";
        var mensagemComentario = $"Chamado #{chamado.Id} — {chamado.Titulo}: novo comentário de {autorNome}.";
        NotificarInteressados(chamado, usuarioId.Value, mensagemComentario, TipoNotificacao.NovoComentario, notificarSolicitante: !interno);

        await _dbContext.SaveChangesAsync();

        var comentarioCriado = await _dbContext.Comentarios
            .Include(c => c.Autor).ThenInclude(u => u.Perfil)
            .Include(c => c.Anexos).ThenInclude(a => a.Autor).ThenInclude(u => u.Perfil)
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
            .Where(a => a.ChamadoId == id && a.ComentarioId == null)
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

        var (valido, titulo, detalhe) = ValidarArquivo(arquivo);
        if (!valido)
        {
            return UnprocessableEntity(ErrorResponse.Create(422, titulo!, detalhe));
        }

        var anexo = await CriarAnexoAsync(id, usuarioId.Value, arquivo);

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

    private (bool Valido, string? Titulo, string? Detalhe) ValidarArquivo(IFormFile arquivo)
    {
        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!_uploadOptions.AllowedExtensions.Contains(extensao))
        {
            return (false, "Tipo de arquivo não permitido.", $"Extensões aceitas: {string.Join(", ", _uploadOptions.AllowedExtensions)}");
        }

        if (arquivo.Length > _uploadOptions.MaxFileSizeBytes)
        {
            return (false, "Arquivo excede o tamanho máximo permitido.", $"Tamanho máximo: {_uploadOptions.MaxFileSizeBytes} bytes.");
        }

        return (true, null, null);
    }

    private async Task<Anexo> CriarAnexoAsync(long chamadoId, long autorId, IFormFile arquivo)
    {
        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var nomeArmazenado = $"{Guid.NewGuid()}{extensao}";
        var caminhoRelativo = Path.Combine(chamadoId.ToString(), nomeArmazenado);
        var caminhoCompleto = Path.Combine(_uploadOptions.StoragePath, caminhoRelativo);

        Directory.CreateDirectory(Path.GetDirectoryName(caminhoCompleto)!);

        await using (var destino = System.IO.File.Create(caminhoCompleto))
        {
            await arquivo.CopyToAsync(destino);
        }

        return new Anexo
        {
            ChamadoId = chamadoId,
            AutorId = autorId,
            NomeArquivo = Path.GetFileName(arquivo.FileName),
            Caminho = caminhoRelativo,
            TipoMime = string.IsNullOrWhiteSpace(arquivo.ContentType) ? "application/octet-stream" : arquivo.ContentType,
            TamanhoBytes = arquivo.Length
        };
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

    // Notifica quem abriu o chamado e o técnico vinculado sobre uma atualização
    // (comentário, mudança de status, atribuição de técnico) — nunca o próprio
    // autor da ação, e nunca duplicado se solicitante e técnico forem a mesma pessoa.
    private void NotificarInteressados(
        Chamado chamado,
        long autorId,
        string mensagem,
        TipoNotificacao tipo,
        bool notificarSolicitante = true,
        bool notificarTecnico = true)
    {
        if (notificarSolicitante && chamado.SolicitanteId != autorId)
        {
            _dbContext.Notificacoes.Add(new Notificacao
            {
                DestinatarioId = chamado.SolicitanteId,
                ChamadoId = chamado.Id,
                Tipo = tipo,
                Mensagem = mensagem
            });
        }

        if (notificarTecnico
            && chamado.TecnicoId.HasValue
            && chamado.TecnicoId.Value != autorId
            && chamado.TecnicoId.Value != chamado.SolicitanteId)
        {
            _dbContext.Notificacoes.Add(new Notificacao
            {
                DestinatarioId = chamado.TecnicoId.Value,
                ChamadoId = chamado.Id,
                Tipo = tipo,
                Mensagem = mensagem
            });
        }
    }

    private async Task NotificarAdministradoresAsync(long chamadoId, string mensagem, TipoNotificacao tipo = TipoNotificacao.PrazoAjustado)
    {
        var administradorIds = await _dbContext.Usuarios
            .Where(u => u.PerfilId == PerfilAdministradorId && u.Ativo)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var administradorId in administradorIds)
        {
            _dbContext.Notificacoes.Add(new Notificacao
            {
                DestinatarioId = administradorId,
                ChamadoId = chamadoId,
                Tipo = tipo,
                Mensagem = mensagem
            });
        }
    }
}
