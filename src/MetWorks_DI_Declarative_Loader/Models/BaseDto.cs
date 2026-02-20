namespace MetWorks.DI.Declarative.Syntax.Models;
public record BaseDto
{
    public Location? Location { get; }
    public bool IsValid => Diagnostics.Count == 0;
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; }
    public BaseDto(
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics = null
    )
    {
        Location = location;
        Diagnostics = diagnostics ?? new List<Diagnostic>();
    }
}
