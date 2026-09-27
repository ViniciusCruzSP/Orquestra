using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Auth;

namespace Orquestra.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request.Email, request.Password, ct);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Credenciais inválidas")
            : Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await auth.GetMeAsync(currentUser.UserId, ct);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Usuário não encontrado")
            : Ok(result);
    }
}

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(256)] string Password);
