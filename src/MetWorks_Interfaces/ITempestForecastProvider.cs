namespace MetWorks.Interfaces;

public interface ITempestForecastProvider
{
    ValueTask<TempestForecast?> GetForecastAsync(CancellationToken cancellationToken = default);
}

public sealed record TempestForecast(
    long StationId,
    DateTimeOffset RetrievedUtc,
    double? Latitude,
    double? Longitude,
    string? TimeZone,
    int? TimeZoneOffsetMinutes,
    IReadOnlyList<TempestForecastDay> Daily,
    IReadOnlyList<TempestForecastHour> Hourly
);

public sealed record TempestForecastDay(
    DateTimeOffset? DayStartLocal,
    int? DayNum,
    int? MonthNum,
    string? Conditions,
    string? Icon,
    DateTimeOffset? SunriseLocal,
    DateTimeOffset? SunsetLocal,
    double? AirTempHigh,
    double? AirTempLow,
    int? PrecipProbability,
    string? PrecipIcon,
    string? PrecipType
);

public sealed record TempestForecastHour(
    DateTimeOffset? TimeLocal,
    int? LocalHour,
    int? LocalDay,
    string? Conditions,
    string? Icon,
    double? AirTemperature,
    double? FeelsLike,
    double? SeaLevelPressure,
    int? RelativeHumidity,
    int? Precip,
    int? PrecipProbability,
    double? WindAvg,
    double? WindGust,
    double? WindDirection,
    string? WindDirectionCardinal,
    double? Uv
);
