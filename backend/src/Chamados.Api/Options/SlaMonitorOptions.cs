namespace Chamados.Api.Options;

public class SlaMonitorOptions
{
    public const string SectionName = "SlaMonitor";

    public int IntervalSeconds { get; set; } = 300;
}
