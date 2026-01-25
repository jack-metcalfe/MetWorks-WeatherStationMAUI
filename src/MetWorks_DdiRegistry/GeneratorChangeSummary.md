# YAML-Based DI Generator - Enhancement Specifications
## Reflection-Based Validation & Type Safety

Document Version: 1.0  
Created: 2026-01-06  
Purpose: Technical specification for enhancing the YAML-based dependency injection generator with assembly reflection and advanced validation capabilities

---

## Executive Summary

This document specifies enhancements to the custom YAML-based dependency injection code generator to add:
1. Assembly metadata and reflection-based type validation
2. Enhanced parameter-to-instance type checking
3. Multiple interface support per class
4. Constructor parameter support (alternative to initializer methods)
5. Multiple initializer method support

These enhancements will catch configuration errors at generation time rather than runtime, improving type safety and developer experience.

---

## Background: Current Generator Architecture

### Current YAML Structure

The generator uses a declarative YAML configuration with three main sections:

1. codeGen - Generation settings and output configuration
2. namespace - Type definitions (classes, interfaces, parameters)
3. instance - Concrete instance configurations with assignments

Example Current Structure:

    codeGen:
      registryClass: "Registry"
      codePath: "C:/Temp/GeneratedCode"
      namespace: "ServiceRegistry"
      initializer: "InitializeAsync"
    
    namespace:
      - name: "Settings"
        interface: []
        class:
          - name: "SettingsRepository"
            interface: InterfaceDefinition.ISettingsRepository
            parameter:
              - name: "iFileLogger"
                class: null
                interface: "InterfaceDefinition.IFileLogger"
    
    instance:
      - name: "TheSettingsRepository"
        class: "Settings.SettingsRepository"
        assignment:
          - name: "iFileLogger"
            literal: null
            instance: "TheFileLogger"

### Current Validation Approach

The current generator performs:
- Name-based type matching
- String comparison for interface implementation
- Manual parameter count checks
- No runtime type information

Limitations:
- Cannot verify actual interface implementation
- Cannot check type inheritance chains
- Cannot validate parameter types against actual methods
- Cannot detect missing required parameters automatically

---

## Enhancement 1: Assembly Metadata Support

### Objective

Enable the generator to load compiled assemblies and use reflection for accurate type validation.

### YAML Schema Addition: assemblies Section

Add a new top-level section to define assembly references:

    assemblies:
      - name: "CoreAssembly"
        path: "bin/Debug/net10.0/WeatherStationCore.dll"
        loadForValidation: true
      
      - name: "SettingsAssembly"
        path: "bin/Debug/net10.0/Settings.dll"
        loadForValidation: true
      
      - name: "MauiAssembly"
        path: "bin/Debug/net10.0/WeatherStationMaui.dll"
        loadForValidation: true
      
      - name: "RedStarAmounts"
        path: "../packages/RedStar.Amounts/lib/net10.0/RedStar.Amounts.dll"
        loadForValidation: true
      
      - name: "System.Runtime"
        path: null
        loadForValidation: false

Fields:
- name: Unique identifier for the assembly
- path: File path to the assembly DLL (null for system assemblies)
- loadForValidation: Whether to load this assembly during generation for validation

### YAML Schema Enhancement: Assembly References in Classes

Link class definitions to their assemblies:

    namespace:
      - name: "Settings"
        assembly: "SettingsAssembly"
        interface: []
        class:
          - name: "SettingsRepository"
            assembly: "SettingsAssembly"
            interface: InterfaceDefinition.ISettingsRepository
            parameter:
              - name: "iFileLogger"
                interface: "InterfaceDefinition.IFileLogger"

New Fields:
- namespace.assembly: Default assembly for all classes in this namespace
- class.assembly: Override assembly for specific class (optional)

### Implementation: AssemblyLoader Class

    using System.Reflection;
    
    public class AssemblyLoader
    {
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
        private readonly Dictionary<string, Type> _typeCache = new();
        
        public void LoadAssemblies(List<AssemblyDefinition> assemblyDefinitions)
        {
            foreach (var assemblyDef in assemblyDefinitions.Where(a => a.LoadForValidation))
            {
                try
                {
                    if (string.IsNullOrEmpty(assemblyDef.Path))
                        continue;
                    
                    var assembly = Assembly.LoadFrom(assemblyDef.Path);
                    _loadedAssemblies[assemblyDef.Name] = assembly;
                    
                    Console.WriteLine($"✅ Loaded assembly: {assemblyDef.Name} from {assemblyDef.Path}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to load assembly {assemblyDef.Name}: {ex.Message}");
                }
            }
        }
        
        public Type? FindType(string fullTypeName, string? assemblyName = null)
        {
            var cacheKey = assemblyName != null ? $"{assemblyName}::{fullTypeName}" : fullTypeName;
            if (_typeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;
            
            Type? foundType = null;
            
            if (assemblyName != null && _loadedAssemblies.TryGetValue(assemblyName, out var assembly))
            {
                foundType = assembly.GetType(fullTypeName);
            }
            else
            {
                foreach (var assembly in _loadedAssemblies.Values)
                {
                    foundType = assembly.GetType(fullTypeName);
                    if (foundType != null)
                        break;
                }
            }
            
            if (foundType != null)
                _typeCache[cacheKey] = foundType;
            
            return foundType;
        }
        
        public bool ImplementsInterface(string className, string interfaceName, string? classAssembly = null)
        {
            var classType = FindType(className, classAssembly);
            var interfaceType = FindType(interfaceName);
            
            if (classType == null || interfaceType == null)
                return false;
            
            return interfaceType.IsAssignableFrom(classType);
        }
        
        public bool IsCompatibleType(string sourceType, string targetType)
        {
            var sourceTypeObj = FindType(sourceType);
            var targetTypeObj = FindType(targetType);
            
            if (sourceTypeObj == null || targetTypeObj == null)
                return false;
            
            return targetTypeObj.IsAssignableFrom(sourceTypeObj);
        }
        
        public ParameterInfo[]? GetInitializerParameters(string className, string initializerName, string? assemblyName = null)
        {
            var type = FindType(className, assemblyName);
            if (type == null)
                return null;
            
            var method = type.GetMethod(initializerName, BindingFlags.Public | BindingFlags.Instance);
            return method?.GetParameters();
        }
        
        public ParameterInfo[]? GetConstructorParameters(string className, string? assemblyName = null)
        {
            var type = FindType(className, assemblyName);
            if (type == null)
                return null;
            
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            
            return constructors.OrderByDescending(c => c.GetParameters().Length)
                              .FirstOrDefault()
                              ?.GetParameters();
        }
    }

Key Methods:
- LoadAssemblies: Loads all assemblies marked for validation
- FindType: Locates a Type object by full name, optionally within a specific assembly
- ImplementsInterface: Checks if a class implements an interface using reflection
- IsCompatibleType: Checks type compatibility including inheritance
- GetInitializerParameters: Retrieves parameters for an initializer method
- GetConstructorParameters: Retrieves parameters for constructors

---

## Enhancement 2: Reflection-Based Validation

### Objective

Use loaded assemblies to perform accurate type checking during code generation.

### Implementation: ReflectionValidator Class

    public class ReflectionValidator
    {
        private readonly AssemblyLoader _assemblyLoader;
        private readonly YamlConfiguration _config;
        
        public ReflectionValidator(AssemblyLoader assemblyLoader, YamlConfiguration config)
        {
            _assemblyLoader = assemblyLoader;
            _config = config;
        }
        
        public List<ValidationError> ValidateAllInstances()
        {
            var errors = new List<ValidationError>();
            
            foreach (var instance in _config.Instances)
            {
                errors.AddRange(ValidateInstance(instance));
            }
            
            return errors;
        }
        
        private List<ValidationError> ValidateInstance(InstanceDefinition instance)
        {
            var errors = new List<ValidationError>();
            
            var classDefinition = _config.FindClassDefinition(instance.Class);
            if (classDefinition == null)
            {
                errors.Add(new ValidationError(
                    $"Instance '{instance.Name}': Class '{instance.Class}' not found in namespace definitions",
                    instance.LineNumber));
                return errors;
            }
            
            var classType = _assemblyLoader.FindType(instance.Class, classDefinition.Assembly);
            if (classType == null)
            {
                errors.Add(new ValidationError(
                    $"Instance '{instance.Name}': Type '{instance.Class}' not found in loaded assemblies",
                    instance.LineNumber));
                return errors;
            }
            
            if (_config.CodeGen.Initializer != null)
            {
                var initializerParams = _assemblyLoader.GetInitializerParameters(
                    instance.Class, 
                    _config.CodeGen.Initializer, 
                    classDefinition.Assembly);
                
                if (initializerParams != null)
                {
                    errors.AddRange(ValidateParametersAgainstReflection(
                        instance, 
                        classDefinition, 
                        initializerParams));
                }
            }
            
            if (instance.Class.EndsWith("[]"))
            {
                errors.AddRange(ValidateArrayElements(instance));
            }
            
            return errors;
        }
        
        private List<ValidationError> ValidateParametersAgainstReflection(
            InstanceDefinition instance,
            ClassDefinition classDefinition,
            ParameterInfo[] actualParameters)
        {
            var errors = new List<ValidationError>();
            
            foreach (var assignment in instance.Assignments)
            {
                var actualParam = actualParameters.FirstOrDefault(p => 
                    p.Name?.Equals(assignment.Name, StringComparison.OrdinalIgnoreCase) == true);
                
                if (actualParam == null)
                {
                    errors.Add(new ValidationError(
                        $"Instance '{instance.Name}': Parameter '{assignment.Name}' not found in {_config.CodeGen.Initializer}",
                        assignment.LineNumber));
                    continue;
                }
                
                if (assignment.Instance != null)
                {
                    var referencedInstance = _config.FindInstance(assignment.Instance);
                    if (referencedInstance == null)
                    {
                        errors.Add(new ValidationError(
                            $"Instance '{instance.Name}': Referenced instance '{assignment.Instance}' not found",
                            assignment.LineNumber));
                        continue;
                    }
                    
                    var instanceType = _assemblyLoader.FindType(referencedInstance.Class);
                    
                    if (instanceType == null)
                    {
                        errors.Add(new ValidationError(
                            $"Instance '{instance.Name}': Type '{referencedInstance.Class}' not found for parameter '{assignment.Name}'",
                            assignment.LineNumber));
                        continue;
                    }
                    
                    if (!actualParam.ParameterType.IsAssignableFrom(instanceType))
                    {
                        errors.Add(new ValidationError(
                            $"Instance '{instance.Name}': Parameter '{assignment.Name}' expects type '{actualParam.ParameterType.FullName}' " +
                            $"but instance '{assignment.Instance}' is of type '{instanceType.FullName}' which is not compatible",
                            assignment.LineNumber));
                    }
                }
                else if (assignment.Literal != null)
                {
                    if (!IsLiteralCompatible(assignment.Literal, actualParam.ParameterType))
                    {
                        errors.Add(new ValidationError(
                            $"Instance '{instance.Name}': Literal value '{assignment.Literal}' is not compatible with parameter type '{actualParam.ParameterType.Name}'",
                            assignment.LineNumber));
                    }
                }
            }
            
            foreach (var actualParam in actualParameters)
            {
                if (!actualParam.IsOptional && 
                    !instance.Assignments.Any(a => a.Name.Equals(actualParam.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add(new ValidationError(
                        $"Instance '{instance.Name}': Required parameter '{actualParam.Name}' of type '{actualParam.ParameterType.Name}' is missing",
                        instance.LineNumber));
                }
            }
            
            return errors;
        }
        
        private List<ValidationError> ValidateArrayElements(InstanceDefinition instance)
        {
            var errors = new List<ValidationError>();
            var elementTypeName = instance.Class.TrimEnd('[', ']');
            var elementType = _assemblyLoader.FindType(elementTypeName);
            
            if (elementType == null)
            {
                errors.Add(new ValidationError(
                    $"Instance '{instance.Name}': Array element type '{elementTypeName}' not found",
                    instance.LineNumber));
                return errors;
            }
            
            foreach (var element in instance.Elements)
            {
                if (element.Instance != null)
                {
                    var referencedInstance = _config.FindInstance(element.Instance);
                    if (referencedInstance == null)
                        continue;
                    
                    var instanceType = _assemblyLoader.FindType(referencedInstance.Class);
                    if (instanceType != null && !elementType.IsAssignableFrom(instanceType))
                    {
                        errors.Add(new ValidationError(
                            $"Instance '{instance.Name}': Element '{element.Instance}' of type '{instanceType.FullName}' " +
                            $"is not compatible with array element type '{elementType.FullName}'",
                            element.LineNumber));
                    }
                }
            }
            
            return errors;
        }
        
        private bool IsLiteralCompatible(string literal, Type targetType)
        {
            try
            {
                if (targetType == typeof(int))
                    return int.TryParse(literal, out _);
                if (targetType == typeof(bool))
                    return bool.TryParse(literal, out _);
                if (targetType == typeof(string))
                    return true;
                if (targetType == typeof(double))
                    return double.TryParse(literal, out _);
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

Validation Checks Performed:
1. Class exists in loaded assemblies
2. Initializer method exists with correct signature
3. Parameter names match actual method parameters
4. Instance types are compatible with parameter types (using IsAssignableFrom)
5. Literal values are compatible with parameter types
6. All required parameters are provided
7. Array element types are compatible

---

## Enhancement 3: Multiple Interfaces Per Class

### Objective

Allow class definitions to specify multiple interfaces, enabling more accurate polymorphic usage validation.

### YAML Schema Change

Current:

    class:
      - name: "FileLogger"
        interface: InterfaceDefinition.IFileLogger

Enhanced:

    class:
      - name: "FileLogger"
        interfaces:
          - "InterfaceDefinition.IFileLogger"
          - "System.IDisposable"
          - "System.IAsyncDisposable"

Backward Compatibility:
- If interface (singular) is present, convert to interfaces array
- Support both formats during transition period

### Use Cases

1. Polymorphic Collections:

    - name: "disposableServices"
      class: "System.IDisposable[]"
      element:
        - instance: "TheFileLogger"

2. Interface-Based Queries:

    // Can cast to any implemented interface
    IFileLogger logger = registry.GetTheFileLogger();
    IDisposable disposable = registry.GetTheFileLogger();
    await ((IAsyncDisposable)registry.GetTheFileLogger()).DisposeAsync();

3. Validation:
   - Ensures TheFileLogger implements IDisposable before allowing it in disposableServices array

### Implementation Changes

Model:

    public class ClassDefinition
    {
        public string Name { get; set; }
        public string? Assembly { get; set; }
        public List<string> Interfaces { get; set; } = new();
        public List<ParameterDefinition> Parameters { get; set; } = new();
    }

Validator Enhancement:

    private bool ImplementsAnyInterface(Type classType, List<string> interfaces)
    {
        foreach (var interfaceName in interfaces)
        {
            var interfaceType = _assemblyLoader.FindType(interfaceName);
            if (interfaceType != null && interfaceType.IsAssignableFrom(classType))
                return true;
        }
        return false;
    }

---

## Enhancement 4: Constructor Parameter Support

### Objective

Support direct constructor invocation as an alternative to initializer methods.

### YAML Schema Addition

    instance:
      - name: "TheFileLogger"
        class: "Logging.FileLogger"
        instantiation: "constructor"
        parameters:
          - name: "fileSizeLimitBytes"
            literal: "10485760"
          - name: "path"
            literal: "logs/log-.txt"

Instantiation Options:
- "initializer" (default): Use InitializeAsync pattern
- "constructor": Use constructor directly

### Generated Code Difference

Initializer Pattern (Current):

    var theFileLogger = new FileLogger();
    await theFileLogger.InitializeAsync(
        fileSizeLimitBytes: 10485760,
        path: "logs/log-.txt"
    );

Constructor Pattern (New):

    var theFileLogger = new FileLogger(
        fileSizeLimitBytes: 10485760,
        path: "logs/log-.txt"
    );

### Use Cases

1. Third-Party Libraries: Libraries without InitializeAsync pattern
2. Simple Objects: DateTime, Guid, value objects
3. Immutability: Objects that must be fully initialized in constructor

### Implementation Approach

Validation:
- Use GetConstructorParameters() instead of GetInitializerParameters()
- Match constructor signature with provided parameters
- Check for constructor overload ambiguity

Generation:
- Emit constructor call instead of new + InitializeAsync
- Handle constructor parameter order correctly

---

## Enhancement 5: Multiple Initializer Methods

### Objective

Support classes with multiple initializer methods, allowing instance-specific method selection.

### YAML Schema

Class Definition:

    class:
      - name: "SettingsRepository"
        interfaces:
          - "InterfaceDefinition.ISettingsRepository"
        initializers:
          - name: "InitializeAsync"
            parameters:
              - name: "logger"
                interface: "InterfaceDefinition.IFileLogger"
          - name: "InitializeWithConfigAsync"
            parameters:
              - name: "logger"
                interface: "InterfaceDefinition.IFileLogger"
              - name: "config"
                class: "ConfigObject"

Instance Selection:

    instance:
      - name: "TheSettingsRepository"
        class: "Settings.SettingsRepository"
        initializer: "InitializeWithConfigAsync"
        assignment:
          - name: "logger"
            instance: "TheFileLogger"
          - name: "config"
            instance: "TheConfigObject"

### Implementation

Default Behavior:
- If no initializer specified in instance, use global codeGen.initializer
- If no global initializer, use first defined initializer
- If instance specifies initializer, override global default

Validation:
- Verify specified initializer exists in class definition
- Validate parameters against selected initializer's signature

---

## Enhanced YAML Model Classes

    public class YamlConfiguration
    {
        public CodeGenSettings CodeGen { get; set; }
        public List<AssemblyDefinition> Assemblies { get; set; } = new();
        public List<NamespaceDefinition> Namespaces { get; set; } = new();
        public List<InstanceDefinition> Instances { get; set; } = new();
    }
    
    public class AssemblyDefinition
    {
        public string Name { get; set; }
        public string? Path { get; set; }
        public bool LoadForValidation { get; set; } = true;
        public int LineNumber { get; set; }
    }
    
    public class CodeGenSettings
    {
        public string RegistryClass { get; set; }
        public string CodePath { get; set; }
        public string Namespace { get; set; }
        public string? Initializer { get; set; }
        public ValidationSettings Validation { get; set; } = new();
    }
    
    public class ValidationSettings
    {
        public bool Enabled { get; set; } = true;
        public bool UseReflection { get; set; } = false;
        public bool StrictTypeChecking { get; set; } = false;
    }
    
    public class NamespaceDefinition
    {
        public string Name { get; set; }
        public string? Assembly { get; set; }
        public List<string> Interfaces { get; set; } = new();
        public List<ClassDefinition> Classes { get; set; } = new();
    }
    
    public class ClassDefinition
    {
        public string Name { get; set; }
        public string? Assembly { get; set; }
        public List<string> Interfaces { get; set; } = new();
        public List<InitializerDefinition> Initializers { get; set; } = new();
        public List<ParameterDefinition> Parameters { get; set; } = new();
        public int LineNumber { get; set; }
    }
    
    public class InitializerDefinition
    {
        public string Name { get; set; }
        public List<ParameterDefinition> Parameters { get; set; } = new();
    }
    
    public class InstanceDefinition
    {
        public string Name { get; set; }
        public string Class { get; set; }
        public string? Instantiation { get; set; }
        public string? Initializer { get; set; }
        public List<AssignmentDefinition> Assignments { get; set; } = new();
        public List<ElementDefinition> Elements { get; set; } = new();
        public int LineNumber { get; set; }
    }
    
    public class ValidationError
    {
        public string Message { get; set; }
        public int LineNumber { get; set; }
        
        public ValidationError(string message, int lineNumber)
        {
            Message = message;
            LineNumber = lineNumber;
        }
    }

---

## Generator Integration

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🔧 YAML-Based DI Code Generator");
        Console.WriteLine("================================");
        
        var yamlPath = args.Length > 0 ? args[0] : "maximal-valid.yaml";
        
        if (!File.Exists(yamlPath))
        {
            Console.WriteLine($"❌ YAML file not found: {yamlPath}");
            return;
        }
        
        var yamlContent = File.ReadAllText(yamlPath);
        var config = YamlParser.Parse(yamlContent);
        
        Console.WriteLine($"✅ Parsed YAML configuration");
        Console.WriteLine($"   - Namespaces: {config.Namespaces.Count}");
        Console.WriteLine($"   - Instances: {config.Instances.Count}");
        Console.WriteLine($"   - Assemblies: {config.Assemblies.Count}");
        
        var assemblyLoader = new AssemblyLoader();
        
        if (config.Assemblies.Any())
        {
            Console.WriteLine();
            Console.WriteLine("📦 Loading assemblies for validation...");
            assemblyLoader.LoadAssemblies(config.Assemblies);
        }
        
        if (config.CodeGen.Validation.UseReflection)
        {
            Console.WriteLine();
            Console.WriteLine("🔍 Performing reflection-based validation...");
            
            var validator = new ReflectionValidator(assemblyLoader, config);
            var errors = validator.ValidateAllInstances();
            
            if (errors.Any())
            {
                Console.WriteLine($"❌ Validation failed with {errors.Count} error(s):");
                foreach (var error in errors.OrderBy(e => e.LineNumber))
                {
                    Console.WriteLine($"   Line {error.LineNumber}: {error.Message}");
                }
                return;
            }
            
            Console.WriteLine("✅ All type validations passed!");
        }
        
        Console.WriteLine();
        Console.WriteLine("⚙️ Generating code...");
        var generator = new CodeGenerator(config, assemblyLoader);
        var generatedCode = generator.Generate();
        
        var outputPath = config.CodeGen.CodePath;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, generatedCode);
        
        Console.WriteLine($"✅ Code generated successfully!");
        Console.WriteLine($"   Output: {outputPath}");
        Console.WriteLine($"   Lines: {generatedCode.Split('\n').Length}");
    }

---

## Priority Matrix

| Enhancement | Impact | Complexity | Priority | Implementation Time |
|-------------|--------|------------|----------|-------------------|
| 1. Assembly Loading + Type Discovery | ⭐⭐⭐⭐⭐ | ⭐⭐ | CRITICAL | 1-2 days |
| 2. Reflection-Based Validation | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | CRITICAL | 2-3 days |
| 3. Multiple Interfaces | ⭐⭐⭐⭐ | ⭐⭐ | HIGH | 1 day |
| 4. Constructor Parameters | ⭐⭐⭐ | ⭐⭐⭐⭐ | MEDIUM | 2 days |
| 5. Multiple Initializers | ⭐⭐ | ⭐⭐⭐⭐ | LOW | 2 days |

---

## Recommended Implementation Order

Phase A: Core Type Safety (Week 1)
1. Add assemblies section to YAML schema
2. Implement AssemblyLoader with type discovery
3. Implement ReflectionValidator for instance validation
4. Integrate validation into generator pipeline
5. Add error reporting with line numbers

Phase B: Enhanced Modeling (Week 2)
6. Add multiple interfaces support
7. Update ClassDefinition model
8. Enhance validation for interface arrays
9. Generate interface-cast helper methods

Phase C: Advanced Features (Later)
10. Add constructor parameter support (if needed)
11. Add multiple initializers (if use case arises)
12. Export YAML schema for IDE validation

---

## Benefits Summary

Before Enhancements (Name-Based):
- ❌ String matching for interface implementation
- ❌ No inheritance support
- ❌ Manual parameter validation
- ❌ Runtime type errors possible
- ⚠️ Typos not caught until runtime

After Enhancements (Reflection-Based):
- ✅ Actual interface implementation check via reflection
- ✅ Full inheritance chain validation
- ✅ Automatic parameter detection and validation
- ✅ Compile-time equivalent type safety
- ✅ Catch configuration errors at generation time
- ✅ Support for complex type scenarios (generics, arrays)

---

## Example Enhanced YAML Configuration

    codeGen:
      registryClass: "Registry"
      codePath: "Generated/Registry.g.cs"
      namespace: "ServiceRegistry"
      initializer: "InitializeAsync"
      validation:
        enabled: true
        useReflection: true
        strictTypeChecking: true
    
    assemblies:
      - name: "CoreAssembly"
        path: "bin/Debug/net10.0/WeatherStationCore.dll"
        loadForValidation: true
      
      - name: "SettingsAssembly"
        path: "bin/Debug/net10.0/Settings.dll"
        loadForValidation: true
    
    namespace:
      - name: "Settings"
        assembly: "SettingsAssembly"
        interface:
          - name: "ISettingsRepository"
        class:
          - name: "SettingsRepository"
            assembly: "SettingsAssembly"
            interfaces:
              - "InterfaceDefinition.ISettingsRepository"
              - "System.IDisposable"
            parameters:
              - name: "iFileLogger"
                interface: "InterfaceDefinition.IFileLogger"
              - name: "settingConfigurations"
                interface: "InterfaceDefinition.ISettingConfiguration[]"
    
    instance:
      - name: "TheSettingsRepository"
        class: "Settings.SettingsRepository"
        assignment:
          - name: "iFileLogger"
            instance: "TheFileLogger"
          - name: "settingConfigurations"
            instance: "TheUdpSettings"

---

## Testing Strategy

Unit Tests:
- AssemblyLoader.LoadAssemblies() - verify assembly loading
- AssemblyLoader.FindType() - type discovery and caching
- AssemblyLoader.ImplementsInterface() - interface checks
- ReflectionValidator.ValidateInstance() - parameter validation
- ReflectionValidator.ValidateArrayElements() - array type checks

Integration Tests:
- Full YAML parsing + validation pipeline
- Error reporting with correct line numbers
- Code generation with validated types
- Backward compatibility with existing YAML files

---

## Migration Guide

Step 1: Add assemblies section to existing YAML

Step 2: Enable reflection validation:

    codeGen:
      validation:
        useReflection: true

Step 3: Run generator and fix reported errors

Step 4: Gradually add assembly references to class definitions

Step 5: Convert interface (singular) to interfaces (plural) where needed

Backward Compatibility:
- Generator falls back to name-based validation if assemblies not loaded
- Both interface and interfaces syntax supported
- Warning messages for deprecated patterns

---

Document Version: 1.0  
Last Updated: 2026-01-06  
Status: Technical Specification  
Next Review: After Phase A Implementation