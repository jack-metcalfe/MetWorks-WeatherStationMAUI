namespace MetWorks.Apps.Maui.WeatherStationMaui.UITools.Converters;

public sealed class QuantityFormatter : IMultiValueConverter
{
    const string Missing = "--";

    public object Convert(
        object?[]? values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (values is null || values.Length == 0)
            return Missing;

        var number = values[0];
        var unit = values.Length >= 2 ? values[1] as string : null;

        if (TryFormatNumber(number, TryGetDigits(parameter), culture, out var formattedNumber))
        {
            if (!string.IsNullOrWhiteSpace(unit))
                return $"{formattedNumber} {unit}";

            return formattedNumber;
        }

        return Missing;
    }

    public object[] ConvertBack(
        object? value,
        Type[] targetTypes,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();

    static int TryGetDigits(object? parameter)
    {
        if (parameter is null) return 0;

        if (parameter is int i) return Math.Clamp(i, 0, 10);

        if (parameter is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return Math.Clamp(parsed, 0, 10);

        return 0;
    }

    static bool TryFormatNumber(object? value, int digits, CultureInfo culture, out string formatted)
    {
        formatted = string.Empty;

        if (value is null)
            return false;

        if (value is double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
                return false;

            formatted = d.ToString($"F{digits}", culture);
            return true;
        }

        if (value is float f)
        {
            if (float.IsNaN(f) || float.IsInfinity(f))
                return false;

            formatted = f.ToString($"F{digits}", culture);
            return true;
        }

        if (value is decimal m)
        {
            formatted = m.ToString($"F{digits}", culture);
            return true;
        }

        // Allow bindings that supply strings (already formatted) or other IFormattable numerics.
        if (value is IFormattable formattable)
        {
            formatted = formattable.ToString($"F{digits}", culture);
            return true;
        }

        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            formatted = s;
            return true;
        }

        return false;
    }
}
