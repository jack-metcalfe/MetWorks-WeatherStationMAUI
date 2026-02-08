namespace MetWorks.Ingest;
public record UdpPacketTableEntry
{
    public string TableName { get; init; } = string.Empty;
    public string TableScriptName => $"{TableName}.sql";
}
public static class UdpPacketTableData
{
    public static Dictionary<PacketEnum, UdpPacketTableEntry> PacketTableDataMap = new()
    {
        { 
            PacketEnum.Observation, 
            new UdpPacketTableEntry { TableName = "observation" }
        },
        { 
            PacketEnum.Wind, 
            new UdpPacketTableEntry { TableName = "wind" } 
        },
        { 
            PacketEnum.Precipitation, 
            new UdpPacketTableEntry { TableName = "precipitation" } 
        },
        { 
            PacketEnum.Lightning, 
            new UdpPacketTableEntry { TableName = "lightning" } 
        }
    };
}