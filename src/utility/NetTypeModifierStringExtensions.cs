
namespace Utility;
public static class NetTypeModifierStringExtensions
{
    public static string GetArraySuffix()
    {
        return "[]";
    }
    public static string GetNullableSuffix()
    {
        return "?";
    }
    public static string? Identifierize(this string? input)
    {
        return (string.IsNullOrWhiteSpace(input))
            ? input
            : input.IdentifierizeArraySuffix().IdentifierizeNullableSuffix().ReplaceDotsWithUnderbars();
    }
    public static string IdentifierizeArraySuffix(this string input)
    {
        return input.Replace("[]", "__Array");
    }
    public static string IdentifierizeNullableSuffix(this string input)
    {
        return input.Replace("?", "__Nullable");
    }
    public static bool HasArraySuffix(this string input)
    {
        return input.Contains("[]");
    }
    public static bool HasNullableSuffix(this string input)
    {
        return input.Contains("?");
    }
    public static string RemoveArraySuffix(this string input)
    {
        return input.Replace("[]", "");
    }
    public static string RemoveNullableSuffix(this string input)
    {
        return input.Replace("?", "");
    }
    internal static string ReplaceUnderbarsWithDots(this string input)
    {
        return input.Replace("_", ".");
    }

}

