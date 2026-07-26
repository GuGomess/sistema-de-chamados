namespace Chamados.Api.Models.Entities;

public class Departamento
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTimeOffset CriadoEm { get; set; }

    public ICollection<Usuario> Tecnicos { get; set; } = new List<Usuario>();
}
