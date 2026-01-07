namespace Utility;
public static class DictionaryExtensions
{
    public static bool IsNullOrEmpty<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        where TKey : notnull =>
        dictionary == null || dictionary.Count == 0;

    public static Dictionary<TKey, TValue> ToSafeDictionary<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        Action<TKey>? onDuplicate = null) where TKey : notnull
    {
        try
        {
            var dict = new Dictionary<TKey, TValue>();
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!dict.TryAdd(key, valueSelector(item)))
                    onDuplicate?.Invoke(key);
            }
            return dict;
        }
        catch (Exception exception)
        {
            throw new Exception("Failed to create safe dictionary.", exception);
        }
    }

}