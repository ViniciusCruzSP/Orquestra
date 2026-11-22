using System.Security.Claims;
using Orquestra.Application.Abstractions;

namespace Orquestra.Api;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

    public Guid OrganizationId => Guid.TryParse(Principal.FindFirstValue("org_id"), out var id) && id != Guid.Empty
        ? id
        : throw new UnauthorizedAccessException("Organização não identificada.");

    public string UserId => Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("Usuário não identificado.");

    public bool IsInRole(string role) => Principal.IsInRole(role);
}
