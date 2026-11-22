using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;
using Orquestra.Domain.Enums;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ativos")]
public sealed class AtivosController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 20,
        [FromQuery(Name = "busca")] string? search = null,
        [FromQuery(Name = "situacao")] AssetStatus? assetStatus = null,
        [FromQuery] string? tipo = null, [FromQuery] Guid? responsavelId = null,
        [FromQuery] DateOnly? de = null, [FromQuery] DateOnly? ate = null, [FromQuery] bool? ativo = null,
        CancellationToken ct = default)
        => Ok(await service.GetAssetsAsync(new PageQuery(page, pageSize, search, AssetStatus: assetStatus,
            Tipo: tipo, ResponsavelId: responsavelId, De: de, Ate: ate, Ativo: ativo), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAssetAsync(id, ct));

    [HttpGet("{id:guid}/historico")]
    public async Task<IActionResult> History(Guid id, [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 20, CancellationToken ct = default)
        => Ok(await service.GetAssetHistoryAsync(id, new PageQuery(page, pageSize), ct));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] AssetWrite request, CancellationToken ct)
    {
        var result = await service.CreateAssetAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] AssetWrite request, CancellationToken ct)
        => Ok(await service.UpdateAssetAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAssetAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/manutencao")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Maintenance(Guid id, [FromBody] StateChangeRequest request, CancellationToken ct)
        => Ok(await service.StartMaintenanceAsync(id, request, ct));

    [HttpPost("{id:guid}/concluir-manutencao")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> CompleteMaintenance(Guid id, [FromBody] StateChangeRequest request,
        CancellationToken ct)
        => Ok(await service.CompleteMaintenanceAsync(id, request, ct));

    [HttpPost("{id:guid}/marcar-com-defeito")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> MarkDefective(Guid id, [FromBody] StateChangeRequest request, CancellationToken ct)
        => Ok(await service.MarkDefectiveAsync(id, request, ct));

    [HttpPost("{id:guid}/descartar")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Discard(Guid id, [FromBody] StateChangeRequest request, CancellationToken ct)
        => Ok(await service.DiscardAssetAsync(id, request, ct));
}
