namespace Chamados.Api.Models.Entities;

public class Notificacao
{
    public long Id { get; set; }

    public long DestinatarioId { get; set; }

    public Usuario Destinatario { get; set; } = null!;

    public long ChamadoId { get; set; }

    public Chamado Chamado { get; set; } = null!;

    public TipoNotificacao Tipo { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public bool Lida { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
