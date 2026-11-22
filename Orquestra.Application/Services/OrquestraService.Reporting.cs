using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService
{
    public PagedResult<MovimentoInventario> HistoricoInventario(PageQuery query)
    {
        query = CheckPage(query);
        if (query.De > query.Ate) throw new ValidationException("O início do período deve ser anterior ou igual ao fim.");
        var ativos = db.Assets.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null)
            ativos = ativos.Where(x => x.AssetTag.Contains(query.Search) || x.Name.Contains(query.Search));
        var movimentos = db.AssetMovements.Where(x => x.OrganizationId == OrgId);
        if (query.AssetStatus is not null) movimentos = movimentos.Where(x => x.NewStatus == query.AssetStatus);
        if (query.Operacao is not null) movimentos = movimentos.Where(x => x.Type == query.Operacao);
        if (!string.IsNullOrWhiteSpace(query.UsuarioId)) movimentos = movimentos.Where(x => x.PerformedByUserId == query.UsuarioId);
        if (query.De is { } de)
        {
            var inicio = new DateTimeOffset(de.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            movimentos = movimentos.Where(x => x.OccurredAt >= inicio);
        }
        if (query.Ate is { } ate)
        {
            var fim = new DateTimeOffset(ate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            movimentos = movimentos.Where(x => x.OccurredAt <= fim);
        }
        var registros = from movimento in movimentos
                        join ativo in ativos on movimento.AssetId equals ativo.Id
                        orderby movimento.OccurredAt descending, movimento.Id descending
                        select new MovimentoInventario(movimento.Id, ativo.Id, ativo.AssetTag, ativo.Name,
                            movimento.Type, movimento.PreviousStatus, movimento.NewStatus, movimento.OccurredAt,
                            movimento.PerformedByUserId, movimento.Notes);
        return Page(registros, query, x => x);
    }

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
            throw new ForbiddenException("O acesso à auditoria exige o perfil Auditor ou Administrador.");
        query = CheckPage(query);
        var source = db.AuditLogs.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null)
            source = source.Where(x => x.Action.Contains(query.Search) || x.EntityType.Contains(query.Search));
        return Task.FromResult(Page(source.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id), query, Map));
    }
}
