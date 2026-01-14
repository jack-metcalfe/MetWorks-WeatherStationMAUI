namespace Interfaces;
public interface IUdpReading : IUdpReadingMinimal
{
    IUdpMessageType IUdpMessageType { get; }
}
