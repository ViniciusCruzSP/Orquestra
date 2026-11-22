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

        foreach (var name in new[] { "Admin", "GestorTI", "OperadorTI", "Auditor", "Colaborador" })
        {
            if (await roles.RoleExistsAsync(name)) continue;
            EnsureSuccess(await roles.CreateAsync(new IdentityRole(name)), "criar um perfil de acesso");
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
                    "É necessário ter uma organização e um usuário administrador. Configure todos os valores de Bootstrap para concluir a preparação.");
            return;
        }
        if (provided.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Todas as configurações de Bootstrap da organização e do administrador devem ser informadas juntas.");

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
            EnsureSuccess(await users.CreateAsync(admin, adminPassword!), "criar o administrador inicial");
        }
        else if (admin.OrganizationId != organization.Id)
        {
            throw new InvalidOperationException("O e-mail do administrador inicial pertence a outra organização.");
        }

        if (!await users.IsInRoleAsync(admin, "Admin"))
            EnsureSuccess(await users.AddToRoleAsync(admin, "Admin"), "atribuir o perfil de administrador inicial");
    }

    private static void EnsureSuccess(IdentityResult result, string action)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Não foi possível {action}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
    }
}
