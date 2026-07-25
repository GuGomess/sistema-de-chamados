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

    public bool Editado { get; set; }

    public bool Oculta { get; set; }

    public List<AnexoDto> Anexos { get; set; } = [];

    public static ComentarioDto FromEntity(Comentario comentario, long viewerUsuarioId, bool viewerEhAdmin)
    {
        var podeVerConteudoReal = viewerEhAdmin || comentario.AutorId == viewerUsuarioId;

        return new()
        {
            Id = comentario.Id,
            IdChamado = comentario.ChamadoId,
            Autor = UsuarioDto.FromEntity(comentario.Autor),
            Mensagem = comentario.Oculta && !podeVerConteudoReal ? "(Ocultado pelo Administrador)" : comentario.Mensagem,
            Interno = comentario.Interno,
            CriadoEm = comentario.CriadoEm,
            Editado = comentario.EditadoEm.HasValue,
            Oculta = comentario.Oculta,
            Anexos = comentario.Anexos
                .OrderBy(a => a.CriadoEm)
                .Select(AnexoDto.FromEntity)
                .ToList()
        };
    }
}

public class ComentarioUpdateRequest
{
    public string Mensagem { get; set; } = string.Empty;
}

public class ComentarioOcultarRequest
{
    public bool Oculta { get; set; }
}
