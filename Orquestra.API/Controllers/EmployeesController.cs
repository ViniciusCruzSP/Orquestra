using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;
using Orquestra.Domain.Enums;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeesController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] EmployeeStatus? employeeStatus = null,
        CancellationToken ct = default)
        => Ok(await service.GetEmployeesAsync(new PageQuery(page, pageSize, search, EmployeeStatus: employeeStatus), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetEmployeeAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Create([FromBody] EmployeeWrite request, CancellationToken ct)
    {
        var result = await service.CreateEmployeeAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Update(Guid id, [FromBody] EmployeeWrite request, CancellationToken ct)
        => Ok(await service.UpdateEmployeeAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.ManageCritical)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteEmployeeAsync(id, ct);
        return NoContent();
    }
}
