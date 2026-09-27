using Orquestra.Domain.Enums;

namespace Orquestra.Domain.Entities;

public sealed class Department
{
    private Department() { }
    public Department(Guid organizationId, string name, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; Name = name; IsActive = true; CreatedAt = UpdatedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void Update(string name, DateTimeOffset now) { Name = name; UpdatedAt = now; }
    public void Deactivate(DateTimeOffset now) { IsActive = false; UpdatedAt = now; }
}

public sealed class Employee
{
    private Employee() { }
    public Employee(Guid organizationId, Guid? departmentId, string fullName, string email, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; DepartmentId = departmentId; FullName = fullName; Email = email;
        Status = EmployeeStatus.Active; CreatedAt = UpdatedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public EmployeeStatus Status { get; private set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void Update(Guid? departmentId, string fullName, string email, DateTimeOffset now)
    {
        if (Status != EmployeeStatus.Active) throw new InvalidOperationException("Only active employees can be edited.");
        DepartmentId = departmentId; FullName = fullName; Email = email; UpdatedAt = now;
    }
    public void Touch(DateTimeOffset now)
    {
        if (Status != EmployeeStatus.Active) throw new InvalidOperationException("Employee is not active.");
        UpdatedAt = now;
    }
    public void StartOffboarding(DateTimeOffset now)
    {
        if (Status != EmployeeStatus.Active) throw new InvalidOperationException("Employee must be active to start offboarding.");
        Status = EmployeeStatus.Offboarding; UpdatedAt = now;
    }
    public void CompleteOffboarding(DateTimeOffset now)
    {
        if (Status != EmployeeStatus.Offboarding) throw new InvalidOperationException("Employee is not in offboarding.");
        Status = EmployeeStatus.Inactive; UpdatedAt = now;
    }
    public void Deactivate(DateTimeOffset now)
    {
        if (Status != EmployeeStatus.Active) throw new InvalidOperationException("Employee cannot be deactivated in this state.");
        Status = EmployeeStatus.Inactive; UpdatedAt = now;
    }
}

public sealed class AssetCategory
{
    private AssetCategory() { }
    public AssetCategory(Guid organizationId, string name, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; Name = name; IsActive = true; CreatedAt = UpdatedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void Update(string name, DateTimeOffset now) { Name = name; UpdatedAt = now; }
    public void Deactivate(DateTimeOffset now) { IsActive = false; UpdatedAt = now; }
}
