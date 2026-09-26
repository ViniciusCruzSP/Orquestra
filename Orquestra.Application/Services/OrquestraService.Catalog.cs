using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService
{
    public Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.Departments.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null) source = source.Where(x => x.Name.Contains(query.Search));
        return Task.FromResult(Page(source.OrderBy(x => x.Name).ThenBy(x => x.Id), query, Map));
    }
    public Task<DepartmentDto> GetDepartmentAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Map(Department(id)));
    public async Task<DepartmentDto> CreateDepartmentAsync(DepartmentWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var name = Required(request.Name, "Name", 120);
        if (db.Departments.Any(x => x.OrganizationId == OrgId && x.Name == name && x.IsActive))
            throw new ConflictException("An active department with this name already exists.");
        var now = Now; var entity = new Department(OrgId, name, now);
        db.Add(entity); Audit("Created", "Department", entity.Id, now);
        await db.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid id, DepartmentWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var entity = Department(id);
        if (!entity.IsActive) throw new ConflictException("Department is inactive.");
        var name = Required(request.Name, "Name", 120);
        if (db.Departments.Any(x => x.OrganizationId == OrgId && x.Id != id && x.Name == name && x.IsActive))
            throw new ConflictException("An active department with this name already exists.");
        var now = Now; entity.Update(name, now); Audit("Updated", "Department", id, now);
        await db.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default)
    {
        var entity = Department(id);
        if (!entity.IsActive) return;
        if (db.Employees.Any(x => x.OrganizationId == OrgId && x.DepartmentId == id && x.Status != EmployeeStatus.Inactive))
            throw new ConflictException("Department has active employees.");
        var now = Now; entity.Deactivate(now); Audit("Deactivated", "Department", id, now);
        await db.SaveChangesAsync(ct);
    }

    public Task<PagedResult<EmployeeDto>> GetEmployeesAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.Employees.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null) source = source.Where(x => x.FullName.Contains(query.Search) || x.Email.Contains(query.Search));
        if (query.EmployeeStatus is not null) source = source.Where(x => x.Status == query.EmployeeStatus);
        return Task.FromResult(Page(source.OrderBy(x => x.FullName).ThenBy(x => x.Id), query, Map));
    }
    public Task<EmployeeDto> GetEmployeeAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Map(Employee(id)));
    public async Task<EmployeeDto> CreateEmployeeAsync(EmployeeWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var name = Required(request.FullName, "FullName", 160); var email = Email(request.Email);
        CheckDepartment(request.DepartmentId);
        if (db.Employees.Any(x => x.OrganizationId == OrgId && x.Email == email && x.Status != EmployeeStatus.Inactive))
            throw new ConflictException("An active employee with this email already exists.");
        var now = Now; var entity = new Employee(OrgId, request.DepartmentId, name, email, now);
        db.Add(entity); Audit("Created", "Employee", entity.Id, now);
        await db.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task<EmployeeDto> UpdateEmployeeAsync(Guid id, EmployeeWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var entity = Employee(id); var name = Required(request.FullName, "FullName", 160); var email = Email(request.Email);
        CheckDepartment(request.DepartmentId);
        if (db.Employees.Any(x => x.OrganizationId == OrgId && x.Id != id && x.Email == email && x.Status != EmployeeStatus.Inactive))
            throw new ConflictException("An active employee with this email already exists.");
        var now = Now;
        try { entity.Update(request.DepartmentId, name, email, now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        Audit("Updated", "Employee", id, now); await db.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task DeleteEmployeeAsync(Guid id, CancellationToken ct = default)
    {
        var entity = Employee(id);
        if (entity.Status == EmployeeStatus.Inactive) return;
        var custodyIds = db.Custodies.Where(c => c.OrganizationId == OrgId && c.EmployeeId == id).Select(c => c.Id);
        if (db.CustodyItems.Any(i => i.OrganizationId == OrgId && i.ReturnedAt == null &&
            custodyIds.Contains(i.CustodyId)))
            throw new ConflictException("Employee has pending assets.");
        var now = Now;
        try { entity.Deactivate(now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        Audit("Deactivated", "Employee", id, now); await db.SaveChangesAsync(ct);
    }
    private void CheckDepartment(Guid? id)
    {
        if (id is null) return;
        if (!db.Departments.Any(x => x.OrganizationId == OrgId && x.Id == id && x.IsActive))
            throw new ValidationException("Department does not exist or is inactive.");
    }

    public Task<PagedResult<CategoryDto>> GetCategoriesAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.AssetCategories.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null) source = source.Where(x => x.Name.Contains(query.Search));
        return Task.FromResult(Page(source.OrderBy(x => x.Name).ThenBy(x => x.Id), query, Map));
    }
    public Task<CategoryDto> GetCategoryAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Map(Category(id)));
    public async Task<CategoryDto> CreateCategoryAsync(CategoryWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var name = Required(request.Name, "Name", 120);
        if (db.AssetCategories.Any(x => x.OrganizationId == OrgId && x.Name == name && x.IsActive))
            throw new ConflictException("An active category with this name already exists.");
        var now = Now; var entity = new AssetCategory(OrgId, name, now);
        db.Add(entity); Audit("Created", "AssetCategory", entity.Id, now);
        await db.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, CategoryWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("Request body is required.");
        var entity = Category(id);
        if (!entity.IsActive) throw new ConflictException("Category is inactive.");
        var name = Required(request.Name, "Name", 120);
        if (db.AssetCategories.Any(x => x.OrganizationId == OrgId && x.Id != id && x.Name == name && x.IsActive))
            throw new ConflictException("An active category with this name already exists.");
        var now = Now; entity.Update(name, now); Audit("Updated", "AssetCategory", id, now);
        await db.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var entity = Category(id);
        if (!entity.IsActive) return;
        if (db.Assets.Any(x => x.OrganizationId == OrgId && x.CategoryId == id && x.IsActive))
            throw new ConflictException("Category has active assets.");
        var now = Now; entity.Deactivate(now); Audit("Deactivated", "AssetCategory", id, now);
        await db.SaveChangesAsync(ct);
    }
}
