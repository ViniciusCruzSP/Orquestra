using Orquestra.Domain.Entities;

namespace Orquestra.Application.Abstractions;

public interface IOrquestraDb
{
    IQueryable<Organization> Organizations { get; }
    IQueryable<Department> Departments { get; }
    IQueryable<Employee> Employees { get; }
    IQueryable<AssetCategory> AssetCategories { get; }
    IQueryable<Asset> Assets { get; }
    IQueryable<Custody> Custodies { get; }
    IQueryable<CustodyItem> CustodyItems { get; }
    IQueryable<AssetMovement> AssetMovements { get; }
    IQueryable<OffboardingProcess> Offboardings { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    void Add<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid OrganizationId { get; }
    string UserId { get; }
    bool IsInRole(string role);
}
