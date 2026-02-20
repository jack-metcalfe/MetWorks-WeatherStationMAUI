namespace MetWorks.DI.Declarative.Generator;

using System.Text;
using MetWorks.DI.Declarative.Syntax.Models;

/// <summary>
/// Builds a deterministic generation report (E2) from a loaded DDI model.
/// </summary>
public static class GenerationReportV1
{
    public static string BuildMarkdown(Model model, string yamlPath, string outputDir)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(yamlPath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(yamlPath));

        if (string.IsNullOrWhiteSpace(outputDir))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(outputDir));

        var orderedInstances = InstanceDependencySorter.Sort(model.Instances);

        var used = UsedModelEntries.Build(model);

        var sb = new StringBuilder();
        sb.AppendLine("# DDI generation report");
        sb.AppendLine();
        sb.AppendLine($"- YAML: `{yamlPath}`");
        sb.AppendLine($"- Output: `{outputDir}`");
        sb.AppendLine();

        WriteInitializerCallGraph(sb, model, orderedInstances);
        sb.AppendLine();
        WriteUnusedModelEntries(sb, model, used);

        return sb.ToString();
    }

    private static void WriteInitializerCallGraph(StringBuilder sb, Model model, IReadOnlyList<Instance> orderedInstances)
    {
        sb.AppendLine("## Initializer call graph (deterministic)");
        sb.AppendLine();
        sb.AppendLine("This describes the async initialization gates and the awaited dependencies between initializers.");
        sb.AppendLine();

        sb.AppendLine("- `Registry.InitializeAllAsync()`");

        foreach (var instance in orderedInstances)
        {
            if (!instance.HasAssignments)
                continue;

            sb.AppendLine($"  - `Registry.When{instance.InstanceName}InitializedAsync()`");
        }

        foreach (var instance in orderedInstances)
        {
            if (!instance.HasAssignments)
                continue;

            sb.AppendLine();
            sb.AppendLine($"- `Registry.When{instance.InstanceName}InitializedAsync()`");
            sb.AppendLine($"  - `{instance.InstanceName}_Initializer.Initialize_{instance.InstanceName}Async(registry)`");

            var deps = GetInitializationDependencies(model, instance);
            if (deps.Count > 0)
            {
                sb.AppendLine("  - awaits:");
                foreach (var dep in deps)
                    sb.AppendLine($"    - `Registry.When{dep}InitializedAsync()`");
            }
        }
    }

    private static IReadOnlyList<string> GetInitializationDependencies(Model model, Instance instance)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assignment in instance.Assignments)
        {
            var depName = assignment.Instance;
            if (string.IsNullOrWhiteSpace(depName))
                continue;

            if (string.Equals(depName, instance.InstanceName, StringComparison.Ordinal))
                continue;

            if (model.InstanceDictionary.TryGetValue(depName, out var depInstance) && depInstance.HasAssignments)
            {
                if (seen.Add(depName))
                    result.Add(depName);
            }
        }

        return result;
    }

    private static void WriteUnusedModelEntries(StringBuilder sb, Model model, UsedModelEntries used)
    {
        sb.AppendLine("## Unused namespace model entries");
        sb.AppendLine();
        sb.AppendLine("Entries are considered used if referenced by `instance:` (creation/init wiring) or by dotted property paths in assignments.");
        sb.AppendLine();

        var allClasses = model.Namespaces.SelectMany(n => n.Classes).ToList();
        var allParameters = allClasses.SelectMany(c => c.Parameters.Values).ToList();
        var allProperties = allClasses
            .SelectMany(c => c.Properties is null ? Enumerable.Empty<Property>() : c.Properties.Values)
            .ToList();

        var unusedNamespaces = model.Namespaces
            .Where(n => n.Classes.All(c => c.ClassQualified is null || !used.UsedClassQualified.Contains(c.ClassQualified)))
            .Select(n => n.NamespaceName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var unusedClasses = allClasses
            .Select(c => c.ClassQualified)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q!)
            .Where(q => !used.UsedClassQualified.Contains(q))
            .OrderBy(q => q, StringComparer.Ordinal)
            .ToList();

        var unusedParameters = allParameters
            .Select(p => p.ParameterQualified)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q!)
            .Where(q => !used.UsedParameterQualified.Contains(q))
            .OrderBy(q => q, StringComparer.Ordinal)
            .ToList();

        var unusedProperties = allProperties
            .Select(p => p.PropertyQualified)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q!)
            .Where(q => !used.UsedPropertyQualified.Contains(q))
            .OrderBy(q => q, StringComparer.Ordinal)
            .ToList();

        sb.AppendLine($"### Namespaces ({unusedNamespaces.Count} unused)");
        if (unusedNamespaces.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("(none)");
        }
        else
        {
            sb.AppendLine();
            foreach (var n in unusedNamespaces)
                sb.AppendLine($"- `{n}`");
        }

        sb.AppendLine();
        sb.AppendLine($"### Classes ({unusedClasses.Count} unused)");
        if (unusedClasses.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("(none)");
        }
        else
        {
            sb.AppendLine();
            foreach (var c in unusedClasses)
                sb.AppendLine($"- `{c}`");
        }

        sb.AppendLine();
        sb.AppendLine($"### Parameters ({unusedParameters.Count} unused)");
        if (unusedParameters.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("(none)");
        }
        else
        {
            sb.AppendLine();
            foreach (var p in unusedParameters)
                sb.AppendLine($"- `{p}`");
        }

        sb.AppendLine();
        sb.AppendLine($"### Properties ({unusedProperties.Count} unused)");
        if (unusedProperties.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("(none)");
        }
        else
        {
            sb.AppendLine();
            foreach (var p in unusedProperties)
                sb.AppendLine($"- `{p}`");
        }
    }

    private sealed record UsedModelEntries(
        HashSet<string> UsedClassQualified,
        HashSet<string> UsedParameterQualified,
        HashSet<string> UsedPropertyQualified)
    {
        public static UsedModelEntries Build(Model model)
        {
            var usedClasses = new HashSet<string>(StringComparer.Ordinal);
            var usedParameters = new HashSet<string>(StringComparer.Ordinal);
            var usedProperties = new HashSet<string>(StringComparer.Ordinal);

            foreach (var instance in model.Instances)
            {
                if (!string.IsNullOrWhiteSpace(instance.ClassQualified))
                    usedClasses.Add(instance.ClassQualified!);

                foreach (var assignment in instance.Assignments)
                {
                    if (!string.IsNullOrWhiteSpace(instance.ClassQualified) && !string.IsNullOrWhiteSpace(assignment.Name))
                        usedParameters.Add($"{instance.ClassQualified}.{assignment.Name}");

                    MarkDottedPropertyChain(model, usedClasses, usedProperties, assignment);
                }
            }

            return new UsedModelEntries(usedClasses, usedParameters, usedProperties);
        }

        private static void MarkDottedPropertyChain(
            Model model,
            HashSet<string> usedClasses,
            HashSet<string> usedProperties,
            Assignment assignment)
        {
            if (string.IsNullOrWhiteSpace(assignment.Instance))
                return;

            if (string.IsNullOrWhiteSpace(assignment.InstancePropertyPath))
                return;

            if (!model.InstanceDictionary.TryGetValue(assignment.Instance!, out var referencedInstance))
                return;

            if (string.IsNullOrWhiteSpace(referencedInstance.ClassQualified))
                return;

            var segments = assignment.InstancePropertyPath!
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length == 0)
                return;

            var currentClassQualified = referencedInstance.ClassQualified!;
            usedClasses.Add(currentClassQualified);

            foreach (var seg in segments)
            {
                if (!model.ClassDictionary.TryGetValue(currentClassQualified, out var classDto))
                    break;

                if (classDto.Properties is null || !classDto.Properties.TryGetValue(seg, out var propDto))
                    break;

                if (!string.IsNullOrWhiteSpace(propDto.PropertyQualified))
                    usedProperties.Add(propDto.PropertyQualified!);

                var nextClassQualified = propDto.ClassQualified;
                if (string.IsNullOrWhiteSpace(nextClassQualified) && !string.IsNullOrWhiteSpace(propDto.InterfaceQualified))
                {
                    nextClassQualified = model.ClassDictionary.Values
                        .Where(c => string.Equals(c.InterfaceQualified, propDto.InterfaceQualified, StringComparison.Ordinal))
                        .Select(c => c.ClassQualified)
                        .Where(q => !string.IsNullOrWhiteSpace(q))
                        .OrderBy(q => q, StringComparer.Ordinal)
                        .FirstOrDefault();
                }

                if (string.IsNullOrWhiteSpace(nextClassQualified))
                    break;

                currentClassQualified = nextClassQualified!;
                usedClasses.Add(currentClassQualified);
            }
        }
    }
}
