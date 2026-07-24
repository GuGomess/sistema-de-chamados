namespace Chamados.Api.Models.Entities;

public class Anexo
{
    public long Id { get; set; }

    public long ChamadoId { get; set; }

    public Chamado Chamado { get; set; } = null!;

    public long AutorId { get; set; }

    public Usuario Autor { get; set; } = null!;

    public string NomeArquivo { get; set; } = string.Empty;

    public string Caminho { get; set; } = string.Empty;

    public string TipoMime { get; set; } = string.Empty;

    public long TamanhoBytes { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
