using Chamados.Api.Data;
using Chamados.Api.Models.Dtos.Auth;
using Chamados.Api.Models.Entities;
using Chamados.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ChamadosDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthController(ChamadosDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var usuario = await _dbContext.Usuarios
            .Include(u => u.Perfil)
            .SingleOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (usuario is null || !usuario.Ativo)
        {
            return Unauthorized();
        }

        var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.Senha);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        var (accessToken, expiresIn) = _tokenService.GerarAccessToken(usuario);
        var refreshToken = _tokenService.GerarRefreshToken();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            Usuario = UsuarioDto.FromEntity(usuario)
        });
    }
}
