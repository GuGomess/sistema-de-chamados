namespace Chamados.Api.Models.Entities;

public class Usuario
{
    public long Id { get; set; }

    public long PerfilId { get; set; }

    public Perfil Perfil { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTimeOffset CriadoEm { get; set; }

    public ICollection<Chamado> ChamadosSolicitados { get; set; } = new List<Chamado>();

    public ICollection<Chamado> ChamadosAtendidos { get; set; } = new List<Chamado>();
}
