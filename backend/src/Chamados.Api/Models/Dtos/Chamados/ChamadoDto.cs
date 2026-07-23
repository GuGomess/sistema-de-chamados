using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Chamados;

public class ChamadoDto
{
    public long Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public UsuarioDto Solicitante { get; set; } = null!;

    public UsuarioDto? Tecnico { get; set; }

    public StatusDto Status { get; set; } = null!;

    public CategoriaDto Categoria { get; set; } = null!;

    public PrioridadeDto Prioridade { get; set; } = null!;

    public DateTimeOffset CriadoEm { get; set; }

    public DateTimeOffset AtualizadoEm { get; set; }

    public DateTimeOffset? PrazoResposta { get; set; }

    public DateTimeOffset? PrazoResolucao { get; set; }

    public DateTimeOffset? ResolvidoEm { get; set; }

    public DateTimeOffset? FechadoEm { get; set; }

    public SituacaoSla SituacaoSlaResposta { get; set; }

    public SituacaoSla SituacaoSlaResolucao { get; set; }

    public static ChamadoDto FromEntity(Chamado chamado) => new()
    {
        Id = chamado.Id,
        Titulo = chamado.Titulo,
        Descricao = chamado.Descricao,
        Solicitante = UsuarioDto.FromEntity(chamado.Solicitante),
        Tecnico = chamado.Tecnico is null ? null : UsuarioDto.FromEntity(chamado.Tecnico),
        Status = StatusDto.FromEntity(chamado.Status),
        Categoria = CategoriaDto.FromEntity(chamado.Categoria),
        Prioridade = PrioridadeDto.FromEntity(chamado.Prioridade),
        CriadoEm = chamado.CriadoEm,
        AtualizadoEm = chamado.AtualizadoEm,
        PrazoResposta = chamado.PrazoResposta,
        PrazoResolucao = chamado.PrazoResolucao,
        ResolvidoEm = chamado.ResolvidoEm,
        FechadoEm = chamado.FechadoEm,
        SituacaoSlaResposta = chamado.SituacaoSlaResposta,
        SituacaoSlaResolucao = chamado.SituacaoSlaResolucao
    };
}

public class StatusDto
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public short Ordem { get; set; }

    public bool Final { get; set; }

    public static StatusDto FromEntity(Status status) => new()
    {
        Id = status.Id,
        Nome = status.Nome,
        Ordem = status.Ordem,
        Final = status.Final
    };
}

public class CategoriaDto
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public bool Ativa { get; set; }

    public static CategoriaDto FromEntity(Categoria categoria) => new()
    {
        Id = categoria.Id,
        Nome = categoria.Nome,
        Descricao = categoria.Descricao,
        Ativa = categoria.Ativa
    };
}

public class PrioridadeDto
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public short Nivel { get; set; }

    public static PrioridadeDto FromEntity(Prioridade prioridade) => new()
    {
        Id = prioridade.Id,
        Nome = prioridade.Nome,
        Nivel = prioridade.Nivel
    };
}
