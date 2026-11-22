using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Dtos;
using Orquestra.Application.Services;
using Orquestra.Domain.Enums;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios")]
public sealed class RelatoriosController(IOrquestraService service) : ControllerBase
{
    [HttpGet("historico")]
    public ActionResult<PagedResult<MovimentoInventario>> Historico([FromQuery] string? busca = null,
        [FromQuery] AssetStatus? situacao = null, [FromQuery] DateOnly? de = null, [FromQuery] DateOnly? ate = null,
        [FromQuery] int pagina = 1, [FromQuery] int itensPorPagina = 20)
        => service.HistoricoInventario(new PageQuery(pagina, itensPorPagina, busca, AssetStatus: situacao, De: de, Ate: ate));
}
