using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Orquestra.Infrastructure.Identity;

namespace Orquestra.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("recuperacao")]
[Route("api/autenticacao")]
public sealed class RecuperacaoController(UserManager<ApplicationUser> usuarios, IConfiguration config,
    ILogger<RecuperacaoController> logger) : ControllerBase
{
    [HttpPost("recuperar-senha")]
    public async Task<IActionResult> Solicitar(PedidoRecuperacao dados, CancellationToken ct)
    {
        var servidor = config["Email:Servidor"];
        var remetente = config["Email:Remetente"];
        if (string.IsNullOrWhiteSpace(servidor) || string.IsNullOrWhiteSpace(remetente)
            || !Uri.TryCreate(config["Email:UrlFrontend"], UriKind.Absolute, out var destino)
            || (destino.Scheme != "https" && !(destino.Scheme == "http" && destino.IsLoopback)))
            return Problem(statusCode: 503, title: "O envio de e-mail não está configurado.");

        var usuario = await usuarios.FindByEmailAsync(dados.Email.Trim());
        if (usuario?.Email is not null)
        {
            var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
            var link = new UriBuilder(new Uri(destino, "/redefinir-senha"))
            {
                Fragment = $"email={Uri.EscapeDataString(usuario.Email)}&codigo={Uri.EscapeDataString(token)}"
            }.Uri.AbsoluteUri;

            using var cliente = new SmtpClient(servidor, config.GetValue("Email:Porta", 587)) { EnableSsl = true };
            if (config["Email:Usuario"] is { Length: > 0 } login)
                cliente.Credentials = new NetworkCredential(login, config["Email:Senha"]);
            using var mensagem = new MailMessage(remetente, usuario.Email, "Redefinição de senha do Orquestra",
                $"Abra este endereço para redefinir sua senha. O link é válido por 30 minutos.\n\n{link}\n\nSe não fez o pedido, ignore este e-mail.");
            try { await cliente.SendMailAsync(mensagem, ct); }
            catch (SmtpException ex) { logger.LogWarning("Falha SMTP na recuperação de senha. Código {Status}", ex.StatusCode); }
        }
        return Accepted(new { mensagem = "Se houver uma conta para esse e-mail, você receberá as instruções." });
    }

    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> Redefinir(PedidoRedefinicao dados)
    {
        var usuario = await usuarios.FindByEmailAsync(dados.Email.Trim());
        if (usuario is null) return Problem(statusCode: 400, title: "O link é inválido ou expirou.");
        var resultado = await usuarios.ResetPasswordAsync(usuario, dados.Codigo, dados.Senha);
        if (!resultado.Succeeded)
            return Problem(statusCode: 400, title: string.Join(" ", resultado.Errors.Select(x => x.Description)));
        return NoContent();
    }
}

public record PedidoRecuperacao([Required, EmailAddress, StringLength(254)] string Email);
public record PedidoRedefinicao([Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(2048)] string Codigo, [Required, StringLength(256, MinimumLength = 12)] string Senha);
