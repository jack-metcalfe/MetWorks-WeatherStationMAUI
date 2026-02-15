namespace MetWorks.Persistence.StreamShipping;

public readonly record struct LoggerRetentionOptions(
    TimeSpan RetainFor,
    TimeSpan PurgeInterval);
