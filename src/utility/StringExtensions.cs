namespace Utility;
public static class StringExtensions
{
    public static string AddNullableSuffix(this string input)
    {
        return input.HasNullableSuffix() ? input + "?" : input;
    }
    public static bool IsNullOrWhiteSpace(this string? input)
    {
        return string.IsNullOrWhiteSpace(input);
    }
    /// <summary>
    /// Decide whether a raw token should be emitted as a C# literal (quoted) or left as-is
    /// (for numeric, boolean, enum-like or qualified identifiers). Returns a C# expression string.
    /// </summary>
    public static string EscapeLiteralForArrayElement(this string? raw)
    {        
        if (raw is null) return "null";

        var s = raw.Trim();

        // explicit null literal
        if (string.Equals(s, "null", StringComparison.OrdinalIgnoreCase))
            return "null";

        // boolean literal
        if (bool.TryParse(s, out _))
            return s;

        // integer or floating point literal
        if (long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _)
            || double.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return s;
        }

        // Heuristic for enum or qualified identifier: allow letters, digits, underscore and dot, optionally generic markers
        // Examples that should be emitted as-is: MyEnum.Value, Namespace.Type.Member, SomeType`1.Member (rare)
        bool IsIdentifierChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '`';

        if (s.Length > 0 && s.All(IsIdentifierChar) && s.Any(char.IsLetter))
        {
            // treat as identifier/enum/qualified name and emit as-is
            return s;
        }

        // Fallback: emit as quoted C# string literal
        return s.EscapeStringLiteral();
    }

    /// <summary>
    /// Escape a string as a valid C# string literal. Returns "null" for null input.
    /// Uses verbatim @"..." only when helpful.
    /// </summary>
    public static string EscapeStringLiteral(this string? input)
    {
        if (input is null) return "null";

        // If the string contains a newline or backslash but no double-quote, prefer verbatim string.
        bool containsDoubleQuote = input.Contains('"');
        bool containsBackslash = input.Contains('\\');
        bool containsNewLine = input.Contains('\n') || input.Contains('\r');

        if (!containsDoubleQuote && (containsBackslash || containsNewLine))
        {
            // Escape double quotes for verbatim form (none in this branch)
            return "@\"" + input.Replace("\"", "\"\"") + "\"";
        }

        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var ch in input)
        {
            switch (ch)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\0': sb.Append("\\0"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    // For non-printable / non-ASCII characters use \uXXXX escape for safety
                    if (ch < 32 || ch > 0x7F)
                        sb.AppendFormat("\\u{0:X4}", (int)ch);
                    else
                        sb.Append(ch);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
    public static string ReplaceTokens(this string input, IDictionary<string, string> tokens)
    {
        var result = input;
        foreach (var kv in tokens)
        {
            result = result.Replace(kv.Key, kv.Value ?? string.Empty, StringComparison.Ordinal);
        }
        return result;
    }
    public static string? GetNamespaceFromDottedIdentifier(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        int lastDotIndex = input.LastIndexOf('.');
        if (lastDotIndex <= 0) return null;
        return input.Substring(0, lastDotIndex);
    }
    public static string? GetTypeNameFromDottedIdentifier(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        int lastDotIndex = input.LastIndexOf('.');
        if (lastDotIndex < 0) return input;
        return input.Substring(lastDotIndex + 1);
    }
    public static string? MakeIdentifier(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "_";

        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
            else if (ch == '.')
                sb.Append('_');
            else
                sb.Append('_');
        }

        if (sb.Length == 0 || !(char.IsLetter(sb[0]) || sb[0] == '_'))
            sb.Insert(0, '_');

        return sb.ToString();
    }

    public static string RemoveArraySuffix(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        return input.EndsWith("[]", StringComparison.Ordinal)
            ? input.Substring(0, input.Length - 2)
            : input;
    }
    public static string? RemoveSystemNamespacePrefix(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        const string systemPrefix = "System.";
        return input.StartsWith(systemPrefix, StringComparison.Ordinal)
            ? input.Substring(systemPrefix.Length)
            : input;
    }
    public static string NormalizeArraySuffix(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        return input.EndsWith("[]", StringComparison.Ordinal)
            ? input.Substring(0, input.Length - 2) + "__Array"
            : input;
    }

    public static string ReplaceUnderbarsWithDots(this string? input)
    {
        return input?.Replace('_', '.') ?? string.Empty;
    }

    public static string ReplaceDotsWithUnderbars(this string? input)
    {
        return input?.Replace('.', '_') ?? string.Empty;
    }

    public static string NamespaceFromFullyQualifiedTypeName(string fullyQualifiedTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullyQualifiedTypeName))
            return string.Empty;

        int lastDotIndex = fullyQualifiedTypeName.LastIndexOf('.');
        return lastDotIndex >= 0
            ? fullyQualifiedTypeName.Substring(0, lastDotIndex)
            : string.Empty;
    }

    public static string TypeNameFromFullyQualifiedTypeName(string fullyQualifiedTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullyQualifiedTypeName))
            return string.Empty;

        int lastDotIndex = fullyQualifiedTypeName.LastIndexOf('.');
        return lastDotIndex >= 0
            ? fullyQualifiedTypeName.Substring(lastDotIndex + 1)
            : fullyQualifiedTypeName;
    }
}
