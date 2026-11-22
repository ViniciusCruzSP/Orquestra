using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;
using Orquestra.Infrastructure.Persistence;

namespace Orquestra.Infrastructure.Identity;

internal static class ContaValida
{
    internal static Task<bool> PodeEntrar(OrquestraDbContext db, ApplicationUser usuario, CancellationToken ct)
        => usuario.ColaboradorId is null ? Task.FromResult(true)
            : db.Set<Employee>().IgnoreQueryFilters().AnyAsync(x => x.OrganizationId == usuario.OrganizationId
                && x.Id == usuario.ColaboradorId && x.Status != EmployeeStatus.Inactive, ct);

    internal static async Task ValidarToken(TokenValidatedContext contexto)
    {
        var principal = contexto.Principal!;
        var servicos = contexto.HttpContext.RequestServices;
        var usuarios = servicos.GetRequiredService<UserManager<ApplicationUser>>();
        var usuario = await usuarios.FindByIdAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
        var db = servicos.GetRequiredService<OrquestraDbContext>();
        if (usuario is null || usuario.OrganizationId.ToString() != principal.FindFirstValue("org_id")
            || !string.Equals(usuario.SecurityStamp, principal.FindFirstValue("versao_sessao"), StringComparison.Ordinal)
            || !await PodeEntrar(db, usuario, contexto.HttpContext.RequestAborted))
            contexto.Fail("A sessão não está mais ativa.");
    }
}
