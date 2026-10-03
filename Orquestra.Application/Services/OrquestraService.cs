using System.Net.Mail;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService(IOrquestraDb db, ICurrentUser currentUser) : IOrquestraService
{
    private Guid OrgId => currentUser.OrganizationId != Guid.Empty
        ? currentUser.OrganizationId : throw new ForbiddenException("O contexto da organização é obrigatório.");
    private string UserId => !string.IsNullOrWhiteSpace(currentUser.UserId)
        ? currentUser.UserId : throw new ForbiddenException("É necessário estar autenticado.");
    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    private void Audit(string action, string entityType, Guid entityId, DateTimeOffset now, string? details = null)
        => db.Add(new AuditLog(OrgId, action, entityType, entityId, UserId, now, details));

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new ValidationException($"O campo {field} é obrigatório.");
        if (trimmed.Length > maxLength) throw new ValidationException($"O campo {field} deve ter no máximo {maxLength} caracteres.");
        return trimmed;
    }
    private static string? Optional(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > maxLength) throw new ValidationException($"O campo {field} deve ter no máximo {maxLength} caracteres.");
        return trimmed;
    }
    private static string Email(string? value)
    {
        var email = Required(value, "e-mail", 254);
        if (!MailAddress.TryCreate(email, out var parsed) || !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("O e-mail informado é inválido.");
        return email;
    }
    private static PageQuery CheckPage(PageQuery query)
    {
        if (query is null || query.Page < 1 || query.PageSize is < 1 or > 100)
            throw new ValidationException("A página deve ser maior que zero e a quantidade de itens deve ficar entre 1 e 100.");
        if ((long)(query.Page - 1) * query.PageSize > int.MaxValue)
            throw new ValidationException("A página solicitada é muito alta.");
        if (query.AssetStatus is not null && !Enum.IsDefined(query.AssetStatus.Value))
            throw new ValidationException("O filtro de situação do ativo é inválido.");
        if (query.EmployeeStatus is not null && !Enum.IsDefined(query.EmployeeStatus.Value))
            throw new ValidationException("O filtro de situação do colaborador é inválido.");
        return query with { Search = Optional(query.Search, "busca", 100) };
    }
    private static PagedResult<TOut> Page<TIn, TOut>(IQueryable<TIn> source, PageQuery query, Func<TIn, TOut> map)
    {
        var total = source.Count();
        var items = source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList().Select(map).ToArray();
        return new PagedResult<TOut>(items, total, query.Page, query.PageSize);
    }
    private static void CheckGuid(Guid id, string field)
    {
        if (id == Guid.Empty) throw new ValidationException($"O campo {field} é obrigatório.");
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
        ?? throw new NotFoundException("Departamento não encontrado.");
    private Employee Employee(Guid id) => db.Employees.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Colaborador não encontrado.");
    private AssetCategory Category(Guid id) => db.AssetCategories.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Categoria de ativo não encontrada.");
    private Asset Asset(Guid id) => db.Assets.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Ativo não encontrado.");
    private Custody Custody(Guid id) => db.Custodies.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Custódia não encontrada.");
    private OffboardingProcess Offboarding(Guid id) => db.Offboardings.SingleOrDefault(x => x.OrganizationId == OrgId && x.Id == id)
        ?? throw new NotFoundException("Processo de desligamento não encontrado.");
}
