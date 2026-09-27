using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService
{
    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var assets = db.Assets.Where(x => x.OrganizationId == OrgId && x.IsActive);
        return Task.FromResult(new DashboardDto(
            assets.Count(),
            assets.Count(x => x.Status == AssetStatus.Available),
            assets.Count(x => x.Status == AssetStatus.Assigned),
            assets.Count(x => x.Status == AssetStatus.Maintenance),
            assets.Count(x => x.Status == AssetStatus.Defective),
            assets.Count(x => x.Status == AssetStatus.Discarded),
            db.Employees.Count(x => x.OrganizationId == OrgId && x.Status == EmployeeStatus.Active),
            db.Offboardings.Count(x => x.OrganizationId == OrgId && x.CompletedAt == null),
            db.CustodyItems.Count(x => x.OrganizationId == OrgId && x.ReturnedAt == null)));
    }
    public Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(PageQuery query, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole("Auditor") && !currentUser.IsInRole("Admin"))
            throw new ForbiddenException("Audit log access requires Auditor or Admin role.");
        query = CheckPage(query);
        var source = db.AuditLogs.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null)
            source = source.Where(x => x.Action.Contains(query.Search) || x.EntityType.Contains(query.Search));
        return Task.FromResult(Page(source.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id), query, Map));
    }
}
