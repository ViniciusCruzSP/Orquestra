namespace Orquestra.Application.Abstractions;

public record EquipamentoEntregue(string Patrimonio, string Nome, string Marca, string Modelo, string? Serie);

public record DadosTermo(Guid CustodiaId, string Organizacao, string Colaborador, string Email,
    string? Departamento, DateTimeOffset EntregueEm, IReadOnlyList<EquipamentoEntregue> Equipamentos);

public interface IGeradorTermo
{
    byte[] Gerar(DadosTermo dados);
}
