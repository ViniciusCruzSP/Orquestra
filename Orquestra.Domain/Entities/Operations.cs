using Orquestra.Domain.Enums;

namespace Orquestra.Domain.Entities;

public sealed class Custody
{
    private Custody() { }
    public Custody(Guid organizationId, Guid employeeId, string deliveredByUserId, string? notes, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; EmployeeId = employeeId;
        DeliveredByUserId = deliveredByUserId; Notes = notes; DeliveredAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTimeOffset DeliveredAt { get; private set; }
    public string DeliveredByUserId { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
}

public sealed class CustodyItem
{
    private CustodyItem() { }
    public CustodyItem(Guid organizationId, Guid custodyId, Guid assetId, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; CustodyId = custodyId; AssetId = assetId; AssignedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CustodyId { get; private set; }
    public Guid AssetId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public string? ReturnedByUserId { get; private set; }
    public string? ReturnNotes { get; private set; }
    public void Return(string userId, string? notes, DateTimeOffset now)
    {
        if (ReturnedAt is not null) throw new InvalidOperationException("O item da custódia já foi devolvido.");
        ReturnedAt = now; ReturnedByUserId = userId; ReturnNotes = notes;
    }
}

public sealed class AssetMovement
{
    private AssetMovement() { }
    public AssetMovement(Guid organizationId, Guid assetId, Guid? custodyId, MovementType type,
        AssetStatus previousStatus, AssetStatus newStatus, string performedByUserId, string? notes, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; AssetId = assetId; CustodyId = custodyId;
        Type = type; PreviousStatus = previousStatus; NewStatus = newStatus;
        PerformedByUserId = performedByUserId; Notes = notes; OccurredAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid? CustodyId { get; private set; }
    public MovementType Type { get; private set; }
    public AssetStatus PreviousStatus { get; private set; }
    public AssetStatus NewStatus { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string PerformedByUserId { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
}

public sealed class OffboardingProcess
{
    private OffboardingProcess() { }
    public OffboardingProcess(Guid organizationId, Guid employeeId, string startedByUserId, string? notes, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; EmployeeId = employeeId;
        StartedByUserId = startedByUserId; Notes = notes; StartedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public string StartedByUserId { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CompletedByUserId { get; private set; }
    public void Complete(string userId, DateTimeOffset now)
    {
        if (CompletedAt is not null) throw new InvalidOperationException("O desligamento já foi concluído.");
        CompletedAt = now; CompletedByUserId = userId;
    }
}

public sealed class AuditLog
{
    private AuditLog() { }
    public AuditLog(Guid organizationId, string action, string entityType, Guid entityId, string userId,
        DateTimeOffset now, string? details = null)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; Action = action; EntityType = entityType;
        EntityId = entityId; UserId = userId; OccurredAt = now; Details = details;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public string? Details { get; private set; }
}
