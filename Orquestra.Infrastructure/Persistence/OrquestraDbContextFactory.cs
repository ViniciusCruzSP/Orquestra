using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Orquestra.Application.Abstractions;

namespace Orquestra.Infrastructure.Persistence;

public sealed class OrquestraDbContextFactory : IDesignTimeDbContextFactory<OrquestraDbContext>
{
    public OrquestraDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Orquestra");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Set ConnectionStrings__Orquestra before running EF Core design-time commands.");
        var options = new DbContextOptionsBuilder<OrquestraDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new OrquestraDbContext(options, new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid OrganizationId => Guid.Empty;
        public string UserId => string.Empty;
        public bool IsInRole(string role) => false;
    }
}
