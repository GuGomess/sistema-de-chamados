using Chamados.Api.Models.Entities;
using Chamados.Api.Services;

namespace Chamados.Api.Tests.Unit;

/// <summary>
/// Testes unitários puros (sem banco) para SlaSituacaoCalculator.Calcular —
/// cobrem os limites de negócio: limiar de risco (80% do prazo decorrido) e o
/// próprio prazo, além do caso de janela de prazo inválida (duração <= 0).
/// </summary>
public class SlaSituacaoCalculatorTests
{
    private static readonly DateTimeOffset CriadoEm = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Janela de 10 horas: 80% (limiar de risco) cai exatamente às 8h.
    private static readonly DateTimeOffset Prazo = CriadoEm.AddHours(10);

    [Fact]
    public void Calcular_MuitoAntesDoLimiarDeRisco_RetornaEmDia()
    {
        var agora = CriadoEm.AddHours(4); // 40% decorrido

        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, Prazo, agora);

        Assert.Equal(SituacaoSla.EmDia, situacao);
    }

    [Fact]
    public void Calcular_UmSegundoAntesDoLimiarDeRisco_RetornaEmDia()
    {
        // 8h menos 1s: ainda abaixo dos 80%, borda inferior do limiar.
        var agora = CriadoEm.AddHours(8).AddSeconds(-1);

        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, Prazo, agora);

        Assert.Equal(SituacaoSla.EmDia, situacao);
    }

    [Fact]
    public void Calcular_ExatamenteNoLimiarDeRisco_RetornaEmRisco()
    {
        // Exatamente 80% decorrido (8h de 10h) — a comparação usa ">=", então o
        // limiar exato já conta como risco.
        var agora = CriadoEm.AddHours(8);

        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, Prazo, agora);

        Assert.Equal(SituacaoSla.EmRisco, situacao);
    }

    [Fact]
    public void Calcular_EntreLimiarDeRiscoEPrazo_RetornaEmRisco()
    {
        var agora = CriadoEm.AddHours(9); // 90% decorrido

        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, Prazo, agora);

        Assert.Equal(SituacaoSla.EmRisco, situacao);
    }

    [Fact]
    public void Calcular_UmSegundoAntesDoPrazo_RetornaEmRisco()
    {
        var agora = Prazo.AddSeconds(-1);

        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, Prazo, agora);

        Assert.Equal(SituacaoSla.EmRisco, situacao);
    }

    [Fact]
    public void Calcular_ExatamenteNoPrazo_RetornaVencido()
    {
        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, Prazo, Prazo);

        Assert.Equal(SituacaoSla.Vencido, situacao);
    }

    [Fact]
    public void Calcular_DepoisDoPrazo_RetornaVencido()
    {
        var agora = Prazo.AddHours(1);

        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, Prazo, agora);

        Assert.Equal(SituacaoSla.Vencido, situacao);
    }

    [Fact]
    public void Calcular_PrazoIgualCriadoEm_DuracaoZero_RetornaVencido()
    {
        // Janela de prazo com duração zero (prazo == criadoEm): tratado como já
        // vencido mesmo "agora" coincidindo com o próprio instante de criação.
        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, CriadoEm, CriadoEm);

        Assert.Equal(SituacaoSla.Vencido, situacao);
    }

    [Fact]
    public void Calcular_PrazoAnteriorACriadoEm_DuracaoNegativa_RetornaVencido()
    {
        // Janela de prazo inválida (prazo antes da criação) — guarda defensiva:
        // mesmo com "agora" anterior ao prazo, duração <= 0 já força Vencido.
        var prazoInvalido = CriadoEm.AddHours(-1);
        var agora = CriadoEm.AddMinutes(-30);

        var situacao = SlaSituacaoCalculator.Calcular(CriadoEm, prazoInvalido, agora);

        Assert.Equal(SituacaoSla.Vencido, situacao);
    }
}
