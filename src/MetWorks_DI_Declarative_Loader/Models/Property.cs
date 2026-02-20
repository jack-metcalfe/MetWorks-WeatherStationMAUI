namespace MetWorks.DI.Declarative.Syntax.Models;
public sealed record Property : BaseDto
{
    public string? PropertyName { get; }
    public string? PropertyQualified { get; }
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
    public Property(
        string? namespaceName,
        string? className,
        string? propertyName,
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
        PropertyName = propertyName;
        PropertyQualified = $"{namespaceName}.{className}.{propertyName}";
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
