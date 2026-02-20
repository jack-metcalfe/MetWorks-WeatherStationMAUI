namespace MetWorks.DI.Declarative.Generator.Models.Elements.Initializer;
public record Instance : InstanceBase
{
    public required string ClassQualified { get; init; }
    public required bool IsArray { get; init; }
    public string? ElementsConstructionExpression { get; init; }
}