namespace Utility;
public static class StreamReaderFactory
{
    public static StreamReader From(ReadOnlyStream readOnlyStream, Encoding? encoding = null)
    {
        var stream = new MemoryStream();
        readOnlyStream.CopyTo(stream);
        stream.Position = 0;
        return new StreamReader(stream, encoding ?? Encoding.UTF8);
    }
    public static StreamReader From(string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var byteArray = encoding.GetBytes(content);
        var stream = new MemoryStream(byteArray);
        return new StreamReader(stream, encoding);
    }
    public static StreamReader FromPath(string filePath, ILogger iFileLogger, Encoding? encoding = null)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new StreamReader(stream, encoding ?? Encoding.UTF8);
        }
        catch (Exception exception)
        {
            throw iFileLogger.LogExceptionAndReturn(
                new IOException($"Failed to create StreamReader for file: {filePath}", exception));
        }
    }
}
