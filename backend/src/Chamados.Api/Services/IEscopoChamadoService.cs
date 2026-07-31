using System.Security.Claims;

namespace Chamados.Api.Services;

/// <summary>
/// Escopo de acesso por papel/departamento aplicado a chamados — compartilhado
/// entre controllers que precisam decidir "quais chamados este usuário pode
/// ver/agir" (ex.: ChamadosController, MetricasController). Extraído de
/// ChamadosController (ver card Trello "Endpoints de métricas").
/// </summary>
public interface IEscopoChamadoService
{
    /// <summary>
    /// Lê o id do usuário autenticado a partir do claim "sub"/NameIdentifier do
    /// token. Recebe o ClaimsPrincipal explicitamente porque este serviço não é
    /// um Controller (não tem acesso implícito a "User").
    /// </summary>
    long? ObterUsuarioId(ClaimsPrincipal user);

    Task<bool> EstaNoDepartamentoAsync(long usuarioId, long idDepartamento);

    Task<List<long>> IdsDepartamentosDoUsuarioAsync(long usuarioId);
}
