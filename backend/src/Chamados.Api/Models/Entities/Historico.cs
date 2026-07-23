namespace Chamados.Api.Models.Entities;

public class Historico
{
    public long Id { get; set; }

    public long ChamadoId { get; set; }

    public Chamado Chamado { get; set; } = null!;

    public long AutorId { get; set; }

    public Usuario Autor { get; set; } = null!;

    public long? StatusAnteriorId { get; set; }

    public Status? StatusAnterior { get; set; }

    public long? StatusNovoId { get; set; }

    public Status? StatusNovo { get; set; }

    public string Acao { get; set; } = string.Empty;

    public string? Detalhe { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
