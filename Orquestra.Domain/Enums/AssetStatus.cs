namespace Orquestra.Domain.Enums;

public enum AssetStatus { Available = 0, Assigned = 1, Maintenance = 2, Defective = 3, Discarded = 4 }
public enum EmployeeStatus { Active = 0, Offboarding = 1, Inactive = 2 }
public enum MovementType
{
    Registered = 0, Updated = 1, Assigned = 2, Returned = 3, MaintenanceStarted = 4,
    MaintenanceCompleted = 5, MarkedDefective = 6, Discarded = 7, Deactivated = 8
}
public enum ReturnDisposition { Available = 0, Maintenance = 1, Defective = 2 }
