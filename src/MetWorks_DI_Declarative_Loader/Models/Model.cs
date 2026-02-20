namespace MetWorks.DI.Declarative.Syntax.Models;
public sealed record Model : BaseDto
{
    public CodeGen? CodeGen { get; }
    public string ModelParsedWhenString { get; } = DateTime.UtcNow.ToString("o");
    public IReadOnlyList<Namespace> Namespaces { get; }
    public IReadOnlyList<Instance> Instances { get; }
    public Dictionary<string, Namespace> NamespaceDictionary { get; } = new();
    public Dictionary<string, Interface> InterfaceDictionary { get; } = new();
    public Dictionary<string, Class> ClassDictionary { get; } = new();
    public Dictionary<string, Parameter> ParameterDictionary { get; } = new();
    public Dictionary<string, Instance> InstanceDictionary { get; } = new();
    public Model(
        CodeGen? codeGen = null,
        IReadOnlyList<Namespace>? namespaces = null,
        IReadOnlyList<Instance>? instances = null,
        Dictionary<string, Namespace>? namespaceDictionary = null,
        Dictionary<string, Interface>? interfaceDictionary = null,
        Dictionary<string, Class>? classDictionary = null,
        Dictionary<string, Parameter>? parameterDictionary = null,
        Dictionary<string, Instance>? instanceDictionary = null,
        Location? location = null,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        CodeGen = codeGen;
        Namespaces = namespaces ?? new List<Namespace>();
        Instances = instances ?? new List<Instance>();
        NamespaceDictionary = namespaceDictionary ?? new Dictionary<string, Namespace>();
        InterfaceDictionary = interfaceDictionary ?? new Dictionary<string, Interface>();
        ClassDictionary = classDictionary ?? new Dictionary<string, Class>();
        ParameterDictionary = parameterDictionary ?? new Dictionary<string, Parameter>();
        InstanceDictionary = instanceDictionary ?? new Dictionary<string, Instance>();
    }
}
