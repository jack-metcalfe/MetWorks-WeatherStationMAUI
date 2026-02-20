namespace MetWorks.DI.Declarative.Syntax;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MetWorks.DI.Declarative.EnumDefinitions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

public static class YamlFormatter
{
    public static string Format(string yamlText)
    {
        ArgumentNullException.ThrowIfNull(yamlText);

        var stream = new YamlStream();

        // Note: RepresentationModel preserves mapping/sequence order as parsed.
        // Comments are preserved when present on nodes and emitted by Save().
        using (var reader = new StringReader(yamlText))
        {
            stream.Load(reader);
        }

        foreach (var doc in stream.Documents)
        {
            if (doc.RootNode is not null)
                NormalizeNodeStyles(doc.RootNode);
        }

        var sb = new StringBuilder(capacity: Math.Max(256, yamlText.Length));
        using (var writer = new StringWriter(sb))
        {
            // `assignAnchors: false` avoids adding anchors/aliases when not needed.
            stream.Save(writer, assignAnchors: false);
        }

        return sb.ToString();
    }

    public static string SortInstancesByDependency(string yamlText)
    {
        ArgumentNullException.ThrowIfNull(yamlText);

        var stream = new YamlStream();
        using (var reader = new StringReader(yamlText))
        {
            stream.Load(reader);
        }

        foreach (var doc in stream.Documents)
        {
            if (doc.RootNode is not YamlMappingNode root)
                continue;

            TrySortInstancesInPlace(root);
            NormalizeNodeStyles(root);
        }

        var sb = new StringBuilder(capacity: Math.Max(256, yamlText.Length));
        using (var writer = new StringWriter(sb))
        {
            stream.Save(writer, assignAnchors: false);
        }

        return sb.ToString();
    }

    public static bool IsFormatted(string yamlText)
    {
        ArgumentNullException.ThrowIfNull(yamlText);

        var formatted = Format(yamlText);
        return string.Equals(NormalizeNewlines(formatted), NormalizeNewlines(yamlText), StringComparison.Ordinal);
    }

    private static string NormalizeNewlines(string s)
        => s.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static void NormalizeNodeStyles(YamlNode node)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                // Allow the emitter to choose the minimal representation.
                scalar.Style = ScalarStyle.Any;
                return;

            case YamlSequenceNode sequence:
                sequence.Style = SequenceStyle.Block;
                foreach (var child in sequence.Children)
                    NormalizeNodeStyles(child);
                return;

            case YamlMappingNode mapping:
                mapping.Style = MappingStyle.Block;
                foreach (var kvp in mapping.Children)
                {
                    NormalizeNodeStyles(kvp.Key);
                    NormalizeNodeStyles(kvp.Value);
                }
                return;

            default:
                return;
        }
    }

    private static bool TrySortInstancesInPlace(YamlMappingNode root)
    {
        var instanceKey = Models.Schema.TokenTypeToName[TokenTypes.instances];
        var instanceNameKey = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceName];
        var assignmentKey = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignments];
        var assignmentInstanceKey = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstance];
        var elementKey = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceElement];
        var elementInstanceKey = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceElementInstance];
        var factoryInstanceKey = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceFactoryInstance];

        var instanceSequence = GetChildSequence(root, instanceKey);
        if (instanceSequence is null || instanceSequence.Children.Count <= 1)
            return false;

        var items = new List<(int Index, string Name, YamlMappingNode Node)>(instanceSequence.Children.Count);
        for (var i = 0; i < instanceSequence.Children.Count; i++)
        {
            if (instanceSequence.Children[i] is not YamlMappingNode mapping)
                continue;

            var name = GetChildScalarValue(mapping, instanceNameKey);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            items.Add((i, name!, mapping));
        }

        if (items.Count <= 1)
            return false;

        var itemByName = items
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

        // Build adjacency where edge: dep -> dependent
        var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            outgoing[item.Name] = new List<string>();
            inDegree[item.Name] = 0;
        }

        foreach (var item in items)
        {
            var deps = new HashSet<string>(StringComparer.Ordinal);

            // factoryInstance dependency
            var factoryInstance = GetChildScalarValue(item.Node, factoryInstanceKey);
            AddDependencyIfInstance(deps, itemByName, factoryInstance);

            // assignment instance dependencies
            var assignments = GetChildSequence(item.Node, assignmentKey);
            if (assignments is not null)
            {
                foreach (var assignmentNode in assignments.Children.OfType<YamlMappingNode>())
                {
                    var referenced = GetChildScalarValue(assignmentNode, assignmentInstanceKey);
                    AddDependencyIfInstance(deps, itemByName, referenced);
                }
            }

            // element instance dependencies
            var elements = GetChildSequence(item.Node, elementKey);
            if (elements is not null)
            {
                foreach (var elementNode in elements.Children.OfType<YamlMappingNode>())
                {
                    var referenced = GetChildScalarValue(elementNode, elementInstanceKey);
                    AddDependencyIfInstance(deps, itemByName, referenced);
                }
            }

            foreach (var dep in deps)
            {
                outgoing[dep].Add(item.Name);
                inDegree[item.Name]++;
            }
        }

        // Stable Kahn: queue ordered by original index
        var ready = new SortedSet<(int Index, string Name)>(Comparer<(int Index, string Name)>.Create(
            (a, b) => a.Index != b.Index ? a.Index.CompareTo(b.Index) : string.CompareOrdinal(a.Name, b.Name)));

        foreach (var item in items)
        {
            if (inDegree[item.Name] == 0)
                ready.Add((item.Index, item.Name));
        }

        var sortedNames = new List<string>(items.Count);
        while (ready.Count > 0)
        {
            var next = ready.Min;
            ready.Remove(next);

            sortedNames.Add(next.Name);

            foreach (var dependent in outgoing[next.Name])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    ready.Add((itemByName[dependent].Index, dependent));
            }
        }

        if (sortedNames.Count != items.Count)
        {
            // Cycle or missing nodes; do not reorder.
            return false;
        }

        var oldOrder = items.OrderBy(i => i.Index).Select(i => i.Name).ToArray();
        if (oldOrder.SequenceEqual(sortedNames, StringComparer.Ordinal))
            return false;

        var newChildren = new List<YamlNode>(instanceSequence.Children.Count);
        foreach (var name in sortedNames)
        {
            newChildren.Add(itemByName[name].Node);
        }

        // Preserve any non-mapping children (shouldn't exist), appending after sorted mappings.
        foreach (var child in instanceSequence.Children)
        {
            if (child is not YamlMappingNode)
                newChildren.Add(child);
        }

        instanceSequence.Children.Clear();
        foreach (var child in newChildren)
            instanceSequence.Children.Add(child);

        return true;
    }

    private static void AddDependencyIfInstance(HashSet<string> deps, Dictionary<string, (int Index, string Name, YamlMappingNode Node)> itemByName, string? referenced)
    {
        if (string.IsNullOrWhiteSpace(referenced))
            return;

        var instanceName = referenced.Split('.', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        if (string.IsNullOrWhiteSpace(instanceName))
            return;

        if (itemByName.ContainsKey(instanceName))
            deps.Add(instanceName);
    }

    private static YamlSequenceNode? GetChildSequence(YamlMappingNode mapping, string key)
    {
        foreach (var kvp in mapping.Children)
        {
            if (kvp.Key is YamlScalarNode k && string.Equals(k.Value, key, StringComparison.Ordinal))
                return kvp.Value as YamlSequenceNode;
        }

        return null;
    }

    private static string? GetChildScalarValue(YamlMappingNode mapping, string key)
    {
        foreach (var kvp in mapping.Children)
        {
            if (kvp.Key is YamlScalarNode k && string.Equals(k.Value, key, StringComparison.Ordinal))
                return (kvp.Value as YamlScalarNode)?.Value;
        }

        return null;
    }
}
