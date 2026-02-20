namespace MetWorks.DI.Declarative.Generator.Models.Accessors;
public record Instance : InstanceBase
{
    public required string ClassQualified { get; init; }
    public string? InterfaceQualified { get; init; }
    public required bool IsArray { get; init; }
    public required bool HasAssignments { get; init; }
}