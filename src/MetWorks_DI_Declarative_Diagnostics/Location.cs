namespace MetWorks.DI.Declarative.Diagnostics;
// Provenance primitives (raw, permissive)
public sealed record Location
{
    public int LineZeroBased { get; }
    public int ColumnZeroBased { get; }
    public string LogicalPath { get; init;}

    public Location(
        YamlNode yamlNode,
        string logicalPath
    )
    {
        LineZeroBased = yamlNode.Start.Line;
        ColumnZeroBased = yamlNode.Start.Column;
        LogicalPath = logicalPath;
    }
    public Location(
        YamlException yamlException,
        string logicalPath
    )
    {
        LineZeroBased = yamlException.Start.Line;
        ColumnZeroBased = yamlException.Start.Column;
        LogicalPath = logicalPath;
    }
    public Location(
        int lineZeroBased,
        int columnZeroBased,
        string logicalPath
    )
    {
        LineZeroBased = lineZeroBased;
        ColumnZeroBased = columnZeroBased;
        LogicalPath = logicalPath;
    }
}
