namespace MetWorks.DI.Declarative.Syntax;
/// <summary>
/// Provides string validation extensions for identifiers, qualified names, lifetimes, and provenance.
/// </summary>
public static class StringExtensions
{   
    // -----------------------------
    // Identifier Validations
    // -----------------------------

    /// <summary>
    /// Returns true if the string is a valid C# identifier.
    /// </summary>
    public static bool IsValidIdentifier(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        
        // Must start with letter or underscore
        if (!(char.IsLetter(value![0]) || value[0] == '_'))
            return false;

        // Remaining chars must be letters, digits, or underscore
        if (value.Skip(1).Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_')))
            return false;

        // Cannot be a C# keyword
        if (!IsKeyword(value))
            return true;
        else
            return false;
    }

    /// <summary>
    /// Returns true if the string is a valid dotted identifier path where each segment is a valid C# identifier.
    /// Examples:
    /// - "SomeClass"
    /// - "SomeClass.Person.Name"
    /// - "SomeClass.Person.Name?"
    /// Notes:
    /// - Allows an optional trailing '?' (treated as a nullable marker).
    /// - Does NOT allow interior '?' (e.g., "A?.B") or empty segments (e.g., "A..B").
    /// </summary>
    public static bool IsValidDottedIdentifier(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var s = value.Trim();

        // Allow a single trailing nullable marker: "A.B.C?"
        if (s.EndsWith("?", StringComparison.Ordinal))
        {
            s = s.Substring(0, s.Length - 1).TrimEnd();
            if (string.IsNullOrWhiteSpace(s)) return false;
        }

        var parts = s.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        // Reject "A..B" or ".A" or "A."
        // (RemoveEmptyEntries would hide empties, so explicitly check original shape)
        if (s.StartsWith(".", StringComparison.Ordinal) ||
            s.EndsWith(".", StringComparison.Ordinal) ||
            s.Contains("..", StringComparison.Ordinal))
            return false;

        return parts.All(p => p.IsValidIdentifier());
    }

    /// <summary>
    /// Returns true if the string follows PascalCase convention.
    /// </summary>
    public static bool IsPascalCase(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return char.IsUpper(value![0]) && !value.Contains(' ');
    }

    private static bool IsKeyword(string value)
    {
        // Use CodeDom provider to detect keywords; CreateProvider may be expensive so keep short-lived.
        using var provider = CodeDomProvider.CreateProvider("CSharp");
        // provider.IsValidIdentifier returns false for keywords and invalid identifiers.
        // We already validated identifier shape, so a false here indicates a keyword or provider-specific invalidity.
        return !provider.IsValidIdentifier(value);
    }

    /// <summary>
    /// Values are unquoted so if nothing else is a valid string
    /// </summary>
    public static string InferredClass(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return typeof(string).Name;

        var s = value!.Trim();

        // Check for boolean literal
        if (s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("false", StringComparison.OrdinalIgnoreCase))
            return typeof(bool).Name;

        // Try numeric types: int, long, double, float, decimal
        if (int.TryParse(s, out _)) return typeof(int).Name;
        if (long.TryParse(s, out _)) return typeof(long).Name;
        if (double.TryParse(s, out _)) return typeof(double).Name;
        if (float.TryParse(s, out _)) return typeof(float).Name;
        if (decimal.TryParse(s, out _)) return typeof(decimal).Name;

        return typeof(string).Name;
    }

    // -----------------------------
    // Qualified Name Validations
    // -----------------------------

    /// <summary>
    /// Returns true if the string is a valid qualified name (Namespace.ClassName).
    /// Each segment must be a valid identifier and there must be at least two segments.
    /// </summary>
    public static bool IsQualifiedName(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var parts = value!.Split('.');
        if (parts.Length < 2) return false;

        // Each part must be a valid identifier
        return parts.All(p => p.IsValidIdentifier());
    }

    /// <summary>
    /// Returns true if the string is a valid interface name (starts with 'I' and followed by uppercase).
    /// </summary>
    public static bool IsInterfaceName(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value!.Length > 1 && value[0] == 'I' && char.IsUpper(value[1]) && value.IsValidIdentifier();
    }

    // -----------------------------
    // Namespace helpers
    // -----------------------------
    /// <summary>
    /// Returns the short name (last segment) of a qualified name.
    /// If the input has no dot, returns the original string (or empty if null/whitespace).
    /// </summary>
    public static string ExtractShortName(this string? qualifiedName)
    {
        if (string.IsNullOrWhiteSpace(qualifiedName)) return string.Empty;
        var parts = qualifiedName!.Split('.');
        return parts.Length == 0 ? string.Empty : parts[^1];
    }

    /// <summary>
    /// Left is all but last dotted section and Right is the last dotted section or string.empty
    /// if no dot present
    /// </summary>
    public static (string LeftPart, string RightPart) SplitQualifiedName(this string? qualifiedName)
    {
        if (string.IsNullOrWhiteSpace(qualifiedName))
            return (string.Empty, string.Empty);
        var parts = qualifiedName!.Split('.');
        if (parts.Length == 0)
            return (string.Empty, string.Empty);
        if (parts.Length == 1)
            return (parts[0],string.Empty);
        var rightPart = parts[^1];
        var leftPart = string.Join('.', parts[..^1]);
        return (leftPart, rightPart);
    }

    /// <summary>
    /// Returns true if every segment of the qualified namespace is a valid identifier.
    /// Does not require there to be more than one segment; use IsQualifiedName when you need at least two segments.
    /// </summary>
    public static bool IsValidNamespace(this string? namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName)) return false;
        var parts = namespaceName!.Split('.');
        if (parts.Length == 0) return false;
        return parts.All(p => p.IsValidIdentifier());
    }

    // -----------------------------
    // Non-nullability & Uniqueness
    // -----------------------------

    // -----------------------------
    // Type token helpers (array/nullable modifiers)
    // -----------------------------

    /// <summary>
    /// Parse a type reference token into its base qualified name and modifiers.
    /// Supported forms:
    ///   - Ns.Type        -> (BaseQualifiedName="Ns.Type", IsArray=false, IsContainerNullable=false, IsElementNullable=false)
    ///   - Ns.Type?       -> (BaseQualifiedName="Ns.Type", IsArray=false, IsContainerNullable=true,  IsElementNullable=false)
    ///   - Ns.Type[]      -> (BaseQualifiedName="Ns.Type", IsArray=true,  IsContainerNullable=false, IsElementNullable=false)
    ///   - Ns.Type[]?     -> (BaseQualifiedName="Ns.Type", IsArray=true,  IsContainerNullable=true,  IsElementNullable=false)
    /// The form Ns.Type?[] (nullable element inside array) is intentionally disallowed and treated as invalid
    /// (the method returns an empty BaseQualifiedName to signal parse failure).
    /// </summary>
    public static (
            string QualifiedName,
            bool IsArray, 
            bool IsContainerNullable, 
            bool IsElementNullable
    ) 
    ParseTypeRef(this string? token)
    {
        bool isArray = false, isContainerNullable = false, isElementNullable = false;

        var emptyReturn = (string.Empty, isArray, isContainerNullable, isElementNullable);

        if (token is null) return emptyReturn;

        var s = token.Trim();
        if (string.IsNullOrWhiteSpace(s)) return emptyReturn;

        // 1) Array + container-nullable: Foo[]?
        if (s.EndsWith("[]?", StringComparison.Ordinal))
        {
            isArray = true;
            isContainerNullable = true;
            s = s.Substring(0, s.Length - 3).TrimEnd();
            return (s, isArray, isContainerNullable, isElementNullable);
        }

        // 2) Array: Foo[]
        if (s.EndsWith("[]", StringComparison.Ordinal))
        {
            isArray = true;
            s = s.Substring(0, s.Length - 2).TrimEnd();

            // Disallow Foo?[] (nullable element inside array) — treat as invalid
            if (s.EndsWith("?", StringComparison.Ordinal))
            {
                return (string.Empty, false, false, false);
            }

            return (s, isArray, isContainerNullable, isElementNullable);
        }

        // 3) Non-array container nullable: Foo?
        if (s.EndsWith("?", StringComparison.Ordinal))
        {
            isContainerNullable = true;
            s = s.Substring(0, s.Length - 1).TrimEnd();
            return (s, isArray, isContainerNullable, isElementNullable);
        }

        // 4) Plain type: Foo
        return (s, false, false, false);
    }

    /// <summary>
    /// TryParse variant that returns true when a non-empty base qualified name was produced.
    /// Note: semantic validation (IsQualifiedName) should still be performed by the caller.
    /// </summary>
    public static bool TryParseTypeRef(this string? token,
        out string qualifiedName,
        out bool isArray,
        out bool isContainerNullable,
        out bool isElementNullable)
    {
        var result = ParseTypeRef(token);
        qualifiedName = result.QualifiedName;
        isArray = result.IsArray;
        isContainerNullable = result.IsContainerNullable;
        isElementNullable = result.IsElementNullable;
        return !string.IsNullOrWhiteSpace(qualifiedName);
    }
}
