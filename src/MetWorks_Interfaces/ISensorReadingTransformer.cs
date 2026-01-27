namespace MetWorks.Interfaces;
/// <summary>
/// Public service interface for the sensor reading transformer.
/// Inherits IServiceReady so consumers can await readiness without depending on the concrete type.
/// </summary>
public interface ISensorReadingTransformer : IServiceReady
{
}