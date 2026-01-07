namespace InterfaceDefinition;
public interface IUdpReading : IUdpReadingMinimal
{
    IUdpMessageType IUdpMessageType { get; }
}
