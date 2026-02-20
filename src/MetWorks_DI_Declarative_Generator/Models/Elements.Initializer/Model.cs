namespace MetWorks.DI.Declarative.Generator.Models.Elements.Initializer;
public record Model : ModelBase, IModelSingleInstance
{
    public string InstanceName => Instance.Name;
    public required Instance Instance { get; init; }
}