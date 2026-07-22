namespace Chamados.Api.Models.Dtos.Chamados;

public class ChamadoUpdateRequest
{
    public long? IdStatus { get; set; }

    public long? IdCategoria { get; set; }

    public long? IdPrioridade { get; set; }

    /// <summary>
    /// Presente apenas quando o campo "idTecnico" veio no corpo da requisição
    /// (distingue "não enviado" de "enviado como null", que significa desatribuir).
    /// </summary>
    public bool IdTecnicoInformado { get; set; }

    public long? IdTecnico { get; set; }
}
