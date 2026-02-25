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
    Amount? AirTempHigh,
    Amount? AirTempLow,
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
    Amount? AirTemperature,
    Amount? FeelsLike,
    Amount? SeaLevelPressure,
    int? RelativeHumidity,
    int? Precip,
    int? PrecipProbability,
    Amount? WindAvg,
    Amount? WindGust,
    double? WindDirection,
    string? WindDirectionCardinal,
    double? Uv
);
