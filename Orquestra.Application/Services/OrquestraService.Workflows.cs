using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService
{
    public Task<PagedResult<CustodyDto>> GetCustodiesAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.Custodies.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null)
        {
            var employeeIds = db.Employees.Where(e => e.OrganizationId == OrgId &&
                (e.FullName.Contains(query.Search) || e.Email.Contains(query.Search))).Select(e => e.Id);
            source = source.Where(x => employeeIds.Contains(x.EmployeeId));
        }
        return Task.FromResult(Page(source.OrderByDescending(x => x.DeliveredAt).ThenByDescending(x => x.Id), query, MapCustody));
    }
    public Task<CustodyDto> GetCustodyAsync(Guid id, CancellationToken ct = default) => Task.FromResult(MapCustody(Custody(id)));
    public async Task<CustodyDto> CreateCustodyAsync(CustodyCreate request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        CheckGuid(request.EmployeeId, "EmployeeId");
        if (request.AssetIds is null || request.AssetIds.Count is < 1 or > 100 || request.AssetIds.Any(x => x == Guid.Empty))
            throw new ValidationException("Provide between 1 and 100 asset IDs.");
        var ids = request.AssetIds.Distinct().ToArray();
        if (ids.Length != request.AssetIds.Count) throw new ValidationException("Asset IDs must be unique.");
        var notes = Optional(request.Notes, "Notes", 2000);
        var employee = Employee(request.EmployeeId);
        if (employee.Status != EmployeeStatus.Active) throw new ConflictException("Assets can only be assigned to active employees.");
        var assets = db.Assets.Where(x => x.OrganizationId == OrgId && ids.Contains(x.Id)).ToDictionary(x => x.Id);
        if (assets.Count != ids.Length) throw new ValidationException("One or more assets do not exist in this organization.");
        if (assets.Values.Any(x => !x.IsActive || x.Status != AssetStatus.Available))
            throw new ConflictException("All assets must be active and available.");
        if (db.CustodyItems.Any(x => x.OrganizationId == OrgId && ids.Contains(x.AssetId) && x.ReturnedAt == null))
            throw new ConflictException("One or more assets already have an active custody.");
        var now = Now; employee.Touch(now);
        var custody = new Custody(OrgId, employee.Id, UserId, notes, now);
        db.Add(custody);
        foreach (var asset in assets.Values)
        {
            var before = asset.Status; asset.Assign(now);
            db.Add(new CustodyItem(OrgId, custody.Id, asset.Id, now));
            db.Add(new AssetMovement(OrgId, asset.Id, custody.Id, MovementType.Assigned, before,
                asset.Status, UserId, notes, now));
            Audit("Assigned", "Asset", asset.Id, now);
        }
        Audit("Created", "Custody", custody.Id, now);
        await db.SaveChangesAsync(ct); return MapCustody(custody);
    }
    public async Task<CustodyDto> ReturnCustodyAsync(Guid id, CustodyReturn request, CancellationToken ct = default)
    {
        if (request is null || request.Items is null || request.Items.Count is < 1 or > 100)
            throw new ValidationException("Provide between 1 and 100 return items.");
        if (request.Items.Any(x => x is null)) throw new ValidationException("Return items cannot be null.");
        var custody = Custody(id);
        var ids = request.Items.Select(x => x.AssetId).ToArray();
        if (ids.Any(x => x == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ValidationException("Return asset IDs must be nonempty and unique.");
        if (request.Items.Any(x => !Enum.IsDefined(x.Disposition)))
            throw new ValidationException("Return disposition is invalid.");
        var items = db.CustodyItems.Where(x => x.OrganizationId == OrgId && x.CustodyId == id &&
            ids.Contains(x.AssetId) && x.ReturnedAt == null).ToDictionary(x => x.AssetId);
        if (items.Count != ids.Length) throw new ConflictException("Every return item must be active in this custody.");
        var assets = db.Assets.Where(x => x.OrganizationId == OrgId && ids.Contains(x.Id)).ToDictionary(x => x.Id);
        if (assets.Count != ids.Length) throw new ConflictException("A custody asset is missing.");
        var now = Now;
        foreach (var requestItem in request.Items)
        {
            var notes = Optional(requestItem.Notes, "Notes", 2000);
            var asset = assets[requestItem.AssetId]; var before = asset.Status;
            try { asset.Return(requestItem.Disposition, now); }
            catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
            items[asset.Id].Return(UserId, notes, now);
            db.Add(new AssetMovement(OrgId, asset.Id, id, MovementType.Returned, before,
                asset.Status, UserId, notes, now));
            Audit("Returned", "Asset", asset.Id, now);
        }
        Audit("ReturnedItems", "Custody", id, now);
        await db.SaveChangesAsync(ct); return MapCustody(custody);
    }
    private CustodyDto MapCustody(Custody x)
    {
        var items = db.CustodyItems.Where(i => i.OrganizationId == OrgId && i.CustodyId == x.Id)
            .OrderBy(i => i.AssignedAt).ThenBy(i => i.Id)
            .ToList().Select(i => new CustodyItemDto(i.Id, i.AssetId, i.AssignedAt, i.ReturnedAt,
                i.ReturnedByUserId, i.ReturnNotes)).ToArray();
        return new CustodyDto(x.Id, x.EmployeeId, x.DeliveredAt, x.DeliveredByUserId, x.Notes, items);
    }

    public Task<PagedResult<OffboardingDto>> GetOffboardingsAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.Offboardings.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null)
        {
            var employeeIds = db.Employees.Where(e => e.OrganizationId == OrgId &&
                (e.FullName.Contains(query.Search) || e.Email.Contains(query.Search))).Select(e => e.Id);
            source = source.Where(x => employeeIds.Contains(x.EmployeeId));
        }
        return Task.FromResult(Page(source.OrderByDescending(x => x.StartedAt).ThenByDescending(x => x.Id), query, MapOffboarding));
    }
    public Task<OffboardingDto> GetOffboardingAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(MapOffboarding(Offboarding(id)));
    public async Task<OffboardingDto> StartOffboardingAsync(OffboardingCreate request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        CheckGuid(request.EmployeeId, "EmployeeId");
        var notes = Optional(request.Notes, "Notes", 2000);
        var employee = Employee(request.EmployeeId);
        if (employee.Status != EmployeeStatus.Active) throw new ConflictException("Employee must be active to start offboarding.");
        if (db.Offboardings.Any(x => x.OrganizationId == OrgId && x.EmployeeId == employee.Id && x.CompletedAt == null))
            throw new ConflictException("Employee already has an open offboarding.");
        var now = Now; employee.StartOffboarding(now);
        var process = new OffboardingProcess(OrgId, employee.Id, UserId, notes, now);
        db.Add(process); Audit("Started", "OffboardingProcess", process.Id, now);
        Audit("OffboardingStarted", "Employee", employee.Id, now);
        await db.SaveChangesAsync(ct); return MapOffboarding(process);
    }
    public async Task<OffboardingDto> CompleteOffboardingAsync(Guid id, CancellationToken ct = default)
    {
        var process = Offboarding(id);
        if (process.CompletedAt is not null) throw new ConflictException("Offboarding has already been completed.");
        var employee = Employee(process.EmployeeId);
        if (employee.Status != EmployeeStatus.Offboarding) throw new ConflictException("Employee is not in offboarding.");
        if (PendingAssetIds(employee.Id).Length > 0) throw new ConflictException("Employee still has pending assets.");
        var now = Now; employee.CompleteOffboarding(now); process.Complete(UserId, now);
        Audit("Completed", "OffboardingProcess", id, now);
        Audit("DeactivatedAfterOffboarding", "Employee", employee.Id, now);
        await db.SaveChangesAsync(ct); return MapOffboarding(process);
    }
    private OffboardingDto MapOffboarding(OffboardingProcess x) => new(x.Id, x.EmployeeId, x.StartedAt,
        x.StartedByUserId, x.Notes, x.CompletedAt, x.CompletedByUserId, PendingAssetIds(x.EmployeeId));
    private Guid[] PendingAssetIds(Guid employeeId)
    {
        var custodyIds = db.Custodies.Where(x => x.OrganizationId == OrgId && x.EmployeeId == employeeId)
            .Select(x => x.Id).ToArray();
        return db.CustodyItems.Where(x => x.OrganizationId == OrgId && x.ReturnedAt == null &&
            custodyIds.Contains(x.CustodyId)).Select(x => x.AssetId).ToArray();
    }
}
