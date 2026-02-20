namespace MetWorks.DI.Declarative.Generator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MetWorks.DI.Declarative.Diagnostics;
using MetWorks.DI.Declarative.Syntax.Models;

public static class InitializerSignatureValidator
{
    public static IReadOnlyList<Diagnostic> Validate(Model model, IEnumerable<string> referenceAssemblyPaths)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(referenceAssemblyPaths);

        if (model.CodeGen is null)
            return Array.Empty<Diagnostic>();

        var initializerName = model.CodeGen.Initializer;
        if (string.IsNullOrWhiteSpace(initializerName))
            return Array.Empty<Diagnostic>();

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

        // Ensure core runtime assemblies are resolvable in the metadata context.
        AddReferenceIfMissing(references, typeof(object).Assembly.Location);
        AddReferenceIfMissing(references, typeof(Task).Assembly.Location);
        AddReferenceIfMissing(references, typeof(CancellationToken).Assembly.Location);

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
                // Ignore non-.NET binaries or load failures; other references may still allow resolution.
            }
        }

        var diagnostics = new List<Diagnostic>();

        foreach (var instance in model.Instances)
        {
            if (!instance.HasAssignments)
                continue;

            var concreteTypeName = instance.ClassQualified;
            if (string.IsNullOrWhiteSpace(concreteTypeName))
                continue;

            var type = ResolveType(loadedAssemblies, concreteTypeName!);
            if (type is null)
            {
                var expectedCall = BuildYamlCallSignature(model, instance, concreteTypeName!, initializerName);
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.InitializerSignatureTypeNotFound,
                    message: $"Initializer signature validation could not resolve type '{concreteTypeName}' for instance '{instance.InstanceName}'.{Environment.NewLine}{expectedCall}",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            var candidateMethods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => string.Equals(m.Name, initializerName, StringComparison.Ordinal))
                .ToList();

            if (candidateMethods.Count == 0)
            {
                var expectedCall = BuildYamlCallSignature(model, instance, concreteTypeName!, initializerName);
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.InitializerSignatureMethodNotFound,
                    message: $"Initializer signature validation could not find method '{initializerName}' on type '{concreteTypeName}' for instance '{instance.InstanceName}'.{Environment.NewLine}{expectedCall}",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            var expectedParameterNames = instance.Assignments
                .Select(a => a.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .ToHashSet(StringComparer.Ordinal);

            // Prefer an overload that contains all YAML-assigned parameter names.
            var matchingMethods = candidateMethods
                .Where(m => expectedParameterNames.IsSubsetOf(m.GetParameters().Select(p => p.Name ?? string.Empty)))
                .ToList();

            MethodInfo method;
            if (matchingMethods.Count == 1)
                method = matchingMethods[0];
            else if (matchingMethods.Count == 0 && candidateMethods.Count == 1)
                method = candidateMethods[0];
            else
            {
                var expectedCall = BuildYamlCallSignature(model, instance, concreteTypeName!, initializerName);
                var candidates = string.Join(
                    Environment.NewLine,
                    candidateMethods.Select(m => $"- {BuildMethodSignature(m, concreteTypeName!)}"));

                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.InitializerSignatureMethodAmbiguous,
                    message: $"Initializer signature validation found multiple '{initializerName}' overloads on type '{concreteTypeName}' for instance '{instance.InstanceName}'.{Environment.NewLine}{expectedCall}{Environment.NewLine}Candidates:{Environment.NewLine}{candidates}",
                    location: instance.Location,
                    logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                continue;
            }

            var parameters = method.GetParameters();
            var parametersByName = parameters
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .ToDictionary(p => p.Name!, p => p, StringComparer.Ordinal);

            foreach (var assignment in instance.Assignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.Name))
                    continue;

                if (!parametersByName.TryGetValue(assignment.Name!, out var parameterInfo))
                {
                    var expectedCall = BuildYamlCallSignature(model, instance, concreteTypeName!, initializerName);
                    var actualSignature = BuildMethodSignature(method, concreteTypeName!);
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.InitializerSignatureParameterMissing,
                        message: $"Initializer signature validation: method '{concreteTypeName}.{initializerName}(...)' is missing parameter '{assignment.Name}'.{Environment.NewLine}{expectedCall}{Environment.NewLine}Actual:   {actualSignature}",
                        location: assignment.Location,
                        logicalPath: assignment.Location?.LogicalPath ?? instance.Location?.LogicalPath ?? "/instances/instance"));
                    continue;
                }

                var yamlParameter = model.ParameterDictionary.GetValueOrDefault($"{concreteTypeName}.{assignment.Name}");
                if (yamlParameter is null)
                    continue;

                var expectedTypeName = (yamlParameter.InterfaceQualified ?? yamlParameter.ClassQualified) ?? string.Empty;
                var expectedIsArray = yamlParameter.IsArray;
                var expectedIsNullable = yamlParameter.IsNullable;

                if (!IsTypeMatch(parameterInfo.ParameterType, expectedTypeName, expectedIsArray, expectedIsNullable))
                {
                    var expectedCall = BuildYamlCallSignature(model, instance, concreteTypeName!, initializerName);
                    var actualSignature = BuildMethodSignature(method, concreteTypeName!);
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.InitializerSignatureParameterTypeMismatch,
                        message: $"Initializer signature validation: parameter '{assignment.Name}' expected '{FormatExpectedType(expectedTypeName, expectedIsArray, expectedIsNullable)}' but was '{FormatActualType(parameterInfo.ParameterType)}' on '{concreteTypeName}.{initializerName}'.{Environment.NewLine}{expectedCall}{Environment.NewLine}Actual:   {actualSignature}",
                        location: assignment.Location,
                        logicalPath: assignment.Location?.LogicalPath ?? instance.Location?.LogicalPath ?? "/instances/instance"));
                }
            }

            foreach (var p in parameters)
            {
                if (string.IsNullOrWhiteSpace(p.Name))
                    continue;

                if (p.IsOptional)
                    continue;

                if (!expectedParameterNames.Contains(p.Name!))
                {
                    var expectedCall = BuildYamlCallSignature(model, instance, concreteTypeName!, initializerName);
                    var actualSignature = BuildMethodSignature(method, concreteTypeName!);
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.InitializerSignatureMissingRequiredParameter,
                        message: $"Initializer signature validation: required parameter '{p.Name}' on '{concreteTypeName}.{initializerName}' is missing from YAML assignments.{Environment.NewLine}{expectedCall}{Environment.NewLine}Actual:   {actualSignature}",
                        location: instance.Location,
                        logicalPath: instance.Location?.LogicalPath ?? "/instances/instance"));
                }
            }
        }

        return diagnostics;
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

    private static bool IsTypeMatch(Type actualType, string expectedTypeName, bool expectedIsArray, bool expectedIsNullable)
    {
        if (string.IsNullOrWhiteSpace(expectedTypeName))
            return true;

        // Value-type narrowing: YAML expects nullable but method is non-nullable.
        // This can cause compile-time failures if YAML supplies null.
        if (expectedIsNullable
            && actualType.IsValueType
            && !(actualType.IsGenericType
                 && actualType.GetGenericTypeDefinition().FullName is not null
                 && string.Equals(actualType.GetGenericTypeDefinition().FullName, "System.Nullable`1", StringComparison.Ordinal)))
        {
            return false;
        }

        if (expectedIsArray)
        {
            if (!actualType.IsArray)
                return false;

            actualType = actualType.GetElementType() ?? actualType;
        }
        else if (actualType.IsArray)
        {
            return false;
        }

        if (actualType.IsGenericType
            && actualType.GetGenericTypeDefinition().FullName is not null
            && string.Equals(actualType.GetGenericTypeDefinition().FullName, "System.Nullable`1", StringComparison.Ordinal))
        {
            var underlying = actualType.GetGenericArguments()[0];

            // Allow YAML `T` to match C# `T?` (widening). This avoids false positives for optional value-type config.
            var underlyingName = (underlying.FullName ?? underlying.Name).Replace('+', '.');
            if (string.Equals(underlyingName, expectedTypeName, StringComparison.Ordinal))
                return true;

            if (!expectedTypeName.Contains('.', StringComparison.Ordinal) && string.Equals(underlying.Name, expectedTypeName, StringComparison.Ordinal))
                return true;
        }

        // Normalize nested types for comparison (metadata uses '+').
        var actualName = (actualType.FullName ?? actualType.Name).Replace('+', '.');

        if (string.Equals(actualName, expectedTypeName, StringComparison.Ordinal))
            return true;

        // If YAML uses an unqualified name, match on simple type name.
        if (!expectedTypeName.Contains('.', StringComparison.Ordinal))
            return string.Equals(actualType.Name, expectedTypeName, StringComparison.Ordinal);

        return false;
    }

    private static string FormatExpectedType(string expectedTypeName, bool expectedIsArray, bool expectedIsNullable)
    {
        var t = expectedIsArray ? $"{expectedTypeName}[]" : expectedTypeName;
        return expectedIsNullable ? $"{t}?" : t;
    }

    private static string FormatActualType(Type actualType) => FormatTypeDisplay(actualType);

    private static string BuildYamlCallSignature(Model model, Instance instance, string concreteTypeName, string initializerName)
    {
        var parameters = instance.Assignments
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a =>
            {
                var name = a.Name!;
                var yamlParam = model.ParameterDictionary.GetValueOrDefault($"{concreteTypeName}.{name}");
                if (yamlParam is null)
                    return $"<unknown> {name}";

                var typeName = (yamlParam.InterfaceQualified ?? yamlParam.ClassQualified) ?? "<unknown>";
                return $"{FormatExpectedType(typeName, yamlParam.IsArray, yamlParam.IsNullable)} {name}";
            });

        return $"Expected: {concreteTypeName}.{initializerName}({string.Join(", ", parameters)})";
    }

    private static string BuildMethodSignature(MethodInfo method, string concreteTypeName)
    {
        var parameters = method
            .GetParameters()
            .Select(p => $"{FormatTypeDisplay(p.ParameterType)} {p.Name}");

        return $"{concreteTypeName}.{method.Name}({string.Join(", ", parameters)})";
    }

    private static string FormatTypeDisplay(Type type)
    {
        if (type.IsArray)
        {
            var element = type.GetElementType() ?? type;
            return $"{FormatTypeDisplay(element)}[]";
        }

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            var defFullName = def.FullName ?? def.Name;

            if (string.Equals(defFullName, "System.Nullable`1", StringComparison.Ordinal))
            {
                var arg = type.GetGenericArguments()[0];
                return $"{FormatTypeDisplay(arg)}?";
            }

            var baseName = def.Name;
            var tickIndex = baseName.IndexOf('`');
            if (tickIndex >= 0)
                baseName = baseName[..tickIndex];

            var prefix = string.IsNullOrWhiteSpace(def.Namespace) ? baseName : $"{def.Namespace}.{baseName}";
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeDisplay));
            return $"{prefix}<{args}>";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static void AddReferenceIfMissing(List<string> references, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!references.Contains(path, StringComparer.OrdinalIgnoreCase))
            references.Add(path);
    }
}
