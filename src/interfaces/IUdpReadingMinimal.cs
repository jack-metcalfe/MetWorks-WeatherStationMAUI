namespace Interfaces;
public interface IUdpReadingMinimal
{
    string Id { get; }
    string JsonString { get; }
    DateTimeOffset ReceivedUtcUtcDateTimeOffset { get; }
}
