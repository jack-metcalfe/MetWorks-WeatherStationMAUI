using System;
using System.Threading;
using BasicEventRelay;
using MetWorksModels.Weather;
using MetWorksModels.Provenance;
using RedStar.Amounts;
using RedStar.Amounts.StandardUnits;
using Utility;

namespace MetWorksWeather.Services;

/// <summary>
/// Mock service that generates fake weather data for development/testing.
/// IMPORTANT: This mock faithfully replicates WeatherDataTransformer's behavior:
/// - Uses ISingletonEventRelay (not Observables)
/// - Sends IObservationReading and IWindReading (same as real service)
/// - Can be swapped with real service transparently
/// </summary>
public class MockWeatherReadingService : IDisposable
{
    private Timer? _timer;
    private readonly Random _random = new();
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    /// <summary>
    /// Start emitting mock weather readings every 2 seconds.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;

        // Emit mock readings every 2 seconds
        _timer = new Timer(_ =>
        {
            try
            {
                // Send same message types as real WeatherDataTransformer
                ISingletonEventRelay.Send(CreateMockObservationReading());
                ISingletonEventRelay.Send(CreateMockWindReading());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mock service error: {ex.Message}");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Stop emitting mock readings.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Creates a mock observation reading with random but realistic values.
    /// Matches the structure of real ObservationReading from WeatherDataTransformer.
    /// </summary>
    private IObservationReading CreateMockObservationReading()
    {
        var now = DateTime.UtcNow;

        return new ObservationReading
        {
            // Identity
            Id = IdGenerator.CreateCombGuid(),
            SourcePacketId = IdGenerator.CreateCombGuid(),
            Timestamp = now,
            ReceivedUtc = now,

            // Weather data (realistic ranges)
            Temperature = new Amount(_random.Next(50, 95), TemperatureUnits.DegreeFahrenheit),
            Pressure = new Amount(_random.Next(2900, 3100) / 100.0, PressureUnits.InchOfMercury),
            HumidityPercent = _random.Next(30, 90),

            // Optional fields
            DewPoint = new Amount(_random.Next(40, 70), TemperatureUnits.DegreeFahrenheit),
            HeatIndex = null,
            WindChill = null,
            StationPressure = new Amount(_random.Next(2900, 3100) / 100.0, PressureUnits.InchOfMercury),
            UvIndex = _random.Next(0, 11),
            SolarRadiation = _random.Next(0, 1000),

            // Provenance (marks as mock data)
            Provenance = new ReadingProvenance
            {
                RawPacketId = IdGenerator.CreateCombGuid(),
                UdpReceiptTime = now,
                TransformStartTime = now,
                TransformEndTime = now.AddMilliseconds(1),
                SourceUnits = "mock-imperial",
                TargetUnits = "imperial",
                TransformerVersion = "mock-1.0"
            }
        };
    }

    /// <summary>
    /// Creates a mock wind reading with random but realistic values.
    /// Matches the structure of real WindReading from WeatherDataTransformer.
    /// </summary>
    private IWindReading CreateMockWindReading()
    {
        var now = DateTime.UtcNow;
        var directionDegrees = _random.Next(0, 360);

        return new WindReading
        {
            // Identity
            Id = IdGenerator.CreateCombGuid(),
            SourcePacketId = IdGenerator.CreateCombGuid(),
            Timestamp = now,
            ReceivedUtc = now,

            // Wind data (realistic ranges)
            Speed = new Amount(_random.Next(0, 30), SpeedUnits.MilePerHour),
            DirectionDegrees = directionDegrees,
            DirectionCardinal = DegreesToCardinal(directionDegrees),

            // Optional fields
            GustSpeed = new Amount(_random.Next(5, 40), SpeedUnits.MilePerHour),
            AverageSpeed = new Amount(_random.Next(0, 25), SpeedUnits.MilePerHour),
            LullSpeed = new Amount(_random.Next(0, 15), SpeedUnits.MilePerHour),

            // Provenance (marks as mock data)
            Provenance = new ReadingProvenance
            {
                RawPacketId = IdGenerator.CreateCombGuid(),
                UdpReceiptTime = now,
                TransformStartTime = now,
                TransformEndTime = now.AddMilliseconds(1),
                SourceUnits = "mock-imperial",
                TargetUnits = "imperial",
                TransformerVersion = "mock-1.0"
            }
        };
    }

    /// <summary>
    /// Convert wind direction degrees to cardinal/intercardinal direction.
    /// Same logic as WeatherDataTransformer.
    /// </summary>
    private static string DegreesToCardinal(double degrees)
    {
        // Normalize to 0-360
        degrees = degrees % 360;
        if (degrees < 0) degrees += 360;

        return degrees switch
        {
            >= 348.75 or < 11.25 => "N",
            >= 11.25 and < 33.75 => "NNE",
            >= 33.75 and < 56.25 => "NE",
            >= 56.25 and < 78.75 => "ENE",
            >= 78.75 and < 101.25 => "E",
            >= 101.25 and < 123.75 => "ESE",
            >= 123.75 and < 146.25 => "SE",
            >= 146.25 and < 168.75 => "SSE",
            >= 168.75 and < 191.25 => "S",
            >= 191.25 and < 213.75 => "SSW",
            >= 213.75 and < 236.25 => "SW",
            >= 236.25 and < 258.75 => "WSW",
            >= 258.75 and < 281.25 => "W",
            >= 281.25 and < 303.75 => "WNW",
            >= 303.75 and < 326.25 => "NW",
            >= 326.25 and < 348.75 => "NNW",
            _ => "N"
        };
    }

    public void Dispose()
    {
        Stop();
    }
}