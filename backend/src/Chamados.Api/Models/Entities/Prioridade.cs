namespace Chamados.Api.Models.Entities;

public class Prioridade
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public short Nivel { get; set; }

    public ICollection<Chamado> Chamados { get; set; } = new List<Chamado>();
}
