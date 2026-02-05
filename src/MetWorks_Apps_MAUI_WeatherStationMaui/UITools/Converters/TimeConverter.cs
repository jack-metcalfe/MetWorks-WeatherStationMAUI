namespace MetWorks.Apps.Maui.WeatherStationMaui.UITools.Converters;

using System.Globalization;
using Microsoft.Maui.Controls;

public sealed class TimeConverter : IValueConverter
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

        if (value is DateTime d)
        {
            if (d == DateTime.MinValue)
                return "--";

            return d.ToLocalTime();
        }
        else return "--";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}