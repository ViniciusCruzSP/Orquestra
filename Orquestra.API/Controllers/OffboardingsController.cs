using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/offboardings")]
public sealed class OffboardingsController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await service.GetOffboardingsAsync(new PageQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetOffboardingAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Create([FromBody] OffboardingCreate request, CancellationToken ct)
    {
        var result = await service.StartOffboardingAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.ManageCritical)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => Ok(await service.CompleteOffboardingAsync(id, ct));
}
