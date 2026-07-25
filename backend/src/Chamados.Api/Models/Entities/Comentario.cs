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

    public DateTimeOffset? EditadoEm { get; set; }

    // Administrador pode ocultar um comentário de qualquer autor (ex.: conteúdo
    // impróprio) sem apagar o registro — mesmo mecanismo de Avaliacao.Oculta:
    // autor original e administrador sempre veem o texto real, os demais veem
    // um placeholder (ver ComentarioDto.FromEntity).
    public bool Oculta { get; set; }

    public ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
}
