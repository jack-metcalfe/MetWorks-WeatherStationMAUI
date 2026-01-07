namespace Utility;
public class StringToEnum
{
    public static T GetEnumKey<T>(string key) where T : struct
    {
        return Enum.TryParse<T>(key, ignoreCase: false, out var result)
            ? result
            : throw new ArgumentException($"Invalid key: {key}");
    }
    public static bool TryGetEnumKey<T>(string input, out T key) where T : struct
    {
        return Enum.TryParse<T>(input, ignoreCase: false, out key);
    }

}
