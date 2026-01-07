namespace Utility;

public static class Converters
{
    public static object ConvertObject(Type type, object value)
    {
        return type switch
        {
            var t when t == typeof(Int32) => Convert.ToInt32(value),
            var t when t == typeof(bool) => Convert.ToBoolean(value),
            var t when t == typeof(double) => Convert.ToDouble(value),
            var t when t == typeof(DateTime) => Convert.ToDateTime(value),
            var t when t == typeof(Guid) => Convert.ToInt16((Guid)value),
            var t when t == typeof(string) => Convert.ToString(value)!,
            _ => throw new NotSupportedException($"Unsupported type: {type.FullName}")
        };
    }
}
