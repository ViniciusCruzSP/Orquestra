using Orquestra.Domain.Enums;

namespace Orquestra.Domain.Entities;

public sealed class Asset
{
    private Asset() { }
    public Asset(Guid organizationId, Guid categoryId, string assetTag, string name, string brand, string model,
        string? serialNumber, string? notes, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; CategoryId = categoryId; AssetTag = assetTag;
        Name = name; Brand = brand; Model = model; SerialNumber = serialNumber; Notes = notes;
        Status = AssetStatus.Available; IsActive = true; CreatedAt = UpdatedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string AssetTag { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string? SerialNumber { get; private set; }
    public string? Notes { get; private set; }
    public AssetStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void Update(Guid categoryId, string assetTag, string name, string brand, string model,
        string? serialNumber, string? notes, DateTimeOffset now)
    {
        if (!IsActive || Status == AssetStatus.Discarded) throw new InvalidOperationException("Asset cannot be edited.");
        CategoryId = categoryId; AssetTag = assetTag; Name = name; Brand = brand; Model = model;
        SerialNumber = serialNumber; Notes = notes; UpdatedAt = now;
    }
    public void Assign(DateTimeOffset now)
    {
        if (!IsActive || Status != AssetStatus.Available) throw new InvalidOperationException("Only available assets can be assigned.");
        Status = AssetStatus.Assigned; UpdatedAt = now;
    }
    public void Return(ReturnDisposition disposition, DateTimeOffset now)
    {
        if (Status != AssetStatus.Assigned) throw new InvalidOperationException("Asset is not assigned.");
        Status = disposition switch
        {
            ReturnDisposition.Available => AssetStatus.Available,
            ReturnDisposition.Maintenance => AssetStatus.Maintenance,
            ReturnDisposition.Defective => AssetStatus.Defective,
            _ => throw new ArgumentOutOfRangeException(nameof(disposition))
        };
        UpdatedAt = now;
    }
    public void StartMaintenance(DateTimeOffset now)
    {
        if (!IsActive || Status is not (AssetStatus.Available or AssetStatus.Defective))
            throw new InvalidOperationException("Only available or defective assets can enter maintenance.");
        Status = AssetStatus.Maintenance; UpdatedAt = now;
    }
    public void CompleteMaintenance(DateTimeOffset now)
    {
        if (Status != AssetStatus.Maintenance) throw new InvalidOperationException("Asset is not in maintenance.");
        Status = AssetStatus.Available; UpdatedAt = now;
    }
    public void MarkDefective(DateTimeOffset now)
    {
        if (!IsActive || Status is not (AssetStatus.Available or AssetStatus.Maintenance))
            throw new InvalidOperationException("Asset cannot be marked defective in this state.");
        Status = AssetStatus.Defective; UpdatedAt = now;
    }
    public void Discard(DateTimeOffset now)
    {
        if (!IsActive || Status is not (AssetStatus.Available or AssetStatus.Defective))
            throw new InvalidOperationException("Asset cannot be discarded in this state.");
        Status = AssetStatus.Discarded; UpdatedAt = now;
    }
    public void Deactivate(DateTimeOffset now)
    {
        if (Status is AssetStatus.Assigned or AssetStatus.Maintenance) throw new InvalidOperationException("Asset has an active workflow.");
        IsActive = false; UpdatedAt = now;
    }
}
