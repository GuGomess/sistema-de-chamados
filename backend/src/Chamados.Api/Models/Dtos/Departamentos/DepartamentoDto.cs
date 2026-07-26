using Chamados.Api.Models.Entities;

namespace Chamados.Api.Models.Dtos.Departamentos;

public class DepartamentoDto
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public bool Ativo { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public static DepartamentoDto FromEntity(Departamento departamento) => new()
    {
        Id = departamento.Id,
        Nome = departamento.Nome,
        Descricao = departamento.Descricao,
        Ativo = departamento.Ativo,
        CriadoEm = departamento.CriadoEm
    };
}

public class DepartamentoCreateRequest
{
    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }
}

public class DepartamentoUpdateRequest
{
    public string? Nome { get; set; }

    public string? Descricao { get; set; }
}

public class DepartamentoStatusRequest
{
    public bool Ativo { get; set; }
}
