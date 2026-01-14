namespace Interfaces;
public interface IUdpMessageTypeDto
{
    string Type { get; }
    string ValueArrayName { get; }
    int ValueArrayDimensions { get; }
    IUdpMessageTypeFieldDto[] IUdpMessageTypeFields { get; }
}
