namespace MetWorks.DI.Declarative.Generator;

using MetWorks.DI.Declarative.Diagnostics;
using MetWorks.DI.Declarative.Syntax.Models;

internal static class InstanceDependencySorter
{
    public static IReadOnlyList<Instance> Sort(IReadOnlyList<Instance> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);

        var instanceByName = instances
            .Where(i => !string.IsNullOrWhiteSpace(i.InstanceName))
            .GroupBy(i => i.InstanceName!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var originalIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < instances.Count; i++)
        {
            var name = instances[i].InstanceName;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (!originalIndexByName.ContainsKey(name))
                originalIndexByName.Add(name, i);
        }

        var nodes = originalIndexByName.Keys.ToList();

        // Dependencies: instance -> referenced instances.
        var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var instance in instances)
        {
            var name = instance.InstanceName;
            if (string.IsNullOrWhiteSpace(name) || !originalIndexByName.ContainsKey(name))
                continue;

            var deps = new HashSet<string>(StringComparer.Ordinal);

            foreach (var a in instance.Assignments)
            {
                if (!string.IsNullOrWhiteSpace(a.Instance))
                    deps.Add(a.Instance!);
            }

            // F1: Factory bindings depend on the referenced factory instance (must be created first).
            if (!string.IsNullOrWhiteSpace(instance.FactoryInstanceName))
                deps.Add(instance.FactoryInstanceName!);

            foreach (var e in instance.Elements)
            {
                if (!string.IsNullOrWhiteSpace(e.Instance))
                    deps.Add(e.Instance!);
            }

            dependencies[name] = deps;
        }

        // Kahn: build indegree and reverse edges.
        var indegree = nodes.ToDictionary(n => n, _ => 0, StringComparer.Ordinal);
        var dependents = nodes.ToDictionary(n => n, _ => new List<string>(), StringComparer.Ordinal);

        foreach (var (name, deps) in dependencies)
        {
            foreach (var dep in deps)
            {
                // Unknown dependencies are handled by loader diagnostics; ignore here.
                if (!indegree.ContainsKey(dep))
                    continue;

                dependents[dep].Add(name);
                indegree[name]++;
            }
        }

        var ready = new PriorityQueue<string, (int Index, string Name)>();
        foreach (var n in nodes)
        {
            if (indegree[n] == 0)
                ready.Enqueue(n, (originalIndexByName[n], n));
        }

        var ordered = new List<string>(nodes.Count);
        while (ready.Count > 0)
        {
            var n = ready.Dequeue();
            ordered.Add(n);

            foreach (var dependent in dependents[n])
            {
                indegree[dependent]--;
                if (indegree[dependent] == 0)
                    ready.Enqueue(dependent, (originalIndexByName[dependent], dependent));
            }
        }

        if (ordered.Count != nodes.Count)
        {
            var remaining = indegree.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToHashSet(StringComparer.Ordinal);
            var cycle = FindCycle(dependencies, remaining);

            var cycleText = cycle.Count > 0
                ? string.Join(" -> ", cycle)
                : string.Join(", ", remaining.OrderBy(n => originalIndexByName[n]));

            var cycleStart = cycle.Count > 0 ? cycle[0] : remaining.First();
            var location = instanceByName.TryGetValue(cycleStart, out var inst) ? inst.Location : null;

            throw new DdiGenerationException(
                new List<Diagnostic>
                {
                    new(
                        diagnosticCode: DiagnosticCode.DependencyCycleDetected,
                        message: $"Cycle detected in DDI instance graph: {cycleText}",
                        location: location,
                        logicalPath: location?.LogicalPath ?? "/instances/instance"
                    )
                });
        }

        return ordered.Select(n => instanceByName[n]).ToList();
    }

    private static IReadOnlyList<string> FindCycle(
        IReadOnlyDictionary<string, HashSet<string>> dependencies,
        HashSet<string> remaining
    )
    {
        var state = new Dictionary<string, int>(StringComparer.Ordinal); // 0=unvisited, 1=visiting, 2=done
        var parent = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var start in remaining)
        {
            if (state.ContainsKey(start))
                continue;

            var cycle = Dfs(start);
            if (cycle.Count > 0)
                return cycle;
        }

        return Array.Empty<string>();

        List<string> Dfs(string node)
        {
            state[node] = 1;

            if (dependencies.TryGetValue(node, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (!remaining.Contains(dep))
                        continue;

                    if (!state.TryGetValue(dep, out var depState))
                    {
                        parent[dep] = node;
                        var found = Dfs(dep);
                        if (found.Count > 0)
                            return found;
                    }
                    else if (depState == 1)
                    {
                        // Found a back-edge: node -> dep
                        var cycle = new List<string> { dep };
                        var cur = node;
                        while (!string.Equals(cur, dep, StringComparison.Ordinal))
                        {
                            cycle.Add(cur);
                            if (!parent.TryGetValue(cur, out var next) || string.IsNullOrWhiteSpace(next))
                                break;

                            cur = next;
                        }

                        cycle.Add(dep);
                        cycle.Reverse();
                        return cycle;
                    }
                }
            }

            state[node] = 2;
            return new List<string>();
        }
    }
}
