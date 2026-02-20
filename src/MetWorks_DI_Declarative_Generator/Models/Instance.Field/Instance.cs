namespace MetWorks.DI.Declarative.Generator.Models.Instance.Field;
public record Instance : InstanceBase
{
    public required string ClassQualified { get; init; }
    public required bool IsArray { get; init; }
}
