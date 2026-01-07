namespace Utility;
public static class NullPropertyGuard
{
    // For reference types
    public static T Get<T>(bool isInitialized, T? value, string propertyName) where T : class
    {
        if (!isInitialized)
            throw new InvalidOperationException("Instance is not initialized.");

        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
            throw new InvalidOperationException($"{propertyName} is not initialized.");

        return value;
    }
    public static T GetSafeClass<T>
        (T? value, string? message = null, IFileLogger? iFileLogger = null) where T : class
    {
        if (value is null
            || (value is string s && string.IsNullOrWhiteSpace(s))
            || (value is List<T> list && !list.Any())
        )
        {
            message ??= $"{typeof(T).Name} is null.";
            var exception = new InvalidOperationException(message);
            throw iFileLogger is null ? exception : iFileLogger.LogExceptionAndReturn(exception);
        }
        return value;
    }

    public static T GetSafeStruct<T>
        (T value, string? message = null, IFileLogger? iFileLogger = null) where T : struct
    {
        if (EqualityComparer<T>.Default.Equals(value, default))
        {
            message ??= $"{typeof(T).Name} is default.";
            var exception = new InvalidOperationException(message);
            throw iFileLogger is null ? exception : iFileLogger.LogExceptionAndReturn(exception);
        }

        return value;
    }
    public static T GetSafeNullableStruct<T>
        (T? value, string? message = null, IFileLogger? iFileLogger = null) where T : struct
    {
        if (value is null || EqualityComparer<T>.Default.Equals(value.Value, default))
        {
            message ??= $"{typeof(T).Name} is null or default.";
            var exception = new InvalidOperationException(message);
            throw iFileLogger is null ? exception : iFileLogger.LogExceptionAndReturn(exception);
        }

        return value.Value;
    }

    // For value types (structs, including bool, int, etc.)
    public static T Get<T>(bool isInitialized, T? value, string propertyName) where T : struct
    {
        if (!isInitialized)
            throw new InvalidOperationException("Instance is not initialized.");

        if (!value.HasValue)
            throw new InvalidOperationException($"{propertyName} is not initialized.");

        return value.Value;
    }
    public static void Set<T>(bool isInitialized, ref T? field, T? value, string propertyName) where T : class
    {
        if (isInitialized)
            throw new InvalidOperationException($"{propertyName} is immutable after initialization.");

        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
            throw new InvalidOperationException($"{propertyName} cannot be null or empty.");

        field = value;
    }
    public static void Set<T>(bool isInitialized, ref T? field, T? value, string propertyName) where T : struct
    {
        if (isInitialized)
            throw new InvalidOperationException($"{propertyName} is immutable after initialization.");

        if (!value.HasValue)
            throw new InvalidOperationException($"{propertyName} cannot be null.");

        field = value.Value;
    }

}

