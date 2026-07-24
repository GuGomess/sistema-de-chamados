using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Chamados;

public class ComentarioDto
{
    public long Id { get; set; }

    public long IdChamado { get; set; }

    public UsuarioDto Autor { get; set; } = null!;

    public string Mensagem { get; set; } = string.Empty;

    public bool Interno { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public List<AnexoDto> Anexos { get; set; } = [];

    public static ComentarioDto FromEntity(Comentario comentario) => new()
    {
        Id = comentario.Id,
        IdChamado = comentario.ChamadoId,
        Autor = UsuarioDto.FromEntity(comentario.Autor),
        Mensagem = comentario.Mensagem,
        Interno = comentario.Interno,
        CriadoEm = comentario.CriadoEm,
        Anexos = comentario.Anexos
            .OrderBy(a => a.CriadoEm)
            .Select(AnexoDto.FromEntity)
            .ToList()
    };
}
