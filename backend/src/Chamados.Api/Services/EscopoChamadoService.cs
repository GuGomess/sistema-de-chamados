using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chamados.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Services;

public class EscopoChamadoService : IEscopoChamadoService
{
    private readonly ChamadosDbContext _dbContext;

    public EscopoChamadoService(ChamadosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public long? ObterUsuarioId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(JwtRegisteredClaimNames.Sub) ?? user.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && long.TryParse(claim.Value, out var id) ? id : null;
    }

    public async Task<bool> EstaNoDepartamentoAsync(long usuarioId, long idDepartamento) =>
        await _dbContext.Usuarios
            .Where(u => u.Id == usuarioId)
            .SelectMany(u => u.Departamentos)
            .AnyAsync(d => d.Id == idDepartamento);

    public async Task<List<long>> IdsDepartamentosDoUsuarioAsync(long usuarioId) =>
        await _dbContext.Usuarios
            .Where(u => u.Id == usuarioId)
            .SelectMany(u => u.Departamentos)
            .Select(d => d.Id)
            .ToListAsync();
}
