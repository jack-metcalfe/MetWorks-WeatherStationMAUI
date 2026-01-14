namespace Utility;
public class ReadOnlyMemoryOfCharFactory
{
    public static ReadOnlyMemory<char> FromPath(
        ILogger iFileLoggger, string filePath, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        string content = File.ReadAllText(filePath, encoding);
        return content.AsMemory();
    }
    public static ReadOnlyMemory<char> AsMemory(string content)
    {
        return content.AsMemory();
    }
    public static ReadOnlyMemory<char> From(byte[] byteArray, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        string content = encoding.GetString(byteArray);
        return content.AsMemory();
    }
}
