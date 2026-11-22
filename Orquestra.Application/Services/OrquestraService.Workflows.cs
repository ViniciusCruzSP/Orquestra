using Orquestra.Application.Dtos;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService
{
    public Task<PagedResult<CustodyDto>> GetCustodiesAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.Custodies.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null)
        {
            var employeeIds = db.Employees.Where(e => e.OrganizationId == OrgId &&
                (e.FullName.Contains(query.Search) || e.Email.Contains(query.Search))).Select(e => e.Id);
            source = source.Where(x => employeeIds.Contains(x.EmployeeId));
        }
        return Task.FromResult(Page(source.OrderByDescending(x => x.DeliveredAt).ThenByDescending(x => x.Id), query, MapCustody));
    }
    public Task<CustodyDto> GetCustodyAsync(Guid id, CancellationToken ct = default) => Task.FromResult(MapCustody(Custody(id)));

    public byte[] ObterTermo(Guid custodiaId) => db.TermosCompromisso
        .Where(x => x.OrganizationId == OrgId && x.CustodiaId == custodiaId)
        .Select(x => x.Pdf).SingleOrDefault()
        ?? throw new NotFoundException("Termo de compromisso não encontrado.");

    public async Task<CustodyDto> CreateCustodyAsync(CustodyCreate request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("O corpo da requisição é obrigatório.");
        CheckGuid(request.EmployeeId, "colaboradorId");
        if (request.AssetIds is null || request.AssetIds.Count is < 1 or > 100 || request.AssetIds.Any(x => x == Guid.Empty))
            throw new ValidationException("Informe entre 1 e 100 ativos.");
        var ids = request.AssetIds.Distinct().ToArray();
        if (ids.Length != request.AssetIds.Count) throw new ValidationException("Os ativos informados não podem se repetir.");
        var notes = Optional(request.Notes, "observações", 2000);
        var employee = Employee(request.EmployeeId);
        if (employee.Status != EmployeeStatus.Active) throw new ConflictException("Os ativos só podem ser entregues a colaboradores ativos.");
        var assets = db.Assets.Where(x => x.OrganizationId == OrgId && ids.Contains(x.Id)).ToDictionary(x => x.Id);
        if (assets.Count != ids.Length) throw new ValidationException("Um ou mais ativos não existem nesta organização.");
        if (assets.Values.Any(x => !x.IsActive || x.Status != AssetStatus.Available))
            throw new ConflictException("Todos os ativos devem estar ativos e disponíveis.");
        if (db.CustodyItems.Any(x => x.OrganizationId == OrgId && ids.Contains(x.AssetId) && x.ReturnedAt == null))
            throw new ConflictException("Um ou mais ativos já possuem uma custódia ativa.");
        var now = Now; employee.Touch(now);
        var custody = new Custody(OrgId, employee.Id, UserId, notes, now);
        var organizacao = db.Organizations.Single(x => x.Id == OrgId);
        var departamento = db.Departments.Where(x => x.OrganizationId == OrgId && x.Id == employee.DepartmentId)
            .Select(x => x.Name).SingleOrDefault();
        var equipamentos = assets.Values.OrderBy(x => x.AssetTag)
            .Select(x => new EquipamentoEntregue(x.AssetTag, x.Name, x.Brand, x.Model, x.SerialNumber)).ToArray();
        var pdf = geradorTermo.Gerar(new DadosTermo(custody.Id, organizacao.Name,
            employee.FullName, employee.Email, departamento, now, equipamentos));
        db.Add(custody);
        db.Add(new TermoCompromisso(OrgId, custody.Id, now, pdf));
        foreach (var asset in assets.Values)
        {
            var before = asset.Status; asset.Assign(now);
            db.Add(new CustodyItem(OrgId, custody.Id, asset.Id, now));
            db.Add(new AssetMovement(OrgId, asset.Id, custody.Id, MovementType.Assigned, before,
                asset.Status, UserId, notes, now));
            Audit("Entregue", "Ativo", asset.Id, now);
        }
        Audit("Criada", "Custódia", custody.Id, now);
        Audit("Termo gerado", "Custódia", custody.Id, now);
        await db.SaveChangesAsync(ct); return MapCustody(custody);
    }
    public async Task<CustodyDto> ReturnCustodyAsync(Guid id, CustodyReturn request, CancellationToken ct = default)
    {
        if (request is null || request.Items is null || request.Items.Count is < 1 or > 100)
            throw new ValidationException("Informe entre 1 e 100 itens para devolução.");
        if (request.Items.Any(x => x is null)) throw new ValidationException("Os itens da devolução não podem ser nulos.");
        var custody = Custody(id);
        var ids = request.Items.Select(x => x.AssetId).ToArray();
        if (ids.Any(x => x == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ValidationException("Os ativos devolvidos devem ser válidos e não podem se repetir.");
        if (request.Items.Any(x => !Enum.IsDefined(x.Disposition)))
            throw new ValidationException("O destino informado para a devolução é inválido.");
        var items = db.CustodyItems.Where(x => x.OrganizationId == OrgId && x.CustodyId == id &&
            ids.Contains(x.AssetId) && x.ReturnedAt == null).ToDictionary(x => x.AssetId);
        if (items.Count != ids.Length) throw new ConflictException("Todos os itens devolvidos devem estar ativos nesta custódia.");
        var assets = db.Assets.Where(x => x.OrganizationId == OrgId && ids.Contains(x.Id)).ToDictionary(x => x.Id);
        if (assets.Count != ids.Length) throw new ConflictException("Um ativo da custódia não foi encontrado.");
        var now = Now;
        foreach (var requestItem in request.Items)
        {
            var notes = Optional(requestItem.Notes, "observações", 2000);
            var asset = assets[requestItem.AssetId]; var before = asset.Status;
            try { asset.Return(requestItem.Disposition, now); }
            catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
            items[asset.Id].Return(UserId, notes, now);
            db.Add(new AssetMovement(OrgId, asset.Id, id, MovementType.Returned, before,
                asset.Status, UserId, notes, now));
            Audit("Devolvido", "Ativo", asset.Id, now);
        }
        Audit("Itens devolvidos", "Custódia", id, now);
        await db.SaveChangesAsync(ct); return MapCustody(custody);
    }
    private CustodyDto MapCustody(Custody x)
    {
        var items = db.CustodyItems.Where(i => i.OrganizationId == OrgId && i.CustodyId == x.Id)
            .OrderBy(i => i.AssignedAt).ThenBy(i => i.Id)
            .ToList().Select(i => new CustodyItemDto(i.Id, i.AssetId, i.AssignedAt, i.ReturnedAt,
                i.ReturnedByUserId, i.ReturnNotes)).ToArray();
        return new CustodyDto(x.Id, x.EmployeeId, x.DeliveredAt, x.DeliveredByUserId, x.Notes, items);
    }

    public Task<PagedResult<OffboardingDto>> GetOffboardingsAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var source = db.Offboardings.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null)
        {
            var employeeIds = db.Employees.Where(e => e.OrganizationId == OrgId &&
                (e.FullName.Contains(query.Search) || e.Email.Contains(query.Search))).Select(e => e.Id);
            source = source.Where(x => employeeIds.Contains(x.EmployeeId));
        }
        return Task.FromResult(Page(source.OrderByDescending(x => x.StartedAt).ThenByDescending(x => x.Id), query, MapOffboarding));
    }
    public Task<OffboardingDto> GetOffboardingAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(MapOffboarding(Offboarding(id)));
    public async Task<OffboardingDto> StartOffboardingAsync(OffboardingCreate request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("O corpo da requisição é obrigatório.");
        CheckGuid(request.EmployeeId, "colaboradorId");
        var notes = Optional(request.Notes, "observações", 2000);
        var employee = Employee(request.EmployeeId);
        if (employee.Status != EmployeeStatus.Active) throw new ConflictException("O colaborador deve estar ativo para iniciar o desligamento.");
        if (db.Offboardings.Any(x => x.OrganizationId == OrgId && x.EmployeeId == employee.Id && x.CompletedAt == null))
            throw new ConflictException("O colaborador já possui um desligamento em aberto.");
        var now = Now; employee.StartOffboarding(now);
        var process = new OffboardingProcess(OrgId, employee.Id, UserId, notes, now);
        db.Add(process); Audit("Iniciado", "ProcessoDeDesligamento", process.Id, now);
        Audit("Desligamento iniciado", "Colaborador", employee.Id, now);
        await db.SaveChangesAsync(ct); return MapOffboarding(process);
    }
    public async Task<OffboardingDto> CompleteOffboardingAsync(Guid id, CancellationToken ct = default)
    {
        var process = Offboarding(id);
        if (process.CompletedAt is not null) throw new ConflictException("O desligamento já foi concluído.");
        var employee = Employee(process.EmployeeId);
        if (employee.Status != EmployeeStatus.Offboarding) throw new ConflictException("O colaborador não está em processo de desligamento.");
        if (PendingAssetIds(employee.Id).Length > 0) throw new ConflictException("O colaborador ainda possui ativos pendentes.");
        var now = Now; employee.CompleteOffboarding(now); process.Complete(UserId, now);
        Audit("Concluído", "ProcessoDeDesligamento", id, now);
        Audit("Inativado após desligamento", "Colaborador", employee.Id, now);
        await db.SaveChangesAsync(ct); return MapOffboarding(process);
    }
    private OffboardingDto MapOffboarding(OffboardingProcess x) => new(x.Id, x.EmployeeId, x.StartedAt,
        x.StartedByUserId, x.Notes, x.CompletedAt, x.CompletedByUserId, PendingAssetIds(x.EmployeeId));
    private Guid[] PendingAssetIds(Guid employeeId)
    {
        var custodyIds = db.Custodies.Where(x => x.OrganizationId == OrgId && x.EmployeeId == employeeId)
            .Select(x => x.Id).ToArray();
        return db.CustodyItems.Where(x => x.OrganizationId == OrgId && x.ReturnedAt == null &&
            custodyIds.Contains(x.CustodyId)).Select(x => x.AssetId).ToArray();
    }
}
