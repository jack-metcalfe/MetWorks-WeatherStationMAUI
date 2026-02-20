namespace MetWorks.DI.Declarative.Generator.Models.ExposeToMauiDi;
public record Instance : InstanceBase
{
    public required string ClassQualified { get; init; }
    public string? InterfaceQualified { get; init; }
    public required bool IsArray { get; init; }
    public required string ServiceTypeQualified { get; init; }
    public required string ResolveExpression { get; init; }
    public required bool UseNonGenericServiceRegistration { get; init; }
}
