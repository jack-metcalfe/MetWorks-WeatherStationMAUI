namespace MetWorks.DI.Declarative.Generator;

using MetWorks.DI.Declarative.Diagnostics;

public sealed class DdiGenerationException : InvalidOperationException
{
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public DdiGenerationException(IReadOnlyList<Diagnostic> diagnostics)
        : base(BuildMessage(diagnostics))
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    private static string BuildMessage(IReadOnlyList<Diagnostic> diagnostics)
    {
        if (diagnostics is null || diagnostics.Count == 0)
            return "DDI code generation failed.";

        // Keep the exception message concise; detailed output should come from structured diagnostics.
        var first = diagnostics[0];
        return $"DDI code generation failed: {first.DiagnosticCode}: {first.Message}";
    }
}
