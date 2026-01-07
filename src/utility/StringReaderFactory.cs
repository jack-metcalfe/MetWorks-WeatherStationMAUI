namespace Utility;
public static class StringReaderFactory
{
    public static StringReader FromString(string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var byteArray = encoding.GetBytes(content);
        var stream = new MemoryStream(byteArray);
        var streamReader = new StreamReader(stream, encoding);
        return new StringReader(streamReader.ReadToEnd());
    }

    public static StringReader FromPath(string filePath, IFileLogger iFileLogger, Encoding? encoding = null)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8);
            return new StringReader(streamReader.ReadToEnd());
        }
        catch (Exception exception)
        {
            throw iFileLogger.LogExceptionAndReturn(
                new IOException($"Failed to create StreamReader for file: {filePath}", exception));
        }
    }
}
