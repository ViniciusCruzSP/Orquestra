using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/custodies")]
public sealed class CustodiesController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await service.GetCustodiesAsync(new PageQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetCustodyAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Create([FromBody] CustodyCreate request, CancellationToken ct)
    {
        var result = await service.CreateCustodyAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/return")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Return(Guid id, [FromBody] CustodyReturn request, CancellationToken ct)
        => Ok(await service.ReturnCustodyAsync(id, request, ct));
}
