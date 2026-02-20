namespace MetWorks.DI.Declarative.Generator.Models.Instance.Factory;
public record Model : ModelBase, IModelSingleInstance
{
    public required Instance Instance { get; init; }
    public string InstanceName => Instance.Name;
}