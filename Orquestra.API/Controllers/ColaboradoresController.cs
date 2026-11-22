using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;
using Orquestra.Domain.Enums;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/colaboradores")]
public sealed class ColaboradoresController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 20,
        [FromQuery(Name = "busca")] string? search = null,
        [FromQuery(Name = "situacao")] EmployeeStatus? employeeStatus = null,
        CancellationToken ct = default)
        => Ok(await service.GetEmployeesAsync(new PageQuery(page, pageSize, search, EmployeeStatus: employeeStatus), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetEmployeeAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] EmployeeWrite request, CancellationToken ct)
    {
        var result = await service.CreateEmployeeAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] EmployeeWrite request, CancellationToken ct)
        => Ok(await service.UpdateEmployeeAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteEmployeeAsync(id, ct);
        return NoContent();
    }
}
