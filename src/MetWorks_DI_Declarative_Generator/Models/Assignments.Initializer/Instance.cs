namespace MetWorks.DI.Declarative.Generator.Models.Assignments.Initializer;
public record Instance : InstanceBase
{
    public required bool HasAssignments { get; init; }
    public required List<Assignment> Assignments { get; init; }
    public required List<string> InitializationDependencies { get; init; }
}