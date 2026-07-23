using Chamados.Api.Models.Entities;

namespace Chamados.Api.Services;

// Lógica de cálculo de situação de SLA compartilhada entre o job periódico
// (SlaMonitorService) e qualquer ação que altere um prazo diretamente (ex.:
// ajuste manual de prazo de resolução), para que o novo estado fique
// correto imediatamente, sem esperar o próximo tick do job.
public static class SlaSituacaoCalculator
{
    private const double LimiarRiscoPercentual = 0.8;

    public static SituacaoSla Calcular(DateTimeOffset criadoEm, DateTimeOffset prazo, DateTimeOffset agora)
    {
        var duracaoTotal = prazo - criadoEm;
        if (agora >= prazo || duracaoTotal <= TimeSpan.Zero)
        {
            return SituacaoSla.Vencido;
        }

        var percentualDecorrido = (agora - criadoEm) / duracaoTotal;
        return percentualDecorrido >= LimiarRiscoPercentual ? SituacaoSla.EmRisco : SituacaoSla.EmDia;
    }
}
