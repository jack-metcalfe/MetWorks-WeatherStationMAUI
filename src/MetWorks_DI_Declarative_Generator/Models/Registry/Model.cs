namespace MetWorks.DI.Declarative.Generator.Models.Registry;

public record Model : ModelBase
{
    public required List<Instance> Instances { get; init; }
}
