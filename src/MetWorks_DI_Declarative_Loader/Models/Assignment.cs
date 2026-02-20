namespace MetWorks.DI.Declarative.Syntax.Models;
// Assignment nested under a named instance binds a parameter on that named instance's class
public sealed record Assignment : BaseDto
{
    public string? Name { get; }
    public string? Literal { get; }
    public string? Instance { get; }
    public bool IsInstance => Instance is not null;
    public string? InstancePropertyPath { get; }
    public string? InitializerParameterAssignmentClause { get; }
    public string? LiteralInferredClass { get; }
    public string? ParameterClassQualified {get; }
    public string? ParameterClassInterfaceQualified { get; }
    public string? ParameterType => ParameterClassInterfaceQualified ?? ParameterClassQualified;
    public Assignment(
        string? name,
        string? literal,
        string? literalInferredClass,
        string? instance,
        string? instancePropertyPath,
        string? initializerParameterAssignmentClause,
        Parameter parameter,
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics
    ) : base(location, diagnostics)
    {
        Name = name;
        Literal = literal;
        LiteralInferredClass = literalInferredClass;
        Instance = instance;
        InstancePropertyPath = instancePropertyPath;
        InitializerParameterAssignmentClause = initializerParameterAssignmentClause;
        ParameterClassQualified = parameter.ClassQualified;
        ParameterClassInterfaceQualified = parameter.InterfaceQualified;
    }
}
