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
        return await db.Set<Custody>().Where(x => x.OrganizationId == sessao.OrganizationId && x.EmployeeId == pessoa)
            .OrderByDescending(x => x.DeliveredAt).ThenBy(x => x.Id).Skip((pagina - 1) * 20).Take(20)
            .Select(x => new CustodiaPessoal(x.Id, x.DeliveredAt,
                db.Set<CustodyItem>().Where(i => i.OrganizationId == x.OrganizationId && i.CustodyId == x.Id)
                    .Join(db.Set<Asset>(), i => i.AssetId, a => a.Id,
                        (i, a) => new ItemPessoal(a.Id, a.AssetTag, a.Name, i.ReturnedAt))
                    .OrderBy(i => i.Patrimonio).ToList())).ToListAsync(ct);
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
