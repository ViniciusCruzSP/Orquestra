using System.Net.Mail;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService(IOrquestraDb db, ICurrentUser currentUser) : IOrquestraService
{
    private Guid OrgId => currentUser.OrganizationId != Guid.Empty
        ? currentUser.OrganizationId : throw new ForbiddenException("Organization context is required.");
    private string UserId => !string.IsNullOrWhiteSpace(currentUser.UserId)
        ? currentUser.UserId : throw new ForbiddenException("Authenticated user is required.");
    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    private void Audit(string action, string entityType, Guid entityId, DateTimeOffset now, string? details = null)
        => db.Add(new AuditLog(OrgId, action, entityType, entityId, UserId, now, details));

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new ValidationException($"{field} is required.");
        if (trimmed.Length > maxLength) throw new ValidationException($"{field} must have at most {maxLength} characters.");
        return trimmed;
    }
    private static string? Optional(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > maxLength) throw new ValidationException($"{field} must have at most {maxLength} characters.");
        return trimmed;
    }
    private static string Email(string? value)
    {
        var email = Required(value, "Email", 254);
        if (!MailAddress.TryCreate(email, out var parsed) || !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Email is invalid.");
        return email;
    }
    private static PageQuery CheckPage(PageQuery query)
    {
        if (query is null || query.Page < 1 || query.PageSize is < 1 or > 100)
            throw new ValidationException("Page must be positive and PageSize must be between 1 and 100.");
        if ((long)(query.Page - 1) * query.PageSize > int.MaxValue)
            throw new ValidationException("Requested page is too large.");
        if (query.AssetStatus is not null && !Enum.IsDefined(query.AssetStatus.Value))
            throw new ValidationException("AssetStatus filter is invalid.");
        if (query.EmployeeStatus is not null && !Enum.IsDefined(query.EmployeeStatus.Value))
            throw new ValidationException("EmployeeStatus filter is invalid.");
        return query with { Search = Optional(query.Search, "Search", 100) };
    }
    private static PagedResult<TOut> Page<TIn, TOut>(IQueryable<TIn> source, PageQuery query, Func<TIn, TOut> map)
    {
        var total = source.Count();
        var items = source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList().Select(map).ToArray();
        return new PagedResult<TOut>(items, total, query.Page, query.PageSize);
    }
    private static void CheckGuid(Guid id, string field)
    {
        if (id == Guid.Empty) throw new ValidationException($"{field} is required.");
    }
    private static DepartmentDto Map(Department x) => new(x.Id, x.Name, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static EmployeeDto Map(Employee x) => new(x.Id, x.DepartmentId, x.FullName, x.Email, x.Status, x.CreatedAt, x.UpdatedAt);
    private static CategoryDto Map(AssetCategory x) => new(x.Id, x.Name, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static AssetDto Map(Asset x) => new(x.Id, x.CategoryId, x.AssetTag, x.Name, x.Brand, x.Model,
        x.SerialNumber, x.Notes, x.Status, x.IsActive, Convert.ToBase64String(x.RowVersion), x.CreatedAt, x.UpdatedAt);
    private static MovementDto Map(AssetMovement x) => new(x.Id, x.AssetId, x.CustodyId, x.Type,
        x.PreviousStatus, x.NewStatus, x.OccurredAt, x.PerformedByUserId, x.Notes);
    private static AuditLogDto Map(AuditLog x) => new(x.Id, x.Action, x.EntityType, x.EntityId, x.UserId, x.OccurredAt, x.Details);

    private Department Department(Guid id) => db.Departments.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Department not found.");
    private Employee Employee(Guid id) => db.Employees.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Employee not found.");
    private AssetCategory Category(Guid id) => db.AssetCategories.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Asset category not found.");
    private Asset Asset(Guid id) => db.Assets.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Asset not found.");
    private Custody Custody(Guid id) => db.Custodies.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Custody not found.");
    private OffboardingProcess Offboarding(Guid id) => db.Offboardings.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Offboarding not found.");
}
