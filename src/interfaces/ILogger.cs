namespace InterfaceDefinition;
public interface ILogger : IRegistryExport
{
    void Information(string message);
    void Warning(string message);
    void Error(string message, Exception exception);
    void Error(string message);
    void Debug(string message);
    void Trace(string message);
    Exception LogExceptionAndReturn(Exception exception);
    Exception LogExceptionAndReturn(Exception exception, string message);
}