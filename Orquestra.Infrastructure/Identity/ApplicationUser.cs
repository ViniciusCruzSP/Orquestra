using Microsoft.AspNetCore.Identity;

namespace Orquestra.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public Guid OrganizationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ColaboradorId { get; set; }
}
