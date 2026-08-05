namespace MetWorks.Apps.Maui.WeatherStationMaui.UITools;

/// <summary>
/// Measures worst-case render widths for a constrained set of candidate strings.
/// Uses a live <see cref="Label"/> already attached to the visual tree so that
/// the platform renderer is available and <c>Measure</c> returns real values.
/// </summary>
internal static class TextMeasure
{
    /// <summary>
    /// Temporarily cycles <paramref name="label"/>'s text through each candidate,
    /// measures each, and returns the maximum width. The label's original text is restored.
    /// Must be called after the label is loaded (has a live renderer).
    /// </summary>
    private static double MeasureMaxWidth(Label label, IEnumerable<string> candidates)
    {
        // NOTE: setting label.Text directly inside this loop unapplies any live OneWay
        // binding on Label.TextProperty.  Callers that need to preserve bindings must
        // re-call SetBinding after all measurements are complete (see ApplyDateTimeWidths).
        double max = 0;
        foreach (var s in candidates)
        {
            label.Text = s;
            var size = label.Measure(double.PositiveInfinity, double.PositiveInfinity);
            if (size.Width > max) max = size.Width;
        }
        return max;
    }

    // ── Convenience: day-of-week abbreviations using current culture ─────────
    internal static IReadOnlyList<string> AllDaysOfWeek(string format = "ddd") =>
        Enumerable.Range(0, 7)
            .Select(i => new DateTime(2024, 1, 1).AddDays(i).ToString(format))
            .ToArray();

    // ── Convenience: one representative date per month ("MMM dd") ────────────
    // Day 28 exists in all months; month name width is the dominant factor.
    internal static IReadOnlyList<string> AllMonthDayCombos(string format = "MMM dd") =>
        Enumerable.Range(1, 12)
            .Select(m => new DateTime(2024, m, 28).ToString(format))
            .ToArray();

    // ── Convenience: HH:mm representatives ───────────────────────────────────
    // Digits in OpenSans are proportional but similar enough that a few
    // representatives cover the worst case.
    internal static IReadOnlyList<string> TimeRepresentatives() => ["00:00", "23:59", "18:38"];

    /// <summary>
    /// Fixes the <see cref="View.WidthRequest"/> on the standard date/time labels used
    /// by MainView* ContentViews. Pass <see langword="null"/> for any label the view does
    /// not contain; those slots are silently skipped.
    /// Must be called after the labels are loaded (have a live renderer).
    /// </summary>
    internal static void ApplyDateTimeWidths(Label dayOfWeek, Label date, Label time)
    {
        dayOfWeek.WidthRequest = MeasureMaxWidth(dayOfWeek, AllDaysOfWeek());
        date.WidthRequest = MeasureMaxWidth(date, AllMonthDayCombos());
        time.WidthRequest = MeasureMaxWidth(time, TimeRepresentatives());

        // MeasureMaxWidth sets label.Text directly, which unapplies any live OneWay
        // binding in MAUI.  Re-establish the bindings so the clock keeps updating.
        dayOfWeek.SetBinding(Label.TextProperty, new Binding(nameof(WeatherViewModel.TimeDayOfWeekDisplay)));
        date.SetBinding(Label.TextProperty, new Binding(nameof(WeatherViewModel.TimeDateDisplay)));
        time.SetBinding(Label.TextProperty, new Binding(nameof(WeatherViewModel.TimeDisplay)));
    }
}
