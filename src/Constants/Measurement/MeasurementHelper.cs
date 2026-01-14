namespace Constants.Measurement;
public class MeasurementHelper
{
    public const string UnitOfMeasure_airPressure = "airPressure";
    public const string UnitOfMeasure_airTemperature = "airTemperature";
    public const string UnitOfMeasure_lightningDistance = "lightningDistance";
    public const string UnitOfMeasure_precipitationAmount = "precipitationAmount";
    public const string UnitOfMeasure_windSpeed = "windSpeed";

    public Dictionary<MeasurementTypeEnum, string> MeasurementTypeEnumToName = new()
    {
        { MeasurementTypeEnum.AirPressure, UnitOfMeasure_airPressure },
        { MeasurementTypeEnum.AirTemperature, UnitOfMeasure_airTemperature },
        { MeasurementTypeEnum.LightningDistance, UnitOfMeasure_lightningDistance },
        { MeasurementTypeEnum.PrecipitationAmount, UnitOfMeasure_precipitationAmount },
        { MeasurementTypeEnum.WindSpeed, UnitOfMeasure_windSpeed },
    };

    public Dictionary<string, MeasurementTypeEnum> NameToMeasurementTypeEnum = new()
    {
        { UnitOfMeasure_airPressure, MeasurementTypeEnum.AirPressure },
        { UnitOfMeasure_airTemperature, MeasurementTypeEnum.AirTemperature },
        { UnitOfMeasure_lightningDistance, MeasurementTypeEnum.LightningDistance },
        { UnitOfMeasure_precipitationAmount, MeasurementTypeEnum.PrecipitationAmount },
        { UnitOfMeasure_windSpeed, MeasurementTypeEnum.WindSpeed },
    };

    public Dictionary<MeasurementTypeEnum, UnitType> MeasurementTypeEnumToUnitType = new()
    {
        { MeasurementTypeEnum.AirPressure, PressureUnits.InchOfMercury.UnitType },
        { MeasurementTypeEnum.AirTemperature, TemperatureUnits.DegreeFahrenheit.UnitType },
        { MeasurementTypeEnum.LightningDistance, LengthUnits.Mile.UnitType },
        { MeasurementTypeEnum.PrecipitationAmount, LengthUnits.Inch.UnitType },
        { MeasurementTypeEnum.WindSpeed, SpeedUnits.MilePerHour.UnitType }
    };

}