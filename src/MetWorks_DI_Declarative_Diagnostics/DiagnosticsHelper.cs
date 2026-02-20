namespace MetWorks.DI.Declarative.Diagnostics;
public static class DiagnosticsHelper
{
    // Canonical provenanceStack-aware Create (no severity override).
    public static Diagnostic Create(
        DiagnosticCode diagnosticCode, 
        string? diagnosticMessage = null, 
        Location? location = null
    )
    {
        return new Diagnostic(diagnosticCode, diagnosticMessage, location);
    }

    // Add to a collection (ICollection<T> is intentionally broad).
    public static List<Diagnostic> Add(
        this List<Diagnostic> list, 
        DiagnosticCode diagnosticCode, 
        string? message = null, 
        Location? location = null
    )
    {
        list.Add(Create(diagnosticCode, message, location));
        return list;
    }
}