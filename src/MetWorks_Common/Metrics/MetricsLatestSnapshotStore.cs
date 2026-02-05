namespace MetWorks.Common.Metrics;

using System.Threading;

public sealed class MetricsLatestSnapshotStore : IMetricsLatestSnapshot
{
    MetricsLatestSnapshot _snapshot = new(
        CapturedUtc: DateTime.MinValue,
        IntervalSeconds: 0,
        JsonPayload: string.Empty,
        PersistStatus: "not_attempted",
        PersistAttemptUtc: null,
        PersistErrorMessage: null);

    MetricsStructuredSnapshot? _structured;

    public MetricsLatestSnapshot Current => Volatile.Read(ref _snapshot);

    public MetricsStructuredSnapshot? CurrentStructured => Volatile.Read(ref _structured);

    public void RecordCaptured(DateTime capturedUtc, int intervalSeconds, string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
            return;

        var parsePayload = jsonPayload;
        if (parsePayload.StartsWith("METRICS ", StringComparison.Ordinal))
            parsePayload = parsePayload[8..];

        if (!MetricsStructuredSnapshotParser.TryParse(parsePayload, out var structured))
            structured = null;

        var prior = Current;
        var next = prior with
        {
            CapturedUtc = capturedUtc,
            IntervalSeconds = intervalSeconds,
            JsonPayload = jsonPayload,
            PersistStatus = "not_attempted",
            PersistAttemptUtc = null,
            PersistErrorMessage = null
        };

        Volatile.Write(ref _snapshot, next);
        Volatile.Write(ref _structured, structured);
    }

    public void RecordPersistAttempt(DateTime attemptUtc)
    {
        var prior = Current;
        var next = prior with
        {
            PersistStatus = "attempted",
            PersistAttemptUtc = attemptUtc,
            PersistErrorMessage = null
        };

        Volatile.Write(ref _snapshot, next);

        var structuredPrior = CurrentStructured;
        if (structuredPrior is not null)
        {
            Volatile.Write(ref _structured, structuredPrior);
        }
    }

    public void RecordPersistSuccess(DateTime attemptUtc)
    {
        var prior = Current;
        var next = prior with
        {
            PersistStatus = "success",
            PersistAttemptUtc = attemptUtc,
            PersistErrorMessage = null
        };

        Volatile.Write(ref _snapshot, next);

        var structuredPrior = CurrentStructured;
        if (structuredPrior is not null)
        {
            Volatile.Write(ref _structured, structuredPrior);
        }
    }

    public void RecordPersistFailure(DateTime attemptUtc, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            message = "unknown";

        var prior = Current;
        var next = prior with
        {
            PersistStatus = "failure",
            PersistAttemptUtc = attemptUtc,
            PersistErrorMessage = message
        };

        Volatile.Write(ref _snapshot, next);

        var structuredPrior = CurrentStructured;
        if (structuredPrior is not null)
        {
            Volatile.Write(ref _structured, structuredPrior);
        }
    }
}
