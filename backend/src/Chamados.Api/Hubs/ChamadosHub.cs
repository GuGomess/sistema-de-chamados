using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chamados.Api.Hubs;

// Hub central de tempo real: cada conexão entra automaticamente no grupo
// pessoal do usuário logado ("user-{id}", usado para o sino de notificações)
// e no grupo de broadcast geral ("todos-chamados", usado por lista/dashboard).
// O grupo por chamado ("chamado-{id}", usado pela tela de detalhe) é opcional
// e controlado explicitamente pelo cliente via EntrarChamado/SairChamado.
[Authorize]
public class ChamadosHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{usuarioId.Value}");
            await Groups.AddToGroupAsync(Context.ConnectionId, "todos-chamados");
        }

        await base.OnConnectedAsync();
    }

    public async Task EntrarChamado(long chamadoId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chamado-{chamadoId}");
    }

    public async Task SairChamado(long chamadoId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chamado-{chamadoId}");
    }

    // Mesmo padrão de ChamadosController.ObterUsuarioId(): claim Sub (JWT) ou,
    // como fallback, ClaimTypes.NameIdentifier.
    private long? ObterUsuarioId()
    {
        var claim = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub) ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}
