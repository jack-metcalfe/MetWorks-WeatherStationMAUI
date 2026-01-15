namespace Constants.Settings.Paths;

using static Constants.Measurement.MeasurementHelper;
public static class Settings
{
    public const string Section = "settings";

    // Unit of Measure Settings
    const string UnitOfMeasureBasePath = "/services/unitOfMeasure/";
    const string UnitOfMeasureSelectionPathSuffix = "/selection";

    public const string AirPressureUnitOfMeasureSettingPath =
            $"{UnitOfMeasureBasePath}{UnitOfMeasure_airPressure}{UnitOfMeasureSelectionPathSuffix}";

    public const string AirTemperatureUnitOfMeasureSettingPath =
            $"{UnitOfMeasureBasePath}{UnitOfMeasure_airTemperature}{UnitOfMeasureSelectionPathSuffix}";

    public const string LightningDistanceUnitOfMeasureSettingPath =
        $"{UnitOfMeasureBasePath}{UnitOfMeasure_lightningDistance}{UnitOfMeasureSelectionPathSuffix}";

    public const string PrecipitationAmountUnitOfMeasureSettingPath =
            $"{UnitOfMeasureBasePath}{UnitOfMeasure_precipitationAmount}{UnitOfMeasureSelectionPathSuffix}";

    public const string WindSpeedUnitOfMeasureSettingPath =
            $"{UnitOfMeasureBasePath}{UnitOfMeasure_windSpeed}{UnitOfMeasureSelectionPathSuffix}";
}
