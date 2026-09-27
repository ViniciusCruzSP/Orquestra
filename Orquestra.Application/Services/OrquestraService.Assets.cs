using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService
{
    public Task<PagedResult<AssetDto>> GetAssetsAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.Assets.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null) source = source.Where(x => x.AssetTag.Contains(query.Search) ||
            x.Name.Contains(query.Search) || (x.SerialNumber != null && x.SerialNumber.Contains(query.Search)));
        if (query.AssetStatus is not null) source = source.Where(x => x.Status == query.AssetStatus);
        return Task.FromResult(Page(source.OrderBy(x => x.AssetTag).ThenBy(x => x.Id), query, Map));
    }
    public Task<AssetDto> GetAssetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Map(Asset(id)));
    public async Task<AssetDto> CreateAssetAsync(AssetWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var data = CheckAsset(request);
        if (db.Assets.Any(x => x.OrganizationId == OrgId && x.AssetTag == data.Tag))
            throw new ConflictException("Asset tag already exists in this organization.");
        var now = Now;
        var asset = new Asset(OrgId, request.CategoryId, data.Tag, data.Name, data.Brand, data.Model,
            data.Serial, data.Notes, now);
        db.Add(asset);
        db.Add(new AssetMovement(OrgId, asset.Id, null, MovementType.Registered, AssetStatus.Available,
            AssetStatus.Available, UserId, null, now));
        Audit("Registered", "Asset", asset.Id, now);
        await db.SaveChangesAsync(ct); return Map(asset);
    }
    public async Task<AssetDto> UpdateAssetAsync(Guid id, AssetWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var asset = Asset(id); var data = CheckAsset(request);
        byte[] expectedVersion;
        try { expectedVersion = Convert.FromBase64String(Required(request.RowVersion, "RowVersion", 64)); }
        catch (FormatException) { throw new ValidationException("RowVersion must be Base64."); }
        if (!asset.RowVersion.SequenceEqual(expectedVersion)) throw new ConflictException("Asset was changed by another request. Reload it and retry.");
        if (db.Assets.Any(x => x.OrganizationId == OrgId && x.Id != id && x.AssetTag == data.Tag))
            throw new ConflictException("Asset tag already exists in this organization.");
        var now = Now;
        try { asset.Update(request.CategoryId, data.Tag, data.Name, data.Brand, data.Model, data.Serial, data.Notes, now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        db.Add(new AssetMovement(OrgId, id, null, MovementType.Updated, asset.Status,
            asset.Status, UserId, null, now));
        Audit("Updated", "Asset", id, now);
        await db.SaveChangesAsync(ct); return Map(asset);
    }
    public async Task DeleteAssetAsync(Guid id, CancellationToken ct = default)
    {
        var asset = Asset(id);
        if (!asset.IsActive) return;
        var now = Now; var before = asset.Status;
        try { asset.Deactivate(now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        db.Add(new AssetMovement(OrgId, id, null, MovementType.Deactivated, before, before, UserId, null, now));
        Audit("Deactivated", "Asset", id, now);
        await db.SaveChangesAsync(ct);
    }
    public Task<PagedResult<MovementDto>> GetAssetHistoryAsync(Guid id, PageQuery query, CancellationToken ct = default)
    {
        _ = Asset(id); query = CheckPage(query);
        var source = db.AssetMovements.Where(x => x.OrganizationId == OrgId && x.AssetId == id)
            .OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id);
        return Task.FromResult(Page(source, query, Map));
    }
    public Task<AssetDto> StartMaintenanceAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.MaintenanceStarted, (asset, now) => asset.StartMaintenance(now), ct);
    public Task<AssetDto> CompleteMaintenanceAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.MaintenanceCompleted, (asset, now) => asset.CompleteMaintenance(now), ct);
    public Task<AssetDto> MarkDefectiveAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.MarkedDefective, (asset, now) => asset.MarkDefective(now), ct);
    public Task<AssetDto> DiscardAssetAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.Discarded, (asset, now) => asset.Discard(now), ct);

    private async Task<AssetDto> ChangeStateAsync(Guid id, StateChangeRequest request, MovementType type,
        Action<Asset, DateTimeOffset> transition, CancellationToken ct)
    {
        var notes = Optional(request?.Notes, "Notes", 2000);
        var asset = Asset(id); var before = asset.Status; var now = Now;
        try { transition(asset, now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        db.Add(new AssetMovement(OrgId, id, null, type, before, asset.Status, UserId, notes, now));
        Audit(type.ToString(), "Asset", id, now);
        await db.SaveChangesAsync(ct); return Map(asset);
    }
    private (string Tag, string Name, string Brand, string Model, string? Serial, string? Notes) CheckAsset(AssetWrite request)
    {
        CheckGuid(request.CategoryId, "CategoryId");
        if (!db.AssetCategories.Any(x => x.OrganizationId == OrgId && x.Id == request.CategoryId && x.IsActive))
            throw new ValidationException("Category does not exist or is inactive.");
        return (Required(request.AssetTag, "AssetTag", 80), Required(request.Name, "Name", 160),
            Required(request.Brand, "Brand", 120), Required(request.Model, "Model", 120),
            Optional(request.SerialNumber, "SerialNumber", 120), Optional(request.Notes, "Notes", 2000));
    }
}
