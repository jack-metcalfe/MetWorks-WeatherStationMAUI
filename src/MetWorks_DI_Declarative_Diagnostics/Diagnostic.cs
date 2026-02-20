namespace MetWorks.DI.Declarative.Diagnostics;
public sealed record Diagnostic
{
    public DiagnosticCode DiagnosticCode { get; }
    public string? Message { get; }
    public Location? Location { get; }

    public Diagnostic(
        DiagnosticCode diagnosticCode,
        string? message = null,
        Location? location = null,
        string logicalPath = @"unknown")
    {
        DiagnosticCode = diagnosticCode;
        Message = message ?? DiagnosticCodeInfo.GetDefaultMessage(diagnosticCode) ?? diagnosticCode.ToString();
        Location = location ?? new Location(0, 0, logicalPath);
    }
}
