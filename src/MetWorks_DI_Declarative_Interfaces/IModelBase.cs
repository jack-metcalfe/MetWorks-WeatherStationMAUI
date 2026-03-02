namespace MetWorks.DI.Declarative.Interfaces;
public interface IModelBase
{
    string TemplateRequested { get; init; }
    string Namespace { get; init; }
    string ContainerClass { get; init; }
}