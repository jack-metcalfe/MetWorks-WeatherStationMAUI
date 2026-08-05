namespace MetWorks.Interfaces;

/// <summary>
/// Bootstrap logger interface. Implementations emit to a lightweight output
/// (e.g., Debug window) and buffer entries so they can be replayed into a
/// fully-initialized logger once infrastructure is ready.
/// </summary>
public interface ILoggerStub : ILogger
{
    /// <summary>
    /// Drain all buffered log entries into <paramref name="target"/>, then clear the buffer.
    /// Safe to call multiple times; subsequent calls are no-ops if the buffer is empty.
    /// </summary>
    void DrainTo(ILogger target);
}
