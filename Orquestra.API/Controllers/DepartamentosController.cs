using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/departamentos")]
public sealed class DepartamentosController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 20,
        [FromQuery(Name = "busca")] string? search = null, CancellationToken ct = default)
        => Ok(await service.GetDepartmentsAsync(new PageQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetDepartmentAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Create([FromBody] DepartmentWrite request, CancellationToken ct)
    {
        var result = await service.CreateDepartmentAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Update(Guid id, [FromBody] DepartmentWrite request, CancellationToken ct)
        => Ok(await service.UpdateDepartmentAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.ManageCritical)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteDepartmentAsync(id, ct);
        return NoContent();
    }
}
