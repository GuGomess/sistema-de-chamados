namespace Chamados.Api.Models.Entities;

public class Avaliacao
{
    public long Id { get; set; }

    public long ChamadoId { get; set; }

    public Chamado Chamado { get; set; } = null!;

    public long AutorId { get; set; }

    public Usuario Autor { get; set; } = null!;

    public short Nota { get; set; }

    public string? Comentario { get; set; }

    public bool Publica { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
