using Orquestra.Domain.Enums;

namespace Orquestra.Application.Dtos;

public record PageQuery(int Page = 1, int PageSize = 20, string? Search = null,
    AssetStatus? AssetStatus = null, EmployeeStatus? EmployeeStatus = null);
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record DepartmentWrite(string Name);
public record DepartmentDto(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record EmployeeWrite(Guid? DepartmentId, string FullName, string Email);
public record EmployeeDto(Guid Id, Guid? DepartmentId, string FullName, string Email, EmployeeStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record CategoryWrite(string Name);
public record CategoryDto(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record AssetWrite(Guid CategoryId, string AssetTag, string Name, string Brand, string Model,
    string? SerialNumber, string? Notes, string? RowVersion = null);
public record AssetDto(Guid Id, Guid CategoryId, string AssetTag, string Name, string Brand, string Model,
    string? SerialNumber, string? Notes, AssetStatus Status, bool IsActive, string RowVersion,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record StateChangeRequest(string? Notes);
public record MovementDto(Guid Id, Guid AssetId, Guid? CustodyId, MovementType Type, AssetStatus PreviousStatus,
    AssetStatus NewStatus, DateTimeOffset OccurredAt, string PerformedByUserId, string? Notes);

public record CustodyCreate(Guid EmployeeId, IReadOnlyList<Guid> AssetIds, string? Notes);
public record CustodyReturnItem(Guid AssetId, ReturnDisposition Disposition, string? Notes);
public record CustodyReturn(IReadOnlyList<CustodyReturnItem> Items);
public record CustodyItemDto(Guid Id, Guid AssetId, DateTimeOffset AssignedAt, DateTimeOffset? ReturnedAt,
    string? ReturnedByUserId, string? ReturnNotes);
public record CustodyDto(Guid Id, Guid EmployeeId, DateTimeOffset DeliveredAt, string DeliveredByUserId,
    string? Notes, IReadOnlyList<CustodyItemDto> Items);

public record OffboardingCreate(Guid EmployeeId, string? Notes);
public record OffboardingDto(Guid Id, Guid EmployeeId, DateTimeOffset StartedAt, string StartedByUserId,
    string? Notes, DateTimeOffset? CompletedAt, string? CompletedByUserId, IReadOnlyList<Guid> PendingAssetIds);

public record DashboardDto(int TotalAssets, int AvailableAssets, int AssignedAssets, int MaintenanceAssets,
    int DefectiveAssets, int DiscardedAssets, int ActiveEmployees, int OpenOffboardings, int PendingCustodyItems);
public record AuditLogDto(Guid Id, string Action, string EntityType, Guid EntityId, string UserId,
    DateTimeOffset OccurredAt, string? Details);
