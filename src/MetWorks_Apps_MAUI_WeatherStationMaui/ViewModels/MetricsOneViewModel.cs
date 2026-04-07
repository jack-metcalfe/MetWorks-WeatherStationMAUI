namespace MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;
public sealed class MetricsOneViewModel : INotifyPropertyChanged, IDisposable
{
    readonly IMetricsLatestSnapshot _latest;
    private readonly IInstanceIdentifier _iInstanceIdentifier;
    readonly ThreadingTimer _timer;

    MetricsLatestSnapshot _current;
    MetricsStructuredSnapshot? _structured;

    string _lastShippingUpload1 = "--";
    string _lastShippingUpload2 = "--";
    DateTime _lastShippingNonEmptyCapturedUtc = DateTime.MinValue;

    public string InstallationId => _iInstanceIdentifier.InstallationId;

    public MetricsLatestSnapshot Current
    {
        get => _current;
        private set
        {
            if (!Equals(_current, value))
            {
                _current = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PersistStatusDisplay));
                OnPropertyChanged(nameof(PersistErrorMessageDisplay));
                OnPropertyChanged(nameof(JsonPayloadPreview));
            }
        }
    }

    public MetricsStructuredSnapshot? Structured
    {
        get => _structured;
        private set
        {
            if (!Equals(_structured, value))
            {
                _structured = value;

                var uploads = value?.Shipping?.TopUploads;
                if (uploads is not null && uploads.Count > 0)
                {
                    _lastShippingUpload1 = FormatShippingUpload(uploads, 0);
                    _lastShippingUpload2 = FormatShippingUpload(uploads, 1);
                    _lastShippingNonEmptyCapturedUtc = value!.CapturedUtc;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(ProcessSummaryLine));
                OnPropertyChanged(nameof(GcSummaryLine));
                OnPropertyChanged(nameof(StorageSummaryLine));
                OnPropertyChanged(nameof(TopRelayHandler1));
                OnPropertyChanged(nameof(TopRelayHandler2));
                OnPropertyChanged(nameof(TopRelayFanout1));
                OnPropertyChanged(nameof(TopPipelineReading1));
                OnPropertyChanged(nameof(TopPipelineReading2));
                OnPropertyChanged(nameof(TopShippingUpload1));
                OnPropertyChanged(nameof(TopShippingUpload2));
            }
        }
    }
    public string ProcessSummaryLine
    {
        get
        {
            var p = Structured?.Process;
            if (p is null) return "--";
            return $"CPU Δ {p.CpuSecondsDelta:F2}s | Util {p.CpuUtilizationRatio:P1} | Threads {p.Threads} | Cores {p.ProcessorCount}";
        }
    }

    public string GcSummaryLine
    {
        get
        {
            var gc = Structured?.Process?.Gc;
            if (gc is null) return "--";
            return $"GC Δ G0 {gc.Gen0Delta} G1 {gc.Gen1Delta} G2 {gc.Gen2Delta} | Mem {gc.ManagedMemoryBytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    public string TopRelayHandler1 => FormatRelayHandler(Structured?.Relay?.TopHandlers, 0);
    public string TopRelayHandler2 => FormatRelayHandler(Structured?.Relay?.TopHandlers, 1);
    public string TopRelayFanout1 => FormatRelayFanout(Structured?.Relay?.TopFanout, 0);
    public string TopPipelineReading1 => FormatPipelineReading(Structured?.Pipeline?.TopReadings, 0);
    public string TopPipelineReading2 => FormatPipelineReading(Structured?.Pipeline?.TopReadings, 1);

    public string TopShippingUpload1 => FormatShippingUploadWithLastKnown(Structured?.Shipping?.TopUploads, 0, _lastShippingUpload1);
    public string TopShippingUpload2 => FormatShippingUploadWithLastKnown(Structured?.Shipping?.TopUploads, 1, _lastShippingUpload2);

    static string FormatRelayHandler(IReadOnlyList<MetricsRelayHandlerHotspot>? list, int idx)
    {
        if (list is null || idx < 0 || idx >= list.Count) return "--";
        var h = list[idx];
        var mt = string.IsNullOrWhiteSpace(h.MessageType) ? "?" : h.MessageType;
        var rt = string.IsNullOrWhiteSpace(h.RecipientType) ? "?" : h.RecipientType;
        return $"{mt} → {rt} | n={h.Count} avg={h.AvgMs:F1}ms max={h.MaxMs:F1}ms";
    }

    static string FormatRelayFanout(IReadOnlyList<MetricsRelayFanoutHotspot>? list, int idx)
    {
        if (list is null || idx < 0 || idx >= list.Count) return "--";
        var f = list[idx];
        var mt = string.IsNullOrWhiteSpace(f.MessageType) ? "?" : f.MessageType;
        return $"{mt} | handler_invocations={f.HandlerInvocations}";
    }

    static string FormatPipelineReading(IReadOnlyList<MetricsPipelineReadingHotspot>? list, int idx)
    {
        if (list is null || idx < 0 || idx >= list.Count) return "--";
        var r = list[idx];
        var t = string.IsNullOrWhiteSpace(r.ReadingType) ? "?" : r.ReadingType;
        return $"{t} | n={r.Count} retrans={r.Retransforms} udp→end avg={r.UdpToTransformEndAvgMs:F1}ms max={r.UdpToTransformEndMaxMs:F1}ms";
    }

    static string FormatShippingUpload(IReadOnlyList<MetricsShippingUploadHotspot>? list, int idx)
    {
        if (list is null || idx < 0 || idx >= list.Count) return "--";
        var u = list[idx];
        var table = string.IsNullOrWhiteSpace(u.Table) ? "?" : u.Table;
        return $"{table} | ok={u.Successes}/{u.Attempts} rows={u.Rows} avg={u.AvgMs:F0}ms max={u.MaxMs:F0}ms";
    }

    string FormatShippingUploadWithLastKnown(IReadOnlyList<MetricsShippingUploadHotspot>? list, int idx, string lastKnown)
    {
        var current = FormatShippingUpload(list, idx);
        if (!string.Equals(current, "--", StringComparison.Ordinal))
            return current;

        if (string.Equals(lastKnown, "--", StringComparison.Ordinal) || _lastShippingNonEmptyCapturedUtc == DateTime.MinValue)
            return "--";

        var age = DateTime.UtcNow - _lastShippingNonEmptyCapturedUtc;
        if (age < TimeSpan.Zero)
            age = TimeSpan.Zero;

        return $"(last {age.TotalSeconds:F0}s ago) {lastKnown}";
    }

    public string StorageSummaryLine
    {
        get
        {
            var s = Structured?.Storage;
            if (s is null) return "--";

            var logBytes = s.LogFile?.Bytes ?? 0;
            return $"settings {ToMiB(s.SettingsOverrideBytes):F1} MiB | log {ToMiB(logBytes):F1} MiB | logger_db {ToMiB(s.LoggerSqliteBytes):F1} MiB | readings_db {ToMiB(s.ReadingsSqliteBytes):F1} MiB";
        }
    }

    static double ToMiB(long bytes) => bytes <= 0 ? 0 : bytes / (1024.0 * 1024.0);

    public string CapturedUtcLocalizedDisplay =>
        Current.CapturedUtc.ToLocalTime() == DateTime.MinValue ? "--" : Current.CapturedUtc.ToLocalTime().ToString("G");

    public string PersistStatusDisplay =>
        string.IsNullOrWhiteSpace(Current.PersistStatus) ? "--" : Current.PersistStatus;

    public string PersistErrorMessageDisplay =>
        string.IsNullOrWhiteSpace(Current.PersistErrorMessage) ? "--" : Current.PersistErrorMessage!;

    public string JsonPayloadPreview
    {
        get
        {
            var s = Current.JsonPayload;
            if (string.IsNullOrWhiteSpace(s)) return "--";
            if (TryPrettyJson(s, out var pretty))
                return pretty;

            return s;
        }
    }

    public void ScrollJsonUpRequested()
    {
        ScrollJsonUp?.Invoke(this, EventArgs.Empty);
    }

    public void ScrollJsonDownRequested()
    {
        ScrollJsonDown?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ScrollJsonUp;
    public event EventHandler? ScrollJsonDown;

    static bool TryPrettyJson(string input, out string pretty)
    {
        pretty = string.Empty;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(input);
            pretty = System.Text.Json.JsonSerializer.Serialize(
                doc.RootElement,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    public MetricsOneViewModel(
        IMetricsLatestSnapshot latest,
        IInstanceIdentifier iInstanceIdentifier
    )
    {
        ArgumentNullException.ThrowIfNull(latest);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);
        _latest = latest;
        _iInstanceIdentifier = iInstanceIdentifier;

        _current = _latest.Current;
        _structured = _latest.CurrentStructured;

        _timer = new ThreadingTimer(
            _ => MainThread.BeginInvokeOnMainThread(
                () => {
                    Current = _latest.Current;
                    Structured = _latest.CurrentStructured;
                }
            ),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1)
        );
    }

    public void Dispose()
    {
        try { _timer.Dispose(); } catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
