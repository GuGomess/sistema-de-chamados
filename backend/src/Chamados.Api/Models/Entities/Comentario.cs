namespace Chamados.Api.Models.Entities;

public class Comentario
{
    public long Id { get; set; }

    public long ChamadoId { get; set; }

    public Chamado Chamado { get; set; } = null!;

    public long AutorId { get; set; }

    public Usuario Autor { get; set; } = null!;

    public string Mensagem { get; set; } = string.Empty;

    public bool Interno { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
}
