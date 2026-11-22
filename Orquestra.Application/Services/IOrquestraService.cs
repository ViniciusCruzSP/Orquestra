using Orquestra.Application.Dtos;

namespace Orquestra.Application.Services;

public interface IOrquestraService
{
    PagedResult<MovimentoInventario> HistoricoInventario(PageQuery query);
    Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(PageQuery query, CancellationToken ct = default);
    Task<DepartmentDto> GetDepartmentAsync(Guid id, CancellationToken ct = default);
    Task<DepartmentDto> CreateDepartmentAsync(DepartmentWrite request, CancellationToken ct = default);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid id, DepartmentWrite request, CancellationToken ct = default);
    Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(PageQuery query, CancellationToken ct = default);
    Task<EmployeeDto> GetEmployeeAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeDto> CreateEmployeeAsync(EmployeeWrite request, CancellationToken ct = default);
    Task<EmployeeDto> UpdateEmployeeAsync(Guid id, EmployeeWrite request, CancellationToken ct = default);
    Task DeleteEmployeeAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<CategoryDto>> GetCategoriesAsync(PageQuery query, CancellationToken ct = default);
    Task<CategoryDto> GetCategoryAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CategoryWrite request, CancellationToken ct = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, CategoryWrite request, CancellationToken ct = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<AssetDto>> GetAssetsAsync(PageQuery query, CancellationToken ct = default);
    Task<AssetDto> GetAssetAsync(Guid id, CancellationToken ct = default);
    Task<AssetDto> CreateAssetAsync(AssetWrite request, CancellationToken ct = default);
    Task<AssetDto> UpdateAssetAsync(Guid id, AssetWrite request, CancellationToken ct = default);
    Task DeleteAssetAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MovementDto>> GetAssetHistoryAsync(Guid id, PageQuery query, CancellationToken ct = default);
    Task<AssetDto> StartMaintenanceAsync(Guid id, StateChangeRequest request, CancellationToken ct = default);
    Task<AssetDto> CompleteMaintenanceAsync(Guid id, StateChangeRequest request, CancellationToken ct = default);
    Task<AssetDto> MarkDefectiveAsync(Guid id, StateChangeRequest request, CancellationToken ct = default);
    Task<AssetDto> DiscardAssetAsync(Guid id, StateChangeRequest request, CancellationToken ct = default);

    Task<PagedResult<CustodyDto>> GetCustodiesAsync(PageQuery query, CancellationToken ct = default);
    Task<CustodyDto> GetCustodyAsync(Guid id, CancellationToken ct = default);
    byte[] ObterTermo(Guid custodiaId);
    Task<CustodyDto> CreateCustodyAsync(CustodyCreate request, CancellationToken ct = default);
    Task<CustodyDto> ReturnCustodyAsync(Guid id, CustodyReturn request, CancellationToken ct = default);

    Task<PagedResult<OffboardingDto>> GetOffboardingsAsync(PageQuery query, CancellationToken ct = default);
    Task<OffboardingDto> GetOffboardingAsync(Guid id, CancellationToken ct = default);
    Task<OffboardingDto> StartOffboardingAsync(OffboardingCreate request, CancellationToken ct = default);
    Task<OffboardingDto> CompleteOffboardingAsync(Guid id, CancellationToken ct = default);

    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(PageQuery query, CancellationToken ct = default);
}
