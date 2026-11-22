using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.Audit)]
[Route("api/auditoria")]
public sealed class AuditoriaController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 20,
        [FromQuery(Name = "busca")] string? search = null, CancellationToken ct = default)
        => Ok(await service.GetAuditLogsAsync(new PageQuery(page, pageSize, search), ct));
}
