namespace Utility;
public class ReadOnlyStringOnStackFactory
{
    public static ReadOnlySpan<char> FromPath(string filePath, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        string content = File.ReadAllText(filePath, encoding);
        return content.AsSpan();
    }
    public static ReadOnlySpan<char> AsSpan(string content)
    {
        return content.AsSpan();
    }
}
