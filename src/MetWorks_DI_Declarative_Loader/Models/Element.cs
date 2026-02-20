namespace MetWorks.DI.Declarative.Syntax.Models;
// Optional collection element shape (use only if you need explicit element entries)
public sealed record Element : BaseDto
{
    public string? ClassQualified {get;}
    public string? ConstructionExpression {get; }
    public string? Literal { get; }
    public bool isLiteral => Literal is not null;
    public string? LiteralInferredClass { get; }
    public string? Instance { get; }
    public bool isInstance => Instance is not null;
    public string? ElementInitializerClause { get; }

    public Element(
        string? literal,
        string? instance,
        Class? @instanceClass,
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        Literal = literal;
        Instance = instance;
        ClassQualified = @instanceClass?.ClassQualified;
        ConstructionExpression = AssignmentCoercion.InitForType(ClassQualified!, isArray: false);
        diagnostics = diagnostics ?? Array.Empty<Diagnostic>();
        LiteralInferredClass = isLiteral ? literal.InferredClass() : null;
        if (diagnostics.Count == 0)
        {
            if (isLiteral)
            {
                if (LiteralInferredClass!.Equals("String")) literal = $"\"{literal}\"";
                ElementInitializerClause = literal;
            }
            else if (isInstance)
                // ToDo: Kludge: "registry.Get and _Internal" should be handled
                // in CodeGenerator or TemplateRenderer instead of here because
                // this these text snippets are specific to code generation.
                ElementInitializerClause = $"registry.Get{instance}_Internal()";
        }
    }
}
