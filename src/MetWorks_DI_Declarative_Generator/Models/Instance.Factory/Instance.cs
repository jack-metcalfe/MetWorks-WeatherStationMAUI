namespace MetWorks.DI.Declarative.Generator.Models.Instance.Factory;
public record Instance : InstanceBase
{
    public required string ClassQualified { get; init; }
    public required bool IsArray { get; init; }
    public required bool HasElements { get; init; }
    public string? FactoryInstanceName { get; init; }
    public string? FactoryMethodName { get; init; }
    public bool HasFactoryMethodBinding => !string.IsNullOrWhiteSpace(FactoryInstanceName) && !string.IsNullOrWhiteSpace(FactoryMethodName);
}
