using Chamados.Api.Data;
using Chamados.Api.Models.Dtos;
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

    [AllowAnonymous]
    [HttpPost("registrar")]
    public async Task<ActionResult<AuthResponse>> Registrar(RegistrarClienteRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 6)
        {
            erros["senha"] = new[] { "Senha deve ter no mínimo 6 caracteres." };
        }

        var emailExiste = await _dbContext.Usuarios
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExiste)
        {
            erros["email"] = new[] { "Já existe um usuário cadastrado com este e-mail." };
        }

        if (erros.Count > 0)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = erros });
        }

        var perfilCliente = await _dbContext.Perfis.FindAsync(3L);
        if (perfilCliente is null)
        {
            return UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Falha de validação", Errors = new Dictionary<string, string[]> { ["perfil"] = new[] { "Perfil de cliente não encontrado." } } });
        }

        var usuario = new Usuario
        {
            Nome = request.Nome,
            Email = request.Email,
            PerfilId = perfilCliente.Id,
            Ativo = true,
            CriadoEm = DateTimeOffset.UtcNow
        };
        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.Senha);

        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        usuario.Perfil = perfilCliente;

        var (accessToken, expiresIn) = _tokenService.GerarAccessToken(usuario);
        var refreshToken = _tokenService.GerarRefreshToken();

        return StatusCode(StatusCodes.Status201Created, new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            Usuario = UsuarioDto.FromEntity(usuario)
        });
    }
}
