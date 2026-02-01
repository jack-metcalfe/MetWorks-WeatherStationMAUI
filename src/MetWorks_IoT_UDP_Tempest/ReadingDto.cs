namespace MetWorks.IoT.UDP.Tempest;
public record ReadingDto
{
    public required string HubSerialNumber { get; init; }
    public required string SerialNumber { get; init; }
    public required string Type { get; init; }
}