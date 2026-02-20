namespace MetWorks.DI.Declarative.Diagnostics;
public sealed record CoercionEntry(
    string ClrTypeName,
    string CSharpAlias,
    Func<string, object> Parser,
    Func<string> DefaultExpression,
    Func<string, string> RenderLiteral
);

public static class AssignmentCoercion
{
    public static readonly List<CoercionEntry> CanonicalCoercions = new()
        {
            new("System.String", "string",
                v => v,
                () => "string.Empty",
                v => $"\"{v}\""),

            new("System.Int32", "int",
                v => int.Parse(v, NumberStyles.Integer, CultureInfo.InvariantCulture),
                () => "0",
                v => v),

            new("System.Double", "double",
                v => double.Parse(v, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture),
                () => "0d",
                v => v),

            new("System.Boolean", "bool",
                v => bool.Parse(v),
                () => "false",
                v => v.ToLowerInvariant()),

            new("System.Char", "char",
                v => v.Length == 1 ? v[0] : throw new FormatException($"Cannot coerce '{v}' to char."),
                () => "'\\0'",
                v => $"'{v}'"),

            new("System.Decimal", "decimal",
                v => decimal.Parse(v, NumberStyles.Number, CultureInfo.InvariantCulture),
                () => "0m",
                v => v + "m"),

            new("System.Guid", "guid",
                v => Guid.Parse(v),
                () => "System.Guid.Empty",
                v => $"System.Guid.Parse(\"{v}\")"),

            new("System.DateTime", "datetime",
                v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                () => "default(System.DateTime)",
                v => $"System.DateTime.Parse(\"{v}\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)")
        };

    public static string? InitForType(string qualifiedClassName, bool isArray)
    {
        if (isArray)
            return $"new {qualifiedClassName}[] {{ }}";

        var entry = CanonicalCoercions
            .FirstOrDefault(c => c.ClrTypeName == qualifiedClassName || c.CSharpAlias == qualifiedClassName);

        return entry != null
            ? entry.DefaultExpression()
            : $"new {qualifiedClassName}()";
    }
    public static string? RenderLiteral(string qualifiedClassName, string value)
    {
        // Special case: YAML "[]" means empty array literal, not the string "[]"
        if (value == "[]")
        {
            // Use Array.Empty<T>() for safety and clarity
            return $"Array.Empty<{qualifiedClassName}>()";
        }

        var entry = CanonicalCoercions
            .FirstOrDefault(c => c.ClrTypeName == qualifiedClassName || c.CSharpAlias == qualifiedClassName);

        return entry != null
            ? entry.RenderLiteral(value)
            : value; // fallback
    }
}
