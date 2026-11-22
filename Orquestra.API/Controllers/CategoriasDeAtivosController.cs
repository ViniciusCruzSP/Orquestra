using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categorias-de-ativos")]
public sealed class CategoriasDeAtivosController(IOrquestraService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 20,
        [FromQuery(Name = "busca")] string? search = null, CancellationToken ct = default)
        => Ok(await service.GetCategoriesAsync(new PageQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetCategoryAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Create([FromBody] CategoryWrite request, CancellationToken ct)
    {
        var result = await service.CreateCategoryAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManageRecords)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryWrite request, CancellationToken ct)
        => Ok(await service.UpdateCategoryAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.ManageCritical)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteCategoryAsync(id, ct);
        return NoContent();
    }
}
