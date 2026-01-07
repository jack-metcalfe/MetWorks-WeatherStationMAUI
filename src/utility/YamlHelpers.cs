namespace Utility;
public static class YamlHelpers
{
    /// <summary>
    /// Parse a Stream containing YAML and return the root document as a YamlMappingNode.
    /// Throws an informative exception for empty input, non-mapping root, or malformed YAML.
    /// </summary>
    public static YamlMappingNode ParseStreamToMappingNode(Stream yamlStream, string? sourcePath = null)
    {
        if (yamlStream is null) throw new ArgumentNullException(nameof(yamlStream));
        using var reader = new StreamReader(yamlStream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var yaml = new YamlStream();
        try
        {
            yaml.Load(reader);
        }
        catch (YamlDotNet.Core.YamlException ye)
        {
            var src = sourcePath is null ? "stream" : sourcePath;
            throw new InvalidDataException($"Failed to parse YAML from {src}: {ye.Message}", ye);
        }

        if (yaml.Documents == null || yaml.Documents.Count == 0)
            throw new InvalidDataException("YAML stream contains no documents.");

        var doc = yaml.Documents[0];
        if (doc.RootNode is null)
            throw new InvalidDataException("YAML document has no root node.");

        if (doc.RootNode is YamlMappingNode mapping)
            return mapping;

        // If root is a sequence and you expect a mapping under a top-level key,
        // you might decide to create an empty mapping or throw. Here we throw.
        var rootType = doc.RootNode.GetType().Name;
        var srcName = sourcePath ?? "stream";
        throw new InvalidDataException($"Expected YAML root to be a mapping node in {srcName} but got {rootType}.");
    }

    /// <summary>
    /// Try-parse variant that returns false on failure and an error message.
    /// The stream position will be advanced as the StreamReader reads; caller may rewind if needed.
    /// </summary>
    public static bool TryParseStreamToMappingNode(Stream yamlStream, out YamlMappingNode? mappingNode, out string? error, string? sourcePath = null)
    {
        mappingNode = null;
        error = null;
        try
        {
            mappingNode = ParseStreamToMappingNode(yamlStream, sourcePath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
