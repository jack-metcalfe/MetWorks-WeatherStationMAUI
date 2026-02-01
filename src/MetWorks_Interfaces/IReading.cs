namespace MetWorks.Interfaces;
public interface IReading
{
    string HubSerialNumber { get; }
    string SerialNumber { get; }
    string Type { get; }
}
