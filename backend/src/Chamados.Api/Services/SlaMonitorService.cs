using Chamados.Api.Constants;
using Chamados.Api.Data;
using Chamados.Api.Models.Entities;
using Chamados.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chamados.Api.Services;

// Recalcula periodicamente a situação de SLA (resposta/resolução) dos
// chamados em aberto e registra as transições no histórico. Limiar de
// "em risco" fixado em 80% do tempo decorrido do prazo.
public class SlaMonitorService : BackgroundService
{
    // Perfil "Administrador" (ver Migrations/…_CriaPerfilEUsuario) — administradores
    // ativos recebem notificação de toda transição de SLA, além do técnico atribuído.
    private const long PerfilAdministradorId = 1L;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitorService> _logger;
    private readonly TimeSpan _interval;

    public SlaMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<SlaMonitorOptions> options,
        ILogger<SlaMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                await AtualizarSituacoesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha ao atualizar situação de SLA dos chamados.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task AtualizarSituacoesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChamadosDbContext>();

        var autorSistemaId = await dbContext.Usuarios
            .Where(u => u.Email == UsuarioSistema.Email)
            .Select(u => (long?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (autorSistemaId is null)
        {
            _logger.LogWarning(
                "Usuário Sistema ({Email}) não encontrado; monitoramento de SLA não pode registrar histórico.",
                UsuarioSistema.Email);
            return;
        }

        var chamados = await dbContext.Chamados
            .Include(c => c.Status)
            .Where(c => !c.Status.Final)
            .ToListAsync(cancellationToken);

        if (chamados.Count == 0)
        {
            return;
        }

        var agora = DateTimeOffset.UtcNow;
        var historicos = new List<Historico>();
        var notificacoes = new List<Notificacao>();
        List<long>? administradorIds = null;

        foreach (var chamado in chamados)
        {
            // Já teve primeira resposta de técnico/administrador: SLA de resposta
            // fica congelado (ver ChamadosController.CriarComentario), não reavaliar.
            if (chamado.PrimeiraRespostaEm is null &&
                TentarCalcularNovaSituacao(chamado.CriadoEm, chamado.PrazoResposta, chamado.SituacaoSlaResposta, agora, out var novaSituacaoResposta))
            {
                var anterior = chamado.SituacaoSlaResposta;
                chamado.SituacaoSlaResposta = novaSituacaoResposta;
                historicos.Add(CriarHistorico(chamado.Id, autorSistemaId.Value, "resposta", anterior, novaSituacaoResposta));

                if (novaSituacaoResposta != SituacaoSla.EmDia)
                {
                    administradorIds ??= await ObterAdministradoresAtivosAsync(dbContext, cancellationToken);
                    notificacoes.AddRange(CriarNotificacoes(chamado, "resposta", novaSituacaoResposta, administradorIds));
                }
            }

            if (TentarCalcularNovaSituacao(chamado.CriadoEm, chamado.PrazoResolucao, chamado.SituacaoSlaResolucao, agora, out var novaSituacaoResolucao))
            {
                var anterior = chamado.SituacaoSlaResolucao;
                chamado.SituacaoSlaResolucao = novaSituacaoResolucao;
                historicos.Add(CriarHistorico(chamado.Id, autorSistemaId.Value, "resolução", anterior, novaSituacaoResolucao));

                if (novaSituacaoResolucao != SituacaoSla.EmDia)
                {
                    administradorIds ??= await ObterAdministradoresAtivosAsync(dbContext, cancellationToken);
                    notificacoes.AddRange(CriarNotificacoes(chamado, "resolução", novaSituacaoResolucao, administradorIds));
                }
            }
        }

        if (historicos.Count > 0)
        {
            dbContext.Historicos.AddRange(historicos);
            dbContext.Notificacoes.AddRange(notificacoes);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Monitoramento de SLA: {Quantidade} transição(ões) registrada(s), {Notificacoes} notificação(ões) gerada(s).",
                historicos.Count,
                notificacoes.Count);
        }
    }

    private static async Task<List<long>> ObterAdministradoresAtivosAsync(ChamadosDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.Usuarios
            .Where(u => u.PerfilId == PerfilAdministradorId && u.Ativo)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

    private static IEnumerable<Notificacao> CriarNotificacoes(Chamado chamado, string rotulo, SituacaoSla situacao, List<long> administradorIds)
    {
        var tipo = situacao == SituacaoSla.Vencido ? TipoNotificacao.SlaVencido : TipoNotificacao.SlaEmRisco;
        var descricaoSituacao = situacao == SituacaoSla.Vencido ? "venceu" : "está em risco de vencer";
        var mensagem = $"Chamado #{chamado.Id} — {chamado.Titulo}: SLA de {rotulo} {descricaoSituacao}.";

        var destinatarios = new HashSet<long>(administradorIds);
        if (chamado.TecnicoId.HasValue)
        {
            destinatarios.Add(chamado.TecnicoId.Value);
        }

        return destinatarios.Select(destinatarioId => new Notificacao
        {
            DestinatarioId = destinatarioId,
            ChamadoId = chamado.Id,
            Tipo = tipo,
            Mensagem = mensagem
        });
    }

    private static bool TentarCalcularNovaSituacao(
        DateTimeOffset criadoEm,
        DateTimeOffset? prazo,
        SituacaoSla situacaoAtual,
        DateTimeOffset agora,
        out SituacaoSla novaSituacao)
    {
        novaSituacao = situacaoAtual;
        if (prazo is null)
        {
            return false;
        }

        novaSituacao = SlaSituacaoCalculator.Calcular(criadoEm, prazo.Value, agora);
        return novaSituacao != situacaoAtual;
    }

    private static Historico CriarHistorico(long chamadoId, long autorId, string rotulo, SituacaoSla anterior, SituacaoSla nova)
    {
        return new Historico
        {
            ChamadoId = chamadoId,
            AutorId = autorId,
            Acao = $"SLA de {rotulo} atualizado automaticamente",
            Detalhe = $"Situação alterada de {anterior} para {nova}."
        };
    }
}
