using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Chamados;

public class AvaliacaoDto
{
    public long Id { get; set; }

    public long IdChamado { get; set; }

    public UsuarioDto Autor { get; set; } = null!;

    public short Nota { get; set; }

    public string? Comentario { get; set; }

    public bool Publica { get; set; }

    public bool Oculta { get; set; }

    public bool Editado { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public static AvaliacaoDto FromEntity(Avaliacao avaliacao) => new()
    {
        Id = avaliacao.Id,
        IdChamado = avaliacao.ChamadoId,
        Autor = UsuarioDto.FromEntity(avaliacao.Autor),
        Nota = avaliacao.Nota,
        Comentario = avaliacao.Comentario,
        Publica = avaliacao.Publica,
        Oculta = avaliacao.Oculta,
        Editado = avaliacao.EditadoEm.HasValue,
        CriadoEm = avaliacao.CriadoEm
    };
}

public class AvaliacaoCreateRequest
{
    public short Nota { get; set; }

    public string? Comentario { get; set; }

    public bool Publica { get; set; }
}

public class AvaliacaoUpdateRequest
{
    public short Nota { get; set; }

    public string? Comentario { get; set; }

    public bool Publica { get; set; }
}

public class AvaliacaoOcultarRequest
{
    public bool Oculta { get; set; }
}
