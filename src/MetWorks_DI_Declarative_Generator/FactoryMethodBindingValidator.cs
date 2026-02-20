namespace MetWorks.DI.Declarative.Generator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MetWorks.DI.Declarative.Diagnostics;
using MetWorks.DI.Declarative.Syntax.Models;

public static class FactoryMethodBindingValidator
{
    public static IReadOnlyList<Diagnostic> Validate(Model model, IEnumerable<string> referenceAssemblyPaths)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(referenceAssemblyPaths);

        var references = referenceAssemblyPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Add platform assemblies to improve resolution when the caller provides only project outputs.
        // NOTE: We must deduplicate by assembly simple-name to avoid MetadataLoadContext load collisions.
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrWhiteSpace(tpa))
        {
            foreach (var p in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                AddReferenceIfMissing(references, p);
        }

        AddReferenceIfMissing(references, typeof(object).Assembly.Location);

        references = DeduplicateByAssemblySimpleName(references);

        var resolver = new PathAssemblyResolver(references);
        using var mlc = new MetadataLoadContext(resolver, coreAssemblyName: "System.Private.CoreLib");

        var loadedAssemblies = new List<Assembly>(capacity: references.Count);
        foreach (var path in references)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                loadedAssemblies.Add(mlc.LoadFromAssemblyPath(path));
            }
            catch
            {
            }
        }

        var diagnostics = new List<Diagnostic>();

        foreach (var instance in model.Instances)
        {
            if (!instance.HasFactoryMethodBinding)
                continue;

            if (string.IsNullOrWhiteSpace(instance.FactoryInstanceName))
                continue;

            if (!model.InstanceDictionary.TryGetValue(instance.FactoryInstanceName!, out var factoryInstance))
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.FactoryBindingInstanceNotFound,
                    message: $"Factory binding for instance '{instance.InstanceName}' references factoryInstance '{instance.FactoryInstanceName}' which was not found.",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            var factoryTypeName = factoryInstance.ClassQualified;
            if (string.IsNullOrWhiteSpace(factoryTypeName))
                continue;

            var factoryType = ResolveType(loadedAssemblies, factoryTypeName!);
            if (factoryType is null)
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.FactoryBindingTypeNotFound,
                    message: $"Factory binding validation could not resolve factory type '{factoryTypeName}' for factoryInstance '{instance.FactoryInstanceName}' (used by '{instance.InstanceName}').",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            var methodName = instance.FactoryMethodName;
            if (string.IsNullOrWhiteSpace(methodName))
                continue;

            var candidateMethods = factoryType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
                .ToList();

            if (candidateMethods.Count == 0)
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.FactoryBindingMethodNotFound,
                    message: $"Factory binding validation could not find method '{methodName}' on factory type '{factoryTypeName}' for instance '{instance.InstanceName}'.",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            if (candidateMethods.Count > 1)
            {
                var candidates = string.Join(
                    Environment.NewLine,
                    candidateMethods.Select(m => $"- {BuildMethodSignature(m, factoryTypeName!)}"));

                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.FactoryBindingMethodAmbiguous,
                    message: $"Factory binding validation found multiple '{methodName}' overloads on '{factoryTypeName}' for instance '{instance.InstanceName}'. Candidates:{Environment.NewLine}{candidates}",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            var method = candidateMethods[0];
            if (method.GetParameters().Length != 0)
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.FactoryBindingMethodHasParameters,
                    message: $"Factory binding method '{factoryTypeName}.{methodName}(...)' must be parameterless for instance '{instance.InstanceName}'.",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            var expectedReturn = instance.InstanceIsArray
                ? $"{instance.ClassQualified}[]"
                : instance.ClassQualified;

            var actualReturn = method.ReturnType.FullName;
            if (!string.Equals(actualReturn, expectedReturn, StringComparison.Ordinal))
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.FactoryBindingReturnTypeMismatch,
                    message: $"Factory binding method '{factoryTypeName}.{methodName}()' return type mismatch for instance '{instance.InstanceName}'. Expected '{expectedReturn}' but was '{actualReturn}'.",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
            }
        }

        return diagnostics;
    }

    private static void AddReferenceIfMissing(List<string> references, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (references.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
            return;

        references.Add(path);
    }

    private static List<string> DeduplicateByAssemblySimpleName(List<string> references)
    {
        var hasCoreLib = references.Any(p => string.Equals(Path.GetFileNameWithoutExtension(p), "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = new List<string>(references.Count);

        foreach (var path in references)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (hasCoreLib && string.Equals(name, "mscorlib", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!seen.Add(name))
                continue;

            deduped.Add(path);
        }

        return deduped;
    }

    private static Type? ResolveType(IReadOnlyList<Assembly> assemblies, string fullName)
    {
        foreach (var asm in assemblies)
        {
            var t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
            if (t is not null)
                return t;
        }

        return null;
    }

    private static string BuildMethodSignature(MethodInfo method, string declaringType)
    {
        var parms = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"));
        return $"{declaringType}.{method.Name}({parms}) : {method.ReturnType.FullName}";
    }
}
