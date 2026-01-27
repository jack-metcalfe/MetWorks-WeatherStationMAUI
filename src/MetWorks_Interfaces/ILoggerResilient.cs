namespace MetWorks.Interfaces;
/// <summary>
/// Marker interface for resilient loggers that support readiness.
/// </summary>
public interface ILoggerResilient : ILogger, IServiceReady
{
}
