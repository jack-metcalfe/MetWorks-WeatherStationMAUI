namespace MetWorks.Persistence.SQLite;

using Microsoft.Data.Sqlite;

public enum SqliteParamType
{
    Null = 0,
    Text = 1,
    Integer = 2,
    Real = 3
}

public readonly record struct SqliteParam(string Name, SqliteParamType Type, string? TextValue, long IntegerValue, double RealValue)
{
    public static SqliteParam Null(string name) => new(name, SqliteParamType.Null, null, 0, 0);
    public static SqliteParam Text(string name, string value) => new(name, SqliteParamType.Text, value, 0, 0);
    public static SqliteParam Integer(string name, long value) => new(name, SqliteParamType.Integer, null, value, 0);
    public static SqliteParam Real(string name, double value) => new(name, SqliteParamType.Real, null, 0, value);

    internal void BindTo(SqliteCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Parameter name is required.", nameof(Name));
        }

        object value = Type switch
        {
            SqliteParamType.Null => DBNull.Value,
            SqliteParamType.Text => TextValue ?? string.Empty,
            SqliteParamType.Integer => IntegerValue,
            SqliteParamType.Real => RealValue,
            _ => throw new InvalidOperationException($"Unknown param type '{Type}'.")
        };

        command.Parameters.AddWithValue(Name, value);
    }
}
