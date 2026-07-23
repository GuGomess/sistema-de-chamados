namespace Chamados.Api.Models.Entities;

public class Chamado
{
    public long Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public long SolicitanteId { get; set; }

    public Usuario Solicitante { get; set; } = null!;

    public long? TecnicoId { get; set; }

    public Usuario? Tecnico { get; set; }

    public long StatusId { get; set; }

    public Status Status { get; set; } = null!;

    public long CategoriaId { get; set; }

    public Categoria Categoria { get; set; } = null!;

    public long PrioridadeId { get; set; }

    public Prioridade Prioridade { get; set; } = null!;

    public DateTimeOffset CriadoEm { get; set; }

    public DateTimeOffset AtualizadoEm { get; set; }

    public DateTimeOffset? PrazoResposta { get; set; }

    public DateTimeOffset? PrazoResolucao { get; set; }

    public DateTimeOffset? ResolvidoEm { get; set; }

    public DateTimeOffset? FechadoEm { get; set; }

    public SituacaoSla SituacaoSlaResposta { get; set; } = SituacaoSla.EmDia;

    public SituacaoSla SituacaoSlaResolucao { get; set; } = SituacaoSla.EmDia;
}
