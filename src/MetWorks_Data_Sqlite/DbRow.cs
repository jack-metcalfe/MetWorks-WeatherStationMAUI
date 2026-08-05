using System.Globalization;

namespace MetWorks.Data.Sqlite;

public sealed class DbRow(IReadOnlyDictionary<string, object?> values)
{
    readonly IReadOnlyDictionary<string, object?> _values = values;

    public object? this[string name] => _values.TryGetValue(name, out var v) ? v : null;

    public bool TryGetString(string name, out string? value)
    {
        if (!_values.TryGetValue(name, out var raw) || raw is null || raw is DBNull)
        {
            value = null;
            return false;
        }

        value = Convert.ToString(raw, CultureInfo.InvariantCulture);
        return true;
    }

    public bool TryGetInt64(string name, out long value)
    {
        value = default;

        if (!_values.TryGetValue(name, out var raw) || raw is null || raw is DBNull)
            return false;

        value = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
        return true;
    }

    public bool TryGetDouble(string name, out double value)
    {
        value = default;

        if (!_values.TryGetValue(name, out var raw) || raw is null || raw is DBNull)
            return false;

        value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
        return true;
    }
}
