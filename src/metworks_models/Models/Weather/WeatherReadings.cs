namespace MetWorksModels.Weather;
/// <summary>
/// Observation reading implementation with temperature, humidity, and pressure.
/// Immutable record with RedStar.Amounts for type-safe unit handling.
/// </summary>
public record ObservationReading : IObservationReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Observation;
    public required ReadingProvenance Provenance { get; init; }

    // IObservationReading properties
    public required Amount Temperature { get; init; }
    public required double HumidityPercent { get; init; }
    public required Amount Pressure { get; init; }
    public Amount? DewPoint { get; init; }
    public Amount? HeatIndex { get; init; }
    public Amount? WindChill { get; init; }
    public Amount? StationPressure { get; init; }
    public double? UvIndex { get; init; }
    public double? SolarRadiation { get; init; }
}

/// <summary>
/// Wind reading implementation with speed, direction, and gusts.
/// Immutable record with RedStar.Amounts for type-safe speed measurements.
/// </summary>
public record WindReading : IWindReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Wind;
    public required ReadingProvenance Provenance { get; init; }

    // IWindReading properties
    public required Amount Speed { get; init; }
    public required double DirectionDegrees { get; init; }
    public required string DirectionCardinal { get; init; }
    public Amount? GustSpeed { get; init; }
    public Amount? AverageSpeed { get; init; }
    public Amount? LullSpeed { get; init; }
}

/// <summary>
/// Precipitation reading implementation with rain rate and accumulation totals.
/// Immutable record with RedStar.Amounts for type-safe precipitation measurements.
/// </summary>
public record PrecipitationReading : IPrecipitationReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Precipitation;
    public required ReadingProvenance Provenance { get; init; }

    // IPrecipitationReading properties
    public required Amount RainRate { get; init; }
    public Amount? DailyAccumulation { get; init; }
    public Amount? HourlyAccumulation { get; init; }
    public Amount? MinuteAccumulation { get; init; }
    public string? PrecipitationType { get; init; }
}

/// <summary>
/// Lightning reading implementation with strike distance and count.
/// Immutable record with RedStar.Amounts for type-safe distance measurements.
/// </summary>
public record LightningReading : ILightningReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Lightning;
    public required ReadingProvenance Provenance { get; init; }

    // ILightningReading properties
    public required Amount StrikeDistance { get; init; }
    public required int StrikeCount { get; init; }
    public Amount? AverageDistance { get; init; }
    public double? StrikeEnergy { get; init; }
    public DateTime? LastStrikeTime { get; init; }
}