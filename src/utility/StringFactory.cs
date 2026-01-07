namespace Utility;
public static class StringFactory
{
    public static string FromPath(IFileLogger iFileLogger, string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return File.ReadAllText(filePath);
        }
        catch (Exception exception)
        {
            throw iFileLogger.LogExceptionAndReturn(
                new IOException($"Failed to read file: {filePath}", exception));
        }
    }
    public static string From(byte[] byteArray, Encoding? encoding = null)
    {
        return (encoding ??= Encoding.UTF8).GetString(byteArray);
    }
}
