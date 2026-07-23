using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Notificacoes;

public class NotificacaoDto
{
    public long Id { get; set; }

    public long IdChamado { get; set; }

    public TipoNotificacao Tipo { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public bool Lida { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public static NotificacaoDto FromEntity(Notificacao notificacao) => new()
    {
        Id = notificacao.Id,
        IdChamado = notificacao.ChamadoId,
        Tipo = notificacao.Tipo,
        Mensagem = notificacao.Mensagem,
        Lida = notificacao.Lida,
        CriadoEm = notificacao.CriadoEm
    };
}

public class NotificacaoContagemDto
{
    public int NaoLidas { get; set; }
}
