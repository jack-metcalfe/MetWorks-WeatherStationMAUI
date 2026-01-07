// IdGenerator.cs
namespace Utility;
public static class IdGenerator
{
    public static Guid CreateCombGuid()
    {
        var guidArray = Guid.NewGuid().ToByteArray();
        var timestamp = DateTime.UtcNow.Ticks;

        // overwrite the last 6 bytes with timestamp
        byte[] timeBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        Array.Copy(timeBytes, 2, guidArray, 10, 6);
        return new Guid(guidArray);
    }
}
