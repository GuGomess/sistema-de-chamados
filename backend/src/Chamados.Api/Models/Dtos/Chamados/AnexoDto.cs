using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Chamados;

public class AnexoDto
{
    public long Id { get; set; }

    public long IdChamado { get; set; }

    public long? IdComentario { get; set; }

    public UsuarioDto Autor { get; set; } = null!;

    public string NomeArquivo { get; set; } = string.Empty;

    public string TipoMime { get; set; } = string.Empty;

    public long TamanhoBytes { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTimeOffset CriadoEm { get; set; }

    public static AnexoDto FromEntity(Anexo anexo) => new()
    {
        Id = anexo.Id,
        IdChamado = anexo.ChamadoId,
        IdComentario = anexo.ComentarioId,
        Autor = UsuarioDto.FromEntity(anexo.Autor),
        NomeArquivo = anexo.NomeArquivo,
        TipoMime = anexo.TipoMime,
        TamanhoBytes = anexo.TamanhoBytes,
        Url = $"/api/v1/chamados/{anexo.ChamadoId}/anexos/{anexo.Id}/download",
        CriadoEm = anexo.CriadoEm
    };
}
