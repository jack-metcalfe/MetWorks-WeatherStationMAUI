namespace MetWorks.Apps.Maui.WeatherStationMaui.UITools.Converters;
public sealed class AirTemperatureFormatter : IValueConverter
{
    public object Convert(
        object? value, 
        Type targetType, 
        object? parameter, 
        CultureInfo culture
    )
    {
        if (value is null)
            return "--";

        if (value is double d)
        {
            if (d is double.NaN)
                return "--";
            else
                return d.ToString("F0", culture);
        }

        if (value is float f)
            return f.ToString("F0", culture);

        if (value is decimal m)
            return m.ToString("F0", culture);

        if (value is IFormattable formattable)
            return formattable.ToString(null, culture);

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}