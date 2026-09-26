using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;
using Orquestra.Domain.Enums;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assets")]
public sealed class AssetsController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] AssetStatus? assetStatus = null,
        CancellationToken ct = default)
        => Ok(await service.GetAssetsAsync(new PageQuery(page, pageSize, search, AssetStatus: assetStatus), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAssetAsync(id, ct));

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await service.GetAssetHistoryAsync(id, new PageQuery(page, pageSize), ct));

    [HttpPost]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Create([FromBody] AssetWrite request, CancellationToken ct)
    {
        var result = await service.CreateAssetAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Update(Guid id, [FromBody] AssetWrite request, CancellationToken ct)
        => Ok(await service.UpdateAssetAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.ManageCritical)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAssetAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/maintenance")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Maintenance(Guid id, [FromBody] StateChangeRequest request, CancellationToken ct)
        => Ok(await service.StartMaintenanceAsync(id, request, ct));

    [HttpPost("{id:guid}/complete-maintenance")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> CompleteMaintenance(Guid id, [FromBody] StateChangeRequest request,
        CancellationToken ct)
        => Ok(await service.CompleteMaintenanceAsync(id, request, ct));

    [HttpPost("{id:guid}/mark-defective")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> MarkDefective(Guid id, [FromBody] StateChangeRequest request, CancellationToken ct)
        => Ok(await service.MarkDefectiveAsync(id, request, ct));

    [HttpPost("{id:guid}/discard")]
    [Authorize(Policy = Policies.ManageCritical)]
    public async Task<IActionResult> Discard(Guid id, [FromBody] StateChangeRequest request, CancellationToken ct)
        => Ok(await service.DiscardAssetAsync(id, request, ct));
}
