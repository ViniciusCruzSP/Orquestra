using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Auth;

namespace Orquestra.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public sealed class AutenticacaoController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("entrar")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request.Email, request.Password, ct);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Credenciais inválidas")
            : Ok(result);
    }

    [HttpGet("usuario-atual")]
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
    [property: System.Text.Json.Serialization.JsonPropertyName("email")]
    [Required(ErrorMessage = "O e-mail é obrigatório."), EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(254)] string Email,
    [property: System.Text.Json.Serialization.JsonPropertyName("senha")]
    [Required(ErrorMessage = "A senha é obrigatória."), StringLength(256)] string Password);
