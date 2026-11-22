namespace Orquestra.Api;

public static class Policies
{
    public const string ManageRecords = nameof(ManageRecords);
    public const string ManageCritical = nameof(ManageCritical);
    public const string Audit = nameof(Audit);
    public const string Sessao = nameof(Sessao);
    public const string Colaborador = nameof(Colaborador);
}

public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string GestorTI = nameof(GestorTI);
    public const string OperadorTI = nameof(OperadorTI);
    public const string Auditor = nameof(Auditor);
    public const string Colaborador = nameof(Colaborador);
}
