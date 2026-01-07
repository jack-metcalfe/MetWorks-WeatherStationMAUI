namespace Utility;
public static class EpochExtensions
{
    public static long ToUnixEpochSeconds(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be in UTC", nameof(utcDateTime));

        return new DateTimeOffset(utcDateTime).ToUnixTimeSeconds();
    }

    public static long ToUnixEpochSeconds(this DateTimeOffset utcDateTimeOffset)
    {
        return utcDateTimeOffset.ToUnixTimeSeconds();
    }
}
