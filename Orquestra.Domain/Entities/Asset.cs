using Orquestra.Domain.Enums;

namespace Orquestra.Domain.Entities;

public sealed class Asset
{
    private Asset() { }
    public Asset(Guid organizationId, Guid categoryId, string assetTag, string name, string brand, string model,
        string? serialNumber, string? notes, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); OrganizationId = organizationId; CategoryId = categoryId; AssetTag = assetTag;
        Name = name; Brand = brand; Model = model; SerialNumber = serialNumber; Notes = notes;
        Status = AssetStatus.Available; IsActive = true; CreatedAt = UpdatedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string AssetTag { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string? SerialNumber { get; private set; }
    public string? Notes { get; private set; }
    public AssetStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? Tipo { get; private set; }
    public DateOnly? AdquiridoEm { get; private set; }
    public string? Processador { get; private set; }
    public string? MemoriaRam { get; private set; }
    public string? Armazenamento { get; private set; }
    public string? SistemaOperacional { get; private set; }
    public string? TipoPeriferico { get; private set; }

    public void DefinirEspecificacoes(string tipo, DateOnly? adquiridoEm, string? processador, string? memoriaRam,
        string? armazenamento, string? sistemaOperacional, string? tipoPeriferico)
    {
        Tipo = tipo;
        AdquiridoEm = adquiridoEm;
        Processador = processador;
        MemoriaRam = memoriaRam;
        Armazenamento = armazenamento;
        SistemaOperacional = sistemaOperacional;
        TipoPeriferico = tipoPeriferico;
    }
    public void Update(Guid categoryId, string assetTag, string name, string brand, string model,
        string? serialNumber, string? notes, DateTimeOffset now)
    {
        if (!IsActive || Status == AssetStatus.Discarded) throw new InvalidOperationException("O ativo não pode ser editado.");
        CategoryId = categoryId; AssetTag = assetTag; Name = name; Brand = brand; Model = model;
        SerialNumber = serialNumber; Notes = notes; UpdatedAt = now;
    }
    public void Assign(DateTimeOffset now)
    {
        if (!IsActive || Status != AssetStatus.Available) throw new InvalidOperationException("Somente ativos disponíveis podem ser entregues.");
        Status = AssetStatus.Assigned; UpdatedAt = now;
    }
    public void Return(ReturnDisposition disposition, DateTimeOffset now)
    {
        if (Status != AssetStatus.Assigned) throw new InvalidOperationException("O ativo não está vinculado a um colaborador.");
        Status = disposition switch
        {
            ReturnDisposition.Available => AssetStatus.Available,
            ReturnDisposition.Maintenance => AssetStatus.Maintenance,
            ReturnDisposition.Defective => AssetStatus.Defective,
            _ => throw new ArgumentOutOfRangeException(nameof(disposition))
        };
        UpdatedAt = now;
    }
    public void StartMaintenance(DateTimeOffset now)
    {
        if (!IsActive || Status is not (AssetStatus.Available or AssetStatus.Defective))
            throw new InvalidOperationException("Somente ativos disponíveis ou com defeito podem entrar em manutenção.");
        Status = AssetStatus.Maintenance; UpdatedAt = now;
    }
    public void CompleteMaintenance(DateTimeOffset now)
    {
        if (Status != AssetStatus.Maintenance) throw new InvalidOperationException("O ativo não está em manutenção.");
        Status = AssetStatus.Available; UpdatedAt = now;
    }
    public void MarkDefective(DateTimeOffset now)
    {
        if (!IsActive || Status is not (AssetStatus.Available or AssetStatus.Maintenance))
            throw new InvalidOperationException("O ativo não pode ser marcado com defeito nessa situação.");
        Status = AssetStatus.Defective; UpdatedAt = now;
    }
    public void Discard(DateTimeOffset now)
    {
        if (!IsActive || Status is not (AssetStatus.Available or AssetStatus.Defective))
            throw new InvalidOperationException("O ativo não pode ser descartado nessa situação.");
        Status = AssetStatus.Discarded; UpdatedAt = now;
    }
    public void Deactivate(DateTimeOffset now)
    {
        if (Status is AssetStatus.Assigned or AssetStatus.Maintenance) throw new InvalidOperationException("O ativo possui uma operação em andamento.");
        IsActive = false; UpdatedAt = now;
    }
}
