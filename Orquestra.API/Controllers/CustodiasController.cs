using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/custodias")]
public sealed class CustodiasController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 20,
        [FromQuery(Name = "busca")] string? search = null, CancellationToken ct = default)
        => Ok(await service.GetCustodiesAsync(new PageQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetCustodyAsync(id, ct));

    [HttpGet("{id:guid}/termo")]
    [Authorize(Policy = Policies.ManageRecords)]
    [ResponseCache(NoStore = true)]
    [Produces("application/pdf")]
    public IActionResult Termo(Guid id) =>
        File(service.ObterTermo(id), "application/pdf", $"termo-{id:N}.pdf");

    [HttpPost]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Create([FromBody] CustodyCreate request, CancellationToken ct)
    {
        var result = await service.CreateCustodyAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/devolver")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Return(Guid id, [FromBody] CustodyReturn request, CancellationToken ct)
        => Ok(await service.ReturnCustodyAsync(id, request, ct));
}
