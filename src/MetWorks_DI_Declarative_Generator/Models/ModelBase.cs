namespace MetWorks.DI.Declarative.Generator.Models;
public record ModelBase : IModelBase
{
    public required string GenerationTimeRoundTripUtc { get; init; } = DateTime.UtcNow.ToString("o");
    public required string TemplateRequested { get; init; }
    public required string Namespace { get; init; }
    public required string ContainerClass { get; init; }
}