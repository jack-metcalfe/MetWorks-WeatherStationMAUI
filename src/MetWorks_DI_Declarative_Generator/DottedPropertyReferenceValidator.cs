namespace MetWorks.DI.Declarative.Generator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MetWorks.DI.Declarative.Diagnostics;
using MetWorks.DI.Declarative.Syntax.Models;

public static class DottedPropertyReferenceValidator
{
    public static IReadOnlyList<Diagnostic> Validate(Model model, IEnumerable<string> referenceAssemblyPaths)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(referenceAssemblyPaths);

        var references = referenceAssemblyPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

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
                // Ignore non-.NET binaries or load failures; other references may still allow resolution.
            }
        }

        var diagnostics = new List<Diagnostic>();

        foreach (var instance in model.Instances)
        {
            if (!instance.HasAssignments)
                continue;

            foreach (var assignment in instance.Assignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.InstancePropertyPath))
                    continue;

                var baseInstanceName = assignment.Instance;
                if (string.IsNullOrWhiteSpace(baseInstanceName))
                    continue;

                if (!model.InstanceDictionary.TryGetValue(baseInstanceName, out var baseInstance))
                    continue;

                var baseConcreteTypeName = baseInstance.ClassQualified;
                if (string.IsNullOrWhiteSpace(baseConcreteTypeName))
                    continue;

                var baseConcreteType = ResolveType(loadedAssemblies, baseConcreteTypeName);
                if (baseConcreteType is null)
                {
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.DottedPropertyTypeNotFound,
                        message: $"Dotted-property validation could not resolve type '{baseConcreteTypeName}' for instance '{baseInstanceName}' referenced by assignment '{instance.InstanceName}.{assignment.Name}'.",
                        location: assignment.Location,
                        logicalPath: assignment.Location?.LogicalPath ?? instance.Location?.LogicalPath ?? "/instances/instance"));
                    continue;
                }

                var propertySegments = assignment.InstancePropertyPath
                    .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                ValidatePropertyChain(
                    diagnostics,
                    model,
                    loadedAssemblies,
                    baseInstance,
                    baseInstanceName,
                    baseConcreteTypeName,
                    baseConcreteType,
                    propertySegments,
                    assignment,
                    instance);
            }
        }

        return diagnostics;
    }

    private static void ValidatePropertyChain(
        List<Diagnostic> diagnostics,
        Model model,
        IReadOnlyList<Assembly> loadedAssemblies,
        Instance baseInstance,
        string baseInstanceName,
        string baseConcreteTypeName,
        Type baseConcreteType,
        IReadOnlyList<string> propertySegments,
        Assignment assignment,
        Instance assignmentOwner)
    {
        var currentYamlTypeName = baseConcreteTypeName;
        var currentConcreteType = baseConcreteType;

        Type? exposedInterfaceType = null;
        if (baseInstance.ExposeToMauiDi && !string.IsNullOrWhiteSpace(baseInstance.InterfaceQualified))
            exposedInterfaceType = ResolveType(loadedAssemblies, baseInstance.InterfaceQualified!);

        for (var i = 0; i < propertySegments.Count; i++)
        {
            var segment = propertySegments[i];
            var isLast = i == propertySegments.Count - 1;

            if (!model.ClassDictionary.TryGetValue(currentYamlTypeName, out var yamlClass))
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.DottedPropertyYamlTypeNotFound,
                    message: $"Dotted-property validation could not find YAML class '{currentYamlTypeName}' while validating '{baseInstanceName}.{string.Join(".", propertySegments)}'.",
                    location: assignment.Location,
                    logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                return;
            }

            if (yamlClass.Properties is null || !yamlClass.Properties.TryGetValue(segment, out var yamlProperty))
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.DottedPropertyYamlPropertyNotDeclared,
                    message: $"Dotted-property validation: YAML model for '{currentYamlTypeName}' does not declare property '{segment}' while validating '{baseInstanceName}.{string.Join(".", propertySegments)}'.",
                    location: assignment.Location,
                    logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                return;
            }

            var concreteProperty = currentConcreteType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
            if (concreteProperty is null)
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.DottedPropertyConcretePropertyNotFound,
                    message: $"Dotted-property validation: concrete type '{FormatTypeDisplay(currentConcreteType)}' does not have public instance property '{segment}' while validating '{baseInstanceName}.{string.Join(".", propertySegments)}'.",
                    location: assignment.Location,
                    logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                return;
            }

            // If the base instance is exposed to MAUI DI, warn when the first segment does not exist on the interface.
            if (i == 0 && exposedInterfaceType is not null)
            {
                var interfaceProperty = exposedInterfaceType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
                if (interfaceProperty is null)
                {
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.DottedPropertyInterfacePropertyNotFound,
                        message: $"Dotted-property validation: base instance '{baseInstanceName}' is exposed to MAUI DI as '{FormatTypeDisplay(exposedInterfaceType)}' but that interface does not declare property '{segment}'.",
                        location: assignment.Location,
                        logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                }
            }

            var expectedTypeName = (yamlProperty.InterfaceQualified ?? yamlProperty.ClassQualified) ?? string.Empty;
            if (!IsTypeMatch(concreteProperty.PropertyType, expectedTypeName, yamlProperty.IsArray, yamlProperty.IsNullable))
            {
                diagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.DottedPropertyConcretePropertyTypeMismatch,
                    message: $"Dotted-property validation: property '{segment}' expected '{FormatExpectedType(expectedTypeName, yamlProperty.IsArray, yamlProperty.IsNullable)}' but was '{FormatTypeDisplay(concreteProperty.PropertyType)}' while validating '{baseInstanceName}.{string.Join(".", propertySegments)}'.",
                    location: assignment.Location,
                    logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                return;
            }

            if (!isLast)
            {
                // To continue validating deeper segments, we need a YAML class type to move to.
                var nextYamlTypeName = yamlProperty.ClassQualified ?? yamlProperty.InterfaceQualified;
                if (string.IsNullOrWhiteSpace(nextYamlTypeName))
                {
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.DottedPropertyYamlPropertyTypeNotDeclared,
                        message: $"Dotted-property validation: YAML property '{currentYamlTypeName}.{segment}' does not declare a type, so the chain '{baseInstanceName}.{string.Join(".", propertySegments)}' cannot be validated past '{segment}'.",
                        location: assignment.Location,
                        logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                    return;
                }

                var nextConcreteTypeName = yamlProperty.ClassQualified;
                if (string.IsNullOrWhiteSpace(nextConcreteTypeName))
                {
                    // We can keep walking concrete reflection types, but we cannot continue the YAML-model validation.
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.DottedPropertyYamlPropertyTypeNotDeclared,
                        message: $"Dotted-property validation: YAML property '{currentYamlTypeName}.{segment}' does not declare a 'class' type, so the chain '{baseInstanceName}.{string.Join(".", propertySegments)}' cannot be validated beyond '{segment}'.",
                        location: assignment.Location,
                        logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                    return;
                }

                var nextConcreteType = ResolveType(loadedAssemblies, nextConcreteTypeName);
                if (nextConcreteType is null)
                {
                    diagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.DottedPropertyTypeNotFound,
                        message: $"Dotted-property validation could not resolve type '{nextConcreteTypeName}' while validating '{baseInstanceName}.{string.Join(".", propertySegments)}'.",
                        location: assignment.Location,
                        logicalPath: assignment.Location?.LogicalPath ?? assignmentOwner.Location?.LogicalPath ?? "/instances/instance"));
                    return;
                }

                currentYamlTypeName = nextConcreteTypeName;
                currentConcreteType = nextConcreteType;
            }
        }
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

            var underlyingName = (underlying.FullName ?? underlying.Name).Replace('+', '.');
            if (string.Equals(underlyingName, expectedTypeName, StringComparison.Ordinal))
                return true;

            if (!expectedTypeName.Contains('.', StringComparison.Ordinal) && string.Equals(underlying.Name, expectedTypeName, StringComparison.Ordinal))
                return true;
        }

        var actualName = (actualType.FullName ?? actualType.Name).Replace('+', '.');

        if (string.Equals(actualName, expectedTypeName, StringComparison.Ordinal))
            return true;

        if (!expectedTypeName.Contains('.', StringComparison.Ordinal))
            return string.Equals(actualType.Name, expectedTypeName, StringComparison.Ordinal);

        return false;
    }

    private static string FormatExpectedType(string expectedTypeName, bool expectedIsArray, bool expectedIsNullable)
    {
        var t = expectedIsArray ? $"{expectedTypeName}[]" : expectedTypeName;
        return expectedIsNullable ? $"{t}?" : t;
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
