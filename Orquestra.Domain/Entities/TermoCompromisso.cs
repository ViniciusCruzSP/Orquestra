namespace Orquestra.Domain.Entities;

public sealed class TermoCompromisso
{
    private TermoCompromisso() { }

    public TermoCompromisso(Guid organizacaoId, Guid custodiaId, DateTimeOffset geradoEm, byte[] pdf)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizacaoId;
        CustodiaId = custodiaId;
        GeradoEm = geradoEm;
        Pdf = pdf;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CustodiaId { get; private set; }
    public DateTimeOffset GeradoEm { get; private set; }
    public byte[] Pdf { get; private set; } = [];
}
