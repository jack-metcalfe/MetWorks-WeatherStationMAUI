namespace MetWorks.DI.Declarative.Syntax.Models;
public sealed record CodeGen : BaseDto
{
    public string? RegistryClass { get; }
    public string? CodePath { get; }
    public string? Namespace { get; }
    public string? Initializer { get; }
    public CodeGen(
        string? registryClass,
        string? codePath,
        string? @namespace,
        string? initializer,
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        RegistryClass = registryClass;
        CodePath = codePath;
        Namespace = @namespace;
        Initializer = initializer;
    }
}
