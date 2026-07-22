namespace Chamados.Api.Models.Entities;

public class Perfil
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
