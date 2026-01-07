namespace Utility;
public class ReadOnlyStreamFactory
{
    public static ReadOnlyStream From(Stream inner) => new ReadOnlyStream(inner);
    public static ReadOnlyStream FromPath(string filePath, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        string content = File.ReadAllText(filePath, encoding);
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return ReadOnlyStreamFactory.From(stream);
    }
}
