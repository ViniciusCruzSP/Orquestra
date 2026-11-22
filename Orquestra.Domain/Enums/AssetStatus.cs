using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Orquestra.Domain.Enums;

[TypeConverter(typeof(AssetStatusTypeConverter))]
public enum AssetStatus
{
    [JsonStringEnumMemberName("Disponível")] Available = 0,
    [JsonStringEnumMemberName("EmUso")] Assigned = 1,
    [JsonStringEnumMemberName("EmManutenção")] Maintenance = 2,
    [JsonStringEnumMemberName("ComDefeito")] Defective = 3,
    [JsonStringEnumMemberName("Descartado")] Discarded = 4
}
[TypeConverter(typeof(EmployeeStatusTypeConverter))]
public enum EmployeeStatus
{
    [JsonStringEnumMemberName("Ativo")] Active = 0,
    [JsonStringEnumMemberName("EmDesligamento")] Offboarding = 1,
    [JsonStringEnumMemberName("Inativo")] Inactive = 2
}
public enum MovementType
{
    [JsonStringEnumMemberName("Cadastrado")] Registered = 0,
    [JsonStringEnumMemberName("Atualizado")] Updated = 1,
    [JsonStringEnumMemberName("Entregue")] Assigned = 2,
    [JsonStringEnumMemberName("Devolvido")] Returned = 3,
    [JsonStringEnumMemberName("ManutençãoIniciada")] MaintenanceStarted = 4,
    [JsonStringEnumMemberName("ManutençãoConcluída")] MaintenanceCompleted = 5,
    [JsonStringEnumMemberName("MarcadoComDefeito")] MarkedDefective = 6,
    [JsonStringEnumMemberName("Descartado")] Discarded = 7,
    [JsonStringEnumMemberName("Inativado")] Deactivated = 8
}
public enum ReturnDisposition
{
    [JsonStringEnumMemberName("Disponível")] Available = 0,
    [JsonStringEnumMemberName("EmManutenção")] Maintenance = 1,
    [JsonStringEnumMemberName("ComDefeito")] Defective = 2
}

internal sealed class AssetStatusTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text)
        {
            return text.Trim().ToLowerInvariant() switch
            {
                "disponível" or "disponivel" => AssetStatus.Available,
                "emuso" or "em uso" => AssetStatus.Assigned,
                "emmanutenção" or "emmanutencao" or "em manutenção" or "em manutencao" => AssetStatus.Maintenance,
                "comdefeito" or "com defeito" => AssetStatus.Defective,
                "descartado" => AssetStatus.Discarded,
                _ => throw new FormatException("A situação do ativo informada é inválida.")
            };
        }
        return base.ConvertFrom(context, culture, value);
    }
}

internal sealed class EmployeeStatusTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text)
        {
            return text.Trim().ToLowerInvariant() switch
            {
                "ativo" => EmployeeStatus.Active,
                "emdesligamento" or "em desligamento" => EmployeeStatus.Offboarding,
                "inativo" => EmployeeStatus.Inactive,
                _ => throw new FormatException("A situação do colaborador informada é inválida.")
            };
        }
        return base.ConvertFrom(context, culture, value);
    }
}
