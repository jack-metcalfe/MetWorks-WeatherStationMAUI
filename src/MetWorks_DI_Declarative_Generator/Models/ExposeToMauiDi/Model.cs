namespace MetWorks.DI.Declarative.Generator.Models.ExposeToMauiDi;
public record Model : ModelBase
{
    public required List<Instance> Instances { get; init; }
}

