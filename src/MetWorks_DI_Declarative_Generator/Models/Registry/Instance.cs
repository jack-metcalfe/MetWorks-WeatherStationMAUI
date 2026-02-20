namespace MetWorks.DI.Declarative.Generator.Models.Registry;
public record Instance : InstanceBase
{
    public required bool HasAssignments { get; init; }
    public required bool HasDisposable { get; init; }
}
