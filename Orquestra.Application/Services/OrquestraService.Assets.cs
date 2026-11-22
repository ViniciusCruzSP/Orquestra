using Orquestra.Application.Dtos;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Services;

public sealed partial class OrquestraService
{
    public Task<PagedResult<AssetDto>> GetAssetsAsync(PageQuery query, CancellationToken ct = default)
    {
        query = CheckPage(query);
        var resultado = Page(FiltrarAtivos(query).OrderBy(x => x.AssetTag).ThenBy(x => x.Id), query, Map);
        var vinculos = VinculosAtuais(resultado.Items.Select(x => x.Id).ToArray());
        return Task.FromResult(resultado with { Items = resultado.Items.Select(x => ComResponsavel(x, vinculos)).ToArray() });
    }

    private IQueryable<Asset> FiltrarAtivos(PageQuery query)
    {
        var source = db.Assets.Where(x => x.OrganizationId == OrgId);
        if (query.Search is not null) source = source.Where(x => x.AssetTag.Contains(query.Search) ||
            x.Name.Contains(query.Search) || (x.SerialNumber != null && x.SerialNumber.Contains(query.Search)));
        if (query.AssetStatus is not null) source = source.Where(x => x.Status == query.AssetStatus);
        if (query.Tipo is not null) source = source.Where(x => x.Tipo == query.Tipo);
        if (query.Ativo is not null) source = source.Where(x => x.IsActive == query.Ativo);
        if (query.De is not null) source = source.Where(x => x.AdquiridoEm >= query.De);
        if (query.Ate is not null) source = source.Where(x => x.AdquiridoEm <= query.Ate);
        if (query.De > query.Ate) throw new ValidationException("O início do período deve ser anterior ou igual ao fim.");
        if (query.ResponsavelId is not null)
        {
            var custodias = db.Custodies.Where(x => x.OrganizationId == OrgId && x.EmployeeId == query.ResponsavelId);
            var itens = db.CustodyItems.Where(x => x.OrganizationId == OrgId && x.ReturnedAt == null &&
                custodias.Any(c => c.Id == x.CustodyId));
            source = source.Where(x => itens.Any(i => i.AssetId == x.Id));
        }
        return source;
    }

    private Dictionary<Guid, (string Nome, Guid CustodiaId)> VinculosAtuais(Guid[] ativos)
    {
        var vinculos = from item in db.CustodyItems
                       join custodia in db.Custodies on item.CustodyId equals custodia.Id
                       join colaborador in db.Employees on custodia.EmployeeId equals colaborador.Id
                       where item.OrganizationId == OrgId && item.ReturnedAt == null && ativos.Contains(item.AssetId)
                       select new { item.AssetId, colaborador.FullName, item.CustodyId };
        return vinculos.ToDictionary(x => x.AssetId, x => (x.FullName, x.CustodyId));
    }

    private static AssetDto ComResponsavel(AssetDto ativo, Dictionary<Guid, (string Nome, Guid CustodiaId)> vinculos)
        => vinculos.TryGetValue(ativo.Id, out var vinculo)
            ? ativo with { Responsavel = vinculo.Nome, CustodiaAtualId = vinculo.CustodiaId }
            : ativo;

    public Task<AssetDto> GetAssetAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(ComResponsavel(Map(Asset(id)), VinculosAtuais([id])));
    public async Task<AssetDto> CreateAssetAsync(AssetWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("O corpo da requisição é obrigatório.");
        var data = CheckAsset(request);
        if (db.Assets.Any(x => x.OrganizationId == OrgId && x.AssetTag == data.Tag))
            throw new ConflictException("O número de patrimônio já existe nesta organização.");
        var now = Now;
        var asset = new Asset(OrgId, request.CategoryId, data.Tag, data.Name, data.Brand, data.Model,
            data.Serial, data.Notes, now);
        AplicarEspecificacoes(asset, request.Especificacoes);
        switch (request.SituacaoInicial)
        {
            case null or AssetStatus.Available: break;
            case AssetStatus.Maintenance: asset.StartMaintenance(now); break;
            case AssetStatus.Defective: asset.MarkDefective(now); break;
            default: throw new ValidationException("O estado inicial deve ser em estoque, em manutenção ou com defeito.");
        }
        db.Add(asset);
        db.Add(new AssetMovement(OrgId, asset.Id, null, MovementType.Registered, AssetStatus.Available,
            asset.Status, UserId, null, now));
        Audit("Cadastrado", "Ativo", asset.Id, now);
        await db.SaveChangesAsync(ct); return Map(asset);
    }
    public async Task<AssetDto> UpdateAssetAsync(Guid id, AssetWrite request, CancellationToken ct = default)
    {
        if (request is null) throw new ValidationException("O corpo da requisição é obrigatório.");
        var asset = Asset(id); var data = CheckAsset(request);
        byte[] expectedVersion;
        try { expectedVersion = Convert.FromBase64String(Required(request.RowVersion, "versão do registro", 64)); }
        catch (FormatException) { throw new ValidationException("A versão do registro deve estar em Base64."); }
        if (!asset.RowVersion.SequenceEqual(expectedVersion)) throw new ConflictException("O ativo foi alterado por outra operação. Recarregue os dados e tente novamente.");
        if (db.Assets.Any(x => x.OrganizationId == OrgId && x.Id != id && x.AssetTag == data.Tag))
            throw new ConflictException("O número de patrimônio já existe nesta organização.");
        var now = Now;
        try { asset.Update(request.CategoryId, data.Tag, data.Name, data.Brand, data.Model, data.Serial, data.Notes, now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        AplicarEspecificacoes(asset, request.Especificacoes);
        db.Add(new AssetMovement(OrgId, id, null, MovementType.Updated, asset.Status,
            asset.Status, UserId, null, now));
        Audit("Atualizado", "Ativo", id, now);
        await db.SaveChangesAsync(ct); return Map(asset);
    }
    public async Task DeleteAssetAsync(Guid id, CancellationToken ct = default)
    {
        var asset = Asset(id);
        if (!asset.IsActive) return;
        var now = Now; var before = asset.Status;
        try { asset.Deactivate(now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        db.Add(new AssetMovement(OrgId, id, null, MovementType.Deactivated, before, before, UserId, null, now));
        Audit("Inativado", "Ativo", id, now);
        await db.SaveChangesAsync(ct);
    }
    public Task<PagedResult<MovementDto>> GetAssetHistoryAsync(Guid id, PageQuery query, CancellationToken ct = default)
    {
        _ = Asset(id); query = CheckPage(query);
        var source = db.AssetMovements.Where(x => x.OrganizationId == OrgId && x.AssetId == id)
            .OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id);
        return Task.FromResult(Page(source, query, Map));
    }
    public Task<AssetDto> StartMaintenanceAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.MaintenanceStarted, (asset, now) => asset.StartMaintenance(now), ct);
    public Task<AssetDto> CompleteMaintenanceAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.MaintenanceCompleted, (asset, now) => asset.CompleteMaintenance(now), ct);
    public Task<AssetDto> MarkDefectiveAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.MarkedDefective, (asset, now) => asset.MarkDefective(now), ct);
    public Task<AssetDto> DiscardAssetAsync(Guid id, StateChangeRequest request, CancellationToken ct = default)
        => ChangeStateAsync(id, request, MovementType.Discarded, (asset, now) => asset.Discard(now), ct);

    private async Task<AssetDto> ChangeStateAsync(Guid id, StateChangeRequest request, MovementType type,
        Action<Asset, DateTimeOffset> transition, CancellationToken ct)
    {
        var notes = type == MovementType.Discarded
            ? Required(request?.Notes, "motivo do descarte", 500)
            : Optional(request?.Notes, "observações", 2000);
        var asset = Asset(id); var before = asset.Status; var now = Now;
        try { transition(asset, now); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        db.Add(new AssetMovement(OrgId, id, null, type, before, asset.Status, UserId, notes, now));
        Audit(MovementTypeName(type), "Ativo", id, now);
        await db.SaveChangesAsync(ct); return Map(asset);
    }
    private (string Tag, string Name, string Brand, string Model, string? Serial, string? Notes) CheckAsset(AssetWrite request)
    {
        CheckGuid(request.CategoryId, "categoriaId");
        if (!db.AssetCategories.Any(x => x.OrganizationId == OrgId && x.Id == request.CategoryId && x.IsActive))
            throw new ValidationException("A categoria não existe ou está inativa.");
        return (Required(request.AssetTag, "patrimônio", 80), Required(request.Name, "nome", 160),
            Required(request.Brand, "marca", 120), Required(request.Model, "modelo", 120),
            Optional(request.SerialNumber, "número de série", 120), Optional(request.Notes, "observações", 2000));
    }

    private static string MovementTypeName(MovementType type) => type switch
    {
        MovementType.Registered => "Cadastrado",
        MovementType.Updated => "Atualizado",
        MovementType.Assigned => "Entregue",
        MovementType.Returned => "Devolvido",
        MovementType.MaintenanceStarted => "Manutenção iniciada",
        MovementType.MaintenanceCompleted => "Manutenção concluída",
        MovementType.MarkedDefective => "Marcado com defeito",
        MovementType.Discarded => "Descartado",
        MovementType.Deactivated => "Inativado",
        _ => "Movimentado"
    };

    private static void AplicarEspecificacoes(Asset ativo, EspecificacoesAtivo? item)
    {
        if (item is null) return;
        switch (item.Tipo)
        {
            case "Computador":
                ativo.DefinirEspecificacoes(item.Tipo, item.AdquiridoEm, Required(item.Processador, "processador", 120),
                    Required(item.MemoriaRam, "memória RAM", 50), Required(item.Armazenamento, "armazenamento", 80),
                    Required(item.SistemaOperacional, "sistema operacional", 100), null);
                break;
            case "Periférico":
                ativo.DefinirEspecificacoes(item.Tipo, item.AdquiridoEm, null, null, null, null,
                    Required(item.TipoPeriferico, "tipo de periférico", 50));
                break;
            default:
                throw new ValidationException("Informe Computador ou Periférico como tipo do ativo.");
        }
    }
}
