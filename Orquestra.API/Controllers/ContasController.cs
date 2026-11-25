using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;
using Orquestra.Infrastructure.Identity;
using Orquestra.Infrastructure.Persistence;

namespace Orquestra.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/contas")]
public sealed class ContasController(UserManager<ApplicationUser> usuarios, OrquestraDbContext db,
    ICurrentUser sessao) : ControllerBase
{
    [HttpGet("{colaboradorId:guid}")]
    public async Task<ActionResult<ContaDto>> Consultar(Guid colaboradorId, CancellationToken ct)
    {
        if (!await db.Set<Employee>().AnyAsync(x => x.Id == colaboradorId && x.OrganizationId == sessao.OrganizationId, ct))
            throw new NotFoundException("Colaborador não encontrado.");

        var conta = await Encontrar(colaboradorId, ct);
        if (conta is null) return NoContent();

        return new ContaDto(conta.Email!, (await usuarios.GetRolesAsync(conta)).Single());
    }

    [HttpPut("{colaboradorId:guid}")]
    public async Task<ContaDto> Salvar(Guid colaboradorId, DadosConta dados, CancellationToken ct)
    {
        if (dados.Perfil is not (Roles.Colaborador or Roles.GestorTI or Roles.OperadorTI or Roles.Auditor))
            throw new Orquestra.Application.Errors.ValidationException("Perfil de acesso inválido.");
        var colaborador = await db.Set<Employee>().SingleOrDefaultAsync(x => x.Id == colaboradorId
            && x.OrganizationId == sessao.OrganizationId, ct) ?? throw new NotFoundException("Colaborador não encontrado.");
        if (colaborador.Status != EmployeeStatus.Active)
            throw new ConflictException("Somente colaboradores ativos podem receber uma conta ou trocar de perfil.");

        await using var transacao = await db.Database.BeginTransactionAsync(ct);
        var conta = await Encontrar(colaboradorId, ct);
        if (conta is null)
        {
            conta = new ApplicationUser
            {
                UserName = colaborador.Email, Email = colaborador.Email, OrganizationId = sessao.OrganizationId,
                ColaboradorId = colaboradorId, CreatedAt = DateTimeOffset.UtcNow
            };
            if (string.IsNullOrWhiteSpace(dados.SenhaInicial))
                throw new Orquestra.Application.Errors.ValidationException("Informe uma senha inicial para criar a conta.");
            Verificar(await usuarios.CreateAsync(conta, dados.SenhaInicial));
        }
        var perfis = await usuarios.GetRolesAsync(conta);
        if (!perfis.Contains(dados.Perfil) || perfis.Count != 1)
        {
            if (perfis.Count != 0) Verificar(await usuarios.RemoveFromRolesAsync(conta, perfis));
            Verificar(await usuarios.AddToRoleAsync(conta, dados.Perfil));
            Verificar(await usuarios.UpdateSecurityStampAsync(conta));
            db.Add(new AuditLog(sessao.OrganizationId, "Perfil definido", "Conta", colaboradorId, sessao.UserId,
                DateTimeOffset.UtcNow, dados.Perfil));
            await db.SaveChangesAsync(ct);
        }
        await transacao.CommitAsync(ct);
        return new(conta.Email!, dados.Perfil);
    }

    private Task<ApplicationUser?> Encontrar(Guid colaboradorId, CancellationToken ct) =>
        usuarios.Users.SingleOrDefaultAsync(x => x.OrganizationId == sessao.OrganizationId
            && x.ColaboradorId == colaboradorId, ct);

    private static void Verificar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new Orquestra.Application.Errors.ValidationException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }
}

public record DadosConta([Required] string Perfil, [StringLength(256)] string? SenhaInicial);
public record ContaDto(string Email, string Perfil);
