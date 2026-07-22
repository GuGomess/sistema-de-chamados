namespace Chamados.Api.Models.Entities;

public class Status
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public short Ordem { get; set; }

    public bool Final { get; set; }

    public ICollection<Chamado> Chamados { get; set; } = new List<Chamado>();
}
