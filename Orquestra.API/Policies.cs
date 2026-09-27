namespace Orquestra.Api;

public static class Policies
{
    public const string ManageRecords = nameof(ManageRecords);
    public const string ManageCritical = nameof(ManageCritical);
    public const string Audit = nameof(Audit);
}

public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string GestorTI = nameof(GestorTI);
    public const string OperadorTI = nameof(OperadorTI);
    public const string Auditor = nameof(Auditor);
}
