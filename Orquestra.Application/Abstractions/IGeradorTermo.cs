namespace Orquestra.Application.Abstractions;

public record EquipamentoEntregue(string Patrimonio, string Nome, string Marca, string Modelo, string? Serie, string? Tipo = null);

public record DadosTermo(Guid CustodiaId, string Organizacao, string Colaborador, string Email,
    string? Departamento, DateTimeOffset EntregueEm, IReadOnlyList<EquipamentoEntregue> Equipamentos,
    string? CpfMascarado = null, string? Cargo = null);

public interface IGeradorTermo
{
    byte[] Gerar(DadosTermo dados);
}
