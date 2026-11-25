using Orquestra.Domain.Entities;
using Orquestra.Domain.Enums;

namespace Orquestra.Testes;

public sealed class AtivoTestes
{
    private static readonly DateTimeOffset Cadastro = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ReturnDisposition.Available, AssetStatus.Available)]
    [InlineData(ReturnDisposition.Maintenance, AssetStatus.Maintenance)]
    [InlineData(ReturnDisposition.Defective, AssetStatus.Defective)]
    public void DevolucaoMudaSituacaoConformeDestino(ReturnDisposition destino, AssetStatus esperado)
    {
        var ativo = NovoAtivo();
        ativo.Assign(Cadastro.AddHours(1));
        var devolucao = Cadastro.AddHours(2);

        ativo.Return(destino, devolucao);

        Assert.Equal(esperado, ativo.Status);
        Assert.Equal(devolucao, ativo.UpdatedAt);
    }

    [Fact]
    public void AtivoEntregueNaoPodeSerEntregueMantidoDescartadoOuInativado()
    {
        var ativo = NovoAtivo();
        var entrega = Cadastro.AddHours(1);
        ativo.Assign(entrega);
        var tentativa = Cadastro.AddHours(2);

        Assert.Throws<InvalidOperationException>(() => ativo.Assign(tentativa));
        Assert.Throws<InvalidOperationException>(() => ativo.StartMaintenance(tentativa));
        Assert.Throws<InvalidOperationException>(() => ativo.Discard(tentativa));
        Assert.Throws<InvalidOperationException>(() => ativo.Deactivate(tentativa));
        Assert.Equal(AssetStatus.Assigned, ativo.Status);
        Assert.Equal(entrega, ativo.UpdatedAt);
        Assert.True(ativo.IsActive);
    }

    [Fact]
    public void DevolverAtivoSemEntregaPreservaSituacao()
    {
        var ativo = NovoAtivo();
        Assert.Throws<InvalidOperationException>(() => ativo.Return(ReturnDisposition.Defective, Cadastro.AddHours(1)));
        Assert.Equal(AssetStatus.Available, ativo.Status);
        Assert.Equal(Cadastro, ativo.UpdatedAt);
    }

    [Fact]
    public void AtivoEmManutencaoSoPodeSerEntregueAposConclusao()
    {
        var ativo = NovoAtivo();
        ativo.StartMaintenance(Cadastro.AddHours(1));
        Assert.Throws<InvalidOperationException>(() => ativo.Assign(Cadastro.AddHours(2)));
        ativo.CompleteMaintenance(Cadastro.AddHours(3));
        ativo.Assign(Cadastro.AddHours(4));
        Assert.Equal(AssetStatus.Assigned, ativo.Status);
    }

    private static Asset NovoAtivo() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "PAT-100", "Notebook", "Marca", "Modelo", null, null, Cadastro);
}
