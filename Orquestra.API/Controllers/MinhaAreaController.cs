using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Application.Services;
using Orquestra.Domain.Entities;
using Orquestra.Infrastructure.Identity;
using Orquestra.Infrastructure.Persistence;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.Colaborador)]
[Route("api/minha-area")]
public sealed class MinhaAreaController(UserManager<ApplicationUser> usuarios, OrquestraDbContext db,
    ICurrentUser sessao, IOrquestraService servico) : ControllerBase
{
    [HttpGet("custodias")]
    public async Task<IReadOnlyList<CustodiaPessoal>> Custodias([FromQuery] int pagina = 1, CancellationToken ct = default)
    {
        if (pagina is < 1 or > 100000) throw new ValidationException("Página inválida.");
        var pessoa = await Colaborador();
        var organizacao = sessao.OrganizationId;
        var custodias = await db.Set<Custody>().Where(x => x.OrganizationId == organizacao && x.EmployeeId == pessoa)
            .OrderByDescending(x => x.DeliveredAt).ThenBy(x => x.Id).Skip((pagina - 1) * 20).Take(20)
            .Select(x => new { x.Id, x.DeliveredAt })
            .ToListAsync(ct);
        if (custodias.Count == 0) return [];

        var ids = custodias.Select(x => x.Id).ToArray();
        var itens = await (from item in db.Set<CustodyItem>()
                           join ativo in db.Set<Asset>()
                               on new { item.OrganizationId, Id = item.AssetId } equals new { ativo.OrganizationId, ativo.Id }
                           where item.OrganizationId == organizacao && ids.Contains(item.CustodyId)
                           orderby ativo.AssetTag, item.Id
                           select new { item.CustodyId, ativo.Id, ativo.AssetTag, ativo.Name, item.ReturnedAt })
            .ToListAsync(ct);
        var porCustodia = itens.ToLookup(x => x.CustodyId,
            x => new ItemPessoal(x.Id, x.AssetTag, x.Name, x.ReturnedAt));

        return custodias.Select(x => new CustodiaPessoal(x.Id, x.DeliveredAt, porCustodia[x.Id].ToList())).ToArray();
    }

    [HttpGet("custodias/{id:guid}/termo")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> Termo(Guid id, CancellationToken ct)
    {
        var pessoa = await Colaborador();
        if (!await db.Set<Custody>().AnyAsync(x => x.OrganizationId == sessao.OrganizationId
            && x.Id == id && x.EmployeeId == pessoa, ct))
            throw new NotFoundException("Termo de compromisso não encontrado.");
        return File(servico.ObterTermo(id), "application/pdf", $"termo-{id:N}.pdf");
    }

    private async Task<Guid> Colaborador() => (await usuarios.FindByIdAsync(sessao.UserId))?.ColaboradorId
        ?? throw new ForbiddenException("A conta não está vinculada a um colaborador.");
}

public record ItemPessoal(Guid AtivoId, string Patrimonio, string Nome, DateTimeOffset? DevolvidoEm);
public record CustodiaPessoal(Guid Id, DateTimeOffset EntregueEm, List<ItemPessoal> Itens);
