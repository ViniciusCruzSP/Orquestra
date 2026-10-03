using System.Text.Json.Serialization;
using Orquestra.Domain.Enums;

namespace Orquestra.Application.Dtos;

public record PageQuery(int Page = 1, int PageSize = 20, string? Search = null,
    AssetStatus? AssetStatus = null, EmployeeStatus? EmployeeStatus = null);
public record PagedResult<T>(
    [property: JsonPropertyName("itens")] IReadOnlyList<T> Items,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("pagina")] int Page,
    [property: JsonPropertyName("itensPorPagina")] int PageSize);

public record DepartmentWrite([property: JsonPropertyName("nome")] string Name);
public record DepartmentDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("ativo")] bool IsActive,
    [property: JsonPropertyName("criadoEm")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("atualizadoEm")] DateTimeOffset UpdatedAt);

public record EmployeeWrite(
    [property: JsonPropertyName("departamentoId")] Guid? DepartmentId,
    [property: JsonPropertyName("nomeCompleto")] string FullName,
    [property: JsonPropertyName("email")] string Email);
public record EmployeeDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("departamentoId")] Guid? DepartmentId,
    [property: JsonPropertyName("nomeCompleto")] string FullName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("situacao")] EmployeeStatus Status,
    [property: JsonPropertyName("criadoEm")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("atualizadoEm")] DateTimeOffset UpdatedAt);

public record CategoryWrite([property: JsonPropertyName("nome")] string Name);
public record CategoryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("ativa")] bool IsActive,
    [property: JsonPropertyName("criadaEm")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("atualizadaEm")] DateTimeOffset UpdatedAt);

public record AssetWrite(
    [property: JsonPropertyName("categoriaId")] Guid CategoryId,
    [property: JsonPropertyName("patrimonio")] string AssetTag,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("marca")] string Brand,
    [property: JsonPropertyName("modelo")] string Model,
    [property: JsonPropertyName("numeroDeSerie")] string? SerialNumber,
    [property: JsonPropertyName("observacoes")] string? Notes,
    [property: JsonPropertyName("versaoDoRegistro")] string? RowVersion = null);
public record AssetDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("categoriaId")] Guid CategoryId,
    [property: JsonPropertyName("patrimonio")] string AssetTag,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("marca")] string Brand,
    [property: JsonPropertyName("modelo")] string Model,
    [property: JsonPropertyName("numeroDeSerie")] string? SerialNumber,
    [property: JsonPropertyName("observacoes")] string? Notes,
    [property: JsonPropertyName("situacao")] AssetStatus Status,
    [property: JsonPropertyName("ativo")] bool IsActive,
    [property: JsonPropertyName("versaoDoRegistro")] string RowVersion,
    [property: JsonPropertyName("criadoEm")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("atualizadoEm")] DateTimeOffset UpdatedAt);
public record StateChangeRequest([property: JsonPropertyName("observacoes")] string? Notes);
public record MovementDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("ativoId")] Guid AssetId,
    [property: JsonPropertyName("custodiaId")] Guid? CustodyId,
    [property: JsonPropertyName("tipo")] MovementType Type,
    [property: JsonPropertyName("situacaoAnterior")] AssetStatus PreviousStatus,
    [property: JsonPropertyName("novaSituacao")] AssetStatus NewStatus,
    [property: JsonPropertyName("ocorridoEm")] DateTimeOffset OccurredAt,
    [property: JsonPropertyName("realizadoPorUsuarioId")] string PerformedByUserId,
    [property: JsonPropertyName("observacoes")] string? Notes);

public record CustodyCreate(
    [property: JsonPropertyName("colaboradorId")] Guid EmployeeId,
    [property: JsonPropertyName("ativosIds")] IReadOnlyList<Guid> AssetIds,
    [property: JsonPropertyName("observacoes")] string? Notes);
public record CustodyReturnItem(
    [property: JsonPropertyName("ativoId")] Guid AssetId,
    [property: JsonPropertyName("destino")] ReturnDisposition Disposition,
    [property: JsonPropertyName("observacoes")] string? Notes);
public record CustodyReturn([property: JsonPropertyName("itens")] IReadOnlyList<CustodyReturnItem> Items);
public record CustodyItemDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("ativoId")] Guid AssetId,
    [property: JsonPropertyName("entregueEm")] DateTimeOffset AssignedAt,
    [property: JsonPropertyName("devolvidoEm")] DateTimeOffset? ReturnedAt,
    [property: JsonPropertyName("devolvidoPorUsuarioId")] string? ReturnedByUserId,
    [property: JsonPropertyName("observacoesDaDevolucao")] string? ReturnNotes);
public record CustodyDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("colaboradorId")] Guid EmployeeId,
    [property: JsonPropertyName("entregueEm")] DateTimeOffset DeliveredAt,
    [property: JsonPropertyName("entreguePorUsuarioId")] string DeliveredByUserId,
    [property: JsonPropertyName("observacoes")] string? Notes,
    [property: JsonPropertyName("itens")] IReadOnlyList<CustodyItemDto> Items);

public record OffboardingCreate(
    [property: JsonPropertyName("colaboradorId")] Guid EmployeeId,
    [property: JsonPropertyName("observacoes")] string? Notes);
public record OffboardingDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("colaboradorId")] Guid EmployeeId,
    [property: JsonPropertyName("iniciadoEm")] DateTimeOffset StartedAt,
    [property: JsonPropertyName("iniciadoPorUsuarioId")] string StartedByUserId,
    [property: JsonPropertyName("observacoes")] string? Notes,
    [property: JsonPropertyName("concluidoEm")] DateTimeOffset? CompletedAt,
    [property: JsonPropertyName("concluidoPorUsuarioId")] string? CompletedByUserId,
    [property: JsonPropertyName("ativosPendentesIds")] IReadOnlyList<Guid> PendingAssetIds);

public record DashboardDto(
    [property: JsonPropertyName("totalDeAtivos")] int TotalAssets,
    [property: JsonPropertyName("ativosDisponiveis")] int AvailableAssets,
    [property: JsonPropertyName("ativosEmUso")] int AssignedAssets,
    [property: JsonPropertyName("ativosEmManutencao")] int MaintenanceAssets,
    [property: JsonPropertyName("ativosComDefeito")] int DefectiveAssets,
    [property: JsonPropertyName("ativosDescartados")] int DiscardedAssets,
    [property: JsonPropertyName("colaboradoresAtivos")] int ActiveEmployees,
    [property: JsonPropertyName("desligamentosAbertos")] int OpenOffboardings,
    [property: JsonPropertyName("itensPendentesDeDevolucao")] int PendingCustodyItems);
public record AuditLogDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("acao")] string Action,
    [property: JsonPropertyName("tipoDeEntidade")] string EntityType,
    [property: JsonPropertyName("entidadeId")] Guid EntityId,
    [property: JsonPropertyName("usuarioId")] string UserId,
    [property: JsonPropertyName("ocorridoEm")] DateTimeOffset OccurredAt,
    [property: JsonPropertyName("detalhes")] string? Details);
