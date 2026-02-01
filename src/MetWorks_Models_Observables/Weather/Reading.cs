namespace MetWorks.Models.Observables.Weather;
public record Reading
{
    public required string HubSerialNumber { get; init; }
    public required string SerialNumber { get; init; }
    public required string Type { get; init; }
}
