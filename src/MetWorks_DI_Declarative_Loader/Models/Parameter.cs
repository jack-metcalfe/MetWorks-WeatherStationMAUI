namespace MetWorks.DI.Declarative.Syntax.Models;
public sealed record Parameter : BaseDto
{
    public string? ParameterName { get; }
    public string? ParameterQualified { get; }
    public string? ClassToken{ get; }
    public string? Class { get; }
    public string? ClassQualified { get; }
    public string? InterfaceToken{ get; }
    public string? Interface { get; }
    public string? InterfaceQualified { get; }
    public bool? IsInterface => Interface is not null;
    public bool IsArray { get; }
    public bool IsNullable { get; }
    public bool? IsElementNullable { get; }
    public Parameter(
        string? namespaceName,
        string? className,
        string? parameterName,
        string? classToken,
        string? @class,
        string? classQualified,
        string? interfaceToken, 
        string? @interface,
        string? interfaceQualified,
        bool? isArray,
        bool? isNullable,
        bool? isElementNullable,
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        ParameterName = parameterName;
        ParameterQualified = $"{namespaceName}.{className}.{parameterName}";
        ClassToken = classToken;
        Class = @class;
        ClassQualified = classQualified;
        InterfaceToken = interfaceToken;
        Interface = @interface;
        InterfaceQualified = interfaceQualified;
        IsArray = isArray ?? false;
        IsNullable = isNullable ?? false;
        IsElementNullable = isElementNullable;
    }
}
