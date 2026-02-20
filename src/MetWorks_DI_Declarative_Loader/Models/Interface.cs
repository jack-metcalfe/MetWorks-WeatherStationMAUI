namespace MetWorks.DI.Declarative.Syntax.Models;
public sealed record Interface : BaseDto
{
    public string? InterfaceName { get; }
    public string? InterfaceQualified {get;}

    public Interface(
        string namespaceName,
        string? interfaceName,
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        InterfaceName = interfaceName;
        InterfaceQualified = interfaceName is null ? null : $"{namespaceName}.{interfaceName}"; 
    }
}
