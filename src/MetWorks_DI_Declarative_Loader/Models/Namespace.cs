namespace MetWorks.DI.Declarative.Syntax.Models;
public sealed record Namespace : BaseDto
{
    public string NamespaceName { get; }
    public IReadOnlyList<Interface> Interfaces { get; }
    public IReadOnlyList<Class> Classes { get; }

    public Namespace(
        string namespaceName,
        IReadOnlyList<Interface>? interfaces,
        IReadOnlyList<Class>? classes,
        Location? location = null,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        NamespaceName = namespaceName;
        Interfaces = interfaces ?? new List<Interface>();
        Classes = classes ?? new List<Class>();
    }
}
