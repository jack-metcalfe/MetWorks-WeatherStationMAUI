namespace Utility;
public static class ListExtensions
{
    public static bool IsNullOrEmpty<T>(this List<T>? list) =>
        list == null || list.Count == 0;

    public static bool IsNullOrEmpty<T>(this IReadOnlyList<T>? list) =>
        list == null || list.Count == 0;

    public static string JoinAndEscapeLiterals(this IReadOnlyList<string>? raws)
    {
        if (raws is null || raws.Count == 0) return string.Empty;
        return string.Join(", ", raws.Select(s => s.EscapeLiteralForArrayElement()));
    }
}
