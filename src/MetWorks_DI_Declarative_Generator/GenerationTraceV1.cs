namespace MetWorks.DI.Declarative.Generator;

using MetWorks.DI.Declarative.Syntax.Models;

/// <summary>
/// Builds stable, line-oriented verbose trace output for DDI generation.
/// Intended for console output (E1) and unit testing without snapshotting full output.
/// </summary>
public static class GenerationTraceV1
{
    /// <summary>
    /// Builds verbose trace lines (v1) for a loaded DDI model.
    /// </summary>
    public static IReadOnlyList<string> BuildLines(Model model, string yamlPath, string outputDir)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(yamlPath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(yamlPath));

        if (string.IsNullOrWhiteSpace(outputDir))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(outputDir));

        var lines = new List<string>();

        lines.Add("DDI_TRACE v=1 mode=verbose");
        lines.Add($"DDI_TRACE yaml={Quote(yamlPath)} out={Quote(outputDir)}");

        var namespaceCount = model.Namespaces.Count;
        var classCount = model.Namespaces.Sum(n => n.Classes.Count);
        var instanceCount = model.Instances.Count;
        var assignmentCount = model.Instances.Sum(i => i.Assignments.Count);
        var elementCount = model.Instances.Sum(i => i.Elements.Count);

        lines.Add($"DDI_TRACE counts namespaces={namespaceCount} classes={classCount} instances={instanceCount} assignments={assignmentCount} elements={elementCount}");

        var orderedInstances = InstanceDependencySorter.Sort(model.Instances);

        for (var i = 0; i < orderedInstances.Count; i++)
        {
            var instance = orderedInstances[i];

            lines.Add(
                $"DDI_ORDER idx={i + 1:000} " +
                $"name={instance.InstanceName} " +
                $"class={instance.ClassQualified} " +
                $"iface={instance.InterfaceQualified ?? "<null>"} " +
                $"exposeToMauiDi={BoolToLowerInvariant(instance.ExposeToMauiDi)} " +
                $"hasElements={BoolToLowerInvariant(instance.HasElements)} " +
                $"hasAssignments={BoolToLowerInvariant(instance.HasAssignments)}");
        }

        foreach (var instance in orderedInstances)
        {
            var depsAwait = GetInitializationDependencies(model, instance);

            lines.Add(
                $"DDI_INIT name={instance.InstanceName} " +
                $"create=new() " +
                $"init={(instance.HasAssignments ? "InitializeAsync" : "<none>")} " +
                $"depsAwait=[{string.Join(',', depsAwait)}] " +
                $"exposeToMauiDi={BoolToLowerInvariant(instance.ExposeToMauiDi)}");
        }

        foreach (var instance in orderedInstances)
        {
            foreach (var assignment in instance.Assignments)
            {
                var source = assignment.Literal is not null
                    ? "literal"
                    : (!string.IsNullOrWhiteSpace(assignment.Instance) ? "instance" : "<none>");

                var value = assignment.Literal is not null
                    ? assignment.Literal
                    : (!string.IsNullOrWhiteSpace(assignment.InstancePropertyPath)
                        ? $"{assignment.Instance}.{assignment.InstancePropertyPath}"
                        : assignment.Instance);

                var expr = ExtractExpression(assignment.InitializerParameterAssignmentClause);

                var line = assignment.Location is null ? "?" : (assignment.Location.LineZeroBased + 1).ToString();
                var col = assignment.Location is null ? "?" : (assignment.Location.ColumnZeroBased + 1).ToString();
                var path = assignment.Location?.LogicalPath ?? "unknown";

                lines.Add(
                    $"DDI_BIND instance={instance.InstanceName} " +
                    $"param={assignment.Name} " +
                    $"paramType={assignment.ParameterType ?? "<null>"} " +
                    $"source={source} " +
                    $"value={value ?? "<null>"} " +
                    $"expr={Quote(expr ?? string.Empty)} " +
                    $"line={line} col={col} path={Quote(path)}");
            }
        }

        return lines;
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

    private static string? ExtractExpression(string? initializerParameterAssignmentClause)
    {
        if (string.IsNullOrWhiteSpace(initializerParameterAssignmentClause))
            return null;

        var idx = initializerParameterAssignmentClause.IndexOf(':', StringComparison.Ordinal);
        if (idx < 0)
            return initializerParameterAssignmentClause.Trim();

        return initializerParameterAssignmentClause[(idx + 1)..].Trim();
    }

    private static string Quote(string value)
        => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string BoolToLowerInvariant(bool value)
        => value ? "true" : "false";
}
