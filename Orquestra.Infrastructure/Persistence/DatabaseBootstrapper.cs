using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Orquestra.Domain.Entities;
using Orquestra.Infrastructure.Identity;

namespace Orquestra.Infrastructure.Persistence;

public sealed class DatabaseBootstrapper(
    OrquestraDbContext db,
    RoleManager<IdentityRole> roles,
    UserManager<ApplicationUser> users,
    IConfiguration configuration)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        foreach (var name in new[] { "Admin", "GestorTI", "OperadorTI", "Auditor" })
        {
            if (await roles.RoleExistsAsync(name)) continue;
            EnsureSuccess(await roles.CreateAsync(new IdentityRole(name)), "create an Identity role");
        }

        var organizationName = configuration["Bootstrap:OrganizationName"];
        var organizationSlug = configuration["Bootstrap:OrganizationSlug"];
        var adminEmail = configuration["Bootstrap:AdminEmail"];
        var adminPassword = configuration["Bootstrap:AdminPassword"];
        var provided = new[] { organizationName, organizationSlug, adminEmail, adminPassword };

        if (provided.All(string.IsNullOrWhiteSpace))
        {
            var adminRole = await roles.FindByNameAsync("Admin");
            var hasOrganization = await db.Set<Organization>().IgnoreQueryFilters().AnyAsync(cancellationToken);
            var hasAdmin = adminRole is not null && await db.UserRoles
                .AnyAsync(x => x.RoleId == adminRole.Id, cancellationToken);
            if (!hasOrganization || !hasAdmin)
                throw new InvalidOperationException(
                    "An organization and Admin user are required. Configure all Bootstrap settings to complete setup.");
            return;
        }
        if (provided.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("All Bootstrap organization and admin settings must be provided together.");

        var organization = await db.Set<Organization>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Slug == organizationSlug, cancellationToken);
        if (organization is null)
        {
            organization = new Organization(organizationName!, organizationSlug!, DateTimeOffset.UtcNow);
            db.Set<Organization>().Add(organization);
            await db.SaveChangesAsync(cancellationToken);
        }

        var admin = await users.FindByEmailAsync(adminEmail!);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail!.Trim(),
                Email = adminEmail.Trim(),
                OrganizationId = organization.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            EnsureSuccess(await users.CreateAsync(admin, adminPassword!), "create the initial admin");
        }
        else if (admin.OrganizationId != organization.Id)
        {
            throw new InvalidOperationException("The bootstrap admin email belongs to another organization.");
        }

        if (!await users.IsInRoleAsync(admin, "Admin"))
            EnsureSuccess(await users.AddToRoleAsync(admin, "Admin"), "assign the initial admin role");
    }

    private static void EnsureSuccess(IdentityResult result, string action)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Could not {action}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
    }
}
