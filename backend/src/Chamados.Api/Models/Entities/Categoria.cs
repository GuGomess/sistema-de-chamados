namespace Chamados.Api.Models.Entities;

public class Categoria
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public bool Ativa { get; set; } = true;

    public ICollection<Chamado> Chamados { get; set; } = new List<Chamado>();
}
