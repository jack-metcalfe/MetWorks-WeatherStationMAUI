using System.Diagnostics;

namespace MetWorks.DI.Declarative.Syntax;
public sealed class Loader
{
    public Models.Model Load(string yamlText)
    {
        // GUARD: Validate input
        ArgumentNullException.ThrowIfNull(yamlText);

        // Logical path has instance names embeded but tokenPath is without instance names
        // Logical path is useful for finding locations in source file and providing context with real data
        // Token path is useful for looking up schema info by context
        // XPath-like need to look up syntax for mimicry of XML for YAML location tracking
        var logicalPath = Models.Schema.TokenTypeToName[TokenTypes.root];

        // PARSE: Load YAML, collect parse errors as diagnostics (no throw)
        YamlMappingNode? rootYamlMappingNode = null;
        Location? location = null;
        var diagnostics = new List<Diagnostic>();
        try
        {
            var yamlStream = new YamlStream();
            using var stringReader = new StringReader(yamlText);
            yamlStream.Load(stringReader);
            if (yamlStream.Documents.Count > 0)
            {
                var yamlNode = yamlStream.Documents[0].RootNode;
                location = new Location(
                    yamlNode: yamlNode,
                    logicalPath: logicalPath
                );
                rootYamlMappingNode = yamlNode as YamlMappingNode;
                if (rootYamlMappingNode is null)
                    diagnostics.Add(
                        diagnosticCode: DiagnosticCode.YamlRootNodeNotMapping,
                        location: location
                    );
            }
            else
                diagnostics.Add(
                    diagnosticCode: DiagnosticCode.RootYamlDocumentMissing,
                    location: location
                );
        }

        catch (YamlException yamlException)
        {
            // YAML parse error—convert to diagnostic
            // See https://github.com/aaubry/YamlDotNet/wiki/Overview#yamldotnetrepresentationmodel
            diagnostics.Add(
                diagnosticCode: DiagnosticCode.YamlParseError,
                message: $"YAML parse error: {yamlException.Message}",
                location: new Location(yamlException, logicalPath)
            );
        }

        catch (Exception exception)
        {
            diagnostics.Add(
                diagnosticCode: DiagnosticCode.UnrecognizedToken,
                message: $"Unexpected YAML load error: {exception.Message}",
                location: location
            );
        }

        // VALIDATE: Fail fast on critical errors (no root node)
        if (rootYamlMappingNode is null) return new Models.Model(diagnostics: diagnostics);

        // DELEGATE: Parse the model (all diagnostics accumulate in shared list)
        return ParseModel(
            yamlMappingNode: rootYamlMappingNode,
            incomingDiagnostics: diagnostics
        );
    }
    private Models.Model ParseModel(
        YamlMappingNode yamlMappingNode,
        List<Diagnostic> incomingDiagnostics
    )
    {
        var type = typeof(Models.Model);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        var logicalPath = $"{tokenTypeName}/";
        var localDiagnostics = new List<Diagnostic>();

        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode,
                dtoType: type,
                logicalPath: logicalPath
            )
        );

        var tokenPath = logicalPath;

        var codeGen = ParseCodeGen(
            yamlMappingNode: yamlMappingNode,
            logicalPath: logicalPath,
            tokenPath: tokenPath,
            incomingDiagnostics: localDiagnostics
        );

        var namespaces = ParseNamespaces(
            yamlMappingNode: yamlMappingNode,
            logicalPath: logicalPath,
            tokenPath: tokenPath,
            incomingDiagnostics: localDiagnostics
        );

        var dictionaries = BuildDictionaries(namespaces, localDiagnostics);
        var instances = new List<Models.Instance>();
        Dictionary<string, Models.Instance> instanceDictionary = new();
        if (localDiagnostics.Count == 0)
        {
            instances = ParseInstances(
                yamlMappingNode: yamlMappingNode,
                logicalPath: logicalPath,
                tokenPath: tokenPath,
                incomingDiagnostics: localDiagnostics,
                parameterDictionary: dictionaries.ParameterDictionary,
                classDictionary: dictionaries.ClassDictionary,
                instanceDictionary: instanceDictionary
            );            
        }

        incomingDiagnostics.AddRange(localDiagnostics);

        return new Models.Model(
            codeGen: codeGen,
            namespaces: namespaces,
            instances: instances,
            namespaceDictionary: dictionaries.NamespaceDictionary,
            interfaceDictionary: dictionaries.InterfaceDictionary,
            classDictionary: dictionaries.ClassDictionary,
            parameterDictionary: dictionaries.ParameterDictionary,
            instanceDictionary: instanceDictionary,
            location: new Location(
                yamlNode: yamlMappingNode,
                logicalPath: logicalPath
            ),
            diagnostics: localDiagnostics
        );
    }
    private Models.CodeGen? ParseCodeGen(
        YamlMappingNode yamlMappingNode,
        string logicalPath, 
        string tokenPath,
        List<Diagnostic> incomingDiagnostics
    )
    {
        var type = typeof(Models.CodeGen);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var codeGenMappingNode = GetChildMapping(
            yamlMappingNode: yamlMappingNode,
            key: tokenTypeName
        );
        if (codeGenMappingNode is null)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.CodeGenMissing,
                location: location
            );

            return null;
        }

        yamlMappingNode = codeGenMappingNode;
       
        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode,
                dtoType: type,
                logicalPath: logicalPath
            )
        );

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.codeGenRegistryClass];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var registryClass = GetScalar(
            yamlMappingNode: yamlMappingNode,
            key: tokenTypeName
        );
        if (string.IsNullOrWhiteSpace(registryClass))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.CodeGenMissingRegistryClass,
                location: location
            );
        }
        else if (!registryClass.IsValidIdentifier())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"{tokenTypeName} '{registryClass}' is not a valid identifier.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{registryClass}']" }
            );
        }
        else if (!registryClass.IsPascalCase())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.IdentifierNotPascalCase,
                message: $"{tokenTypeName} '{registryClass}' is not in PascalCase.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{registryClass}']" }
            );
        }

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.codeGenCodePath];
        var codePath = GetScalar(
            yamlMappingNode: yamlMappingNode,
            key: tokenTypeName
        );
        if (string.IsNullOrWhiteSpace(codePath))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.CodeGenMissingGeneratedPath,
                message: null,
                location: location
            );
        }

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.codeGenNamespace];
        var codeGenNamespace = GetScalar(
            yamlMappingNode: yamlMappingNode,
            key: tokenTypeName
        );
        if (string.IsNullOrWhiteSpace(codeGenNamespace))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.CodeGenMissingNamespace,
                message: null,
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='']" }
            );
        }
        else if (!codeGenNamespace.IsValidNamespace())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.NamespaceInvalidSegment,
                message: $"{tokenTypeName} '{codeGenNamespace}' is not a valid namespace.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{codeGenNamespace}']" }
            );
        }

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.codeGenInitializer];
        var initializer = GetScalar(
            yamlMappingNode: yamlMappingNode,
            key: tokenTypeName
        );
        if (string.IsNullOrWhiteSpace(initializer))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.CodeGenMissingInitializer,
                location: location
            );
        }
        else if (!initializer.IsValidIdentifier())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"{tokenTypeName} '{initializer}' is not a valid identifier.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{initializer}']" }
            );
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return new Models.CodeGen(
            registryClass: registryClass,
            codePath: codePath,
            @namespace: codeGenNamespace,
            initializer: initializer,
            location: location,
            diagnostics: localDiagnostics
        );
    }

    private List<Models.Namespace> ParseNamespaces(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics
    )
    {
        var tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespaces];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var list = new List<Models.Namespace>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.NamespacesMissing,
                location: location
            );

            return list;
        }

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.NamespaceInvalidNode,
                    message: $"Namespace at {childLogicalPath}[{i}] must be a mapping node.",
                    location: new Location(childNode, $"{logicalPath}[{i}]")
                );
                continue;
            }

            var dto = ParseNamespace(
                childYamlMappingNode, 
                childLogicalPath, 
                tokenPath,
                localDiagnostics
            );

            if (dto is not null) list.Add(dto);
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return list;
    }
    private Models.Namespace? ParseNamespace(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics
    )
    {
        var type = typeof(Models.Namespace);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();
        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode,
                dtoType: type,
                logicalPath: logicalPath
            )
        );

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceName];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var name = GetScalar(yamlMappingNode, tokenTypeName);
        location = new Location(yamlMappingNode, logicalPath);
        if (string.IsNullOrWhiteSpace(name))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.NamespaceMissingName,
                message: $"Missing '{tokenTypeName}' in {logicalPath}.",
                location: location
            );
        }
        else if (!name.IsValidNamespace())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"Namespace name '{name}' is not a valid identifier.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{name}']" }
            );
        }

        IReadOnlyList<Models.Interface> interfaces = Array.Empty<Models.Interface>();
        IReadOnlyList<Models.Class> classes = Array.Empty<Models.Class>();
        if (name is not null)
        {
            interfaces = ParseInterfaces(
                yamlMappingNode: yamlMappingNode, 
                logicalPath: logicalPath, 
                tokenPath: tokenPath,
                incomingDiagnostics: localDiagnostics,
                namespaceName: name
            );

            classes = ParseClasses(
                yamlMappingNode: yamlMappingNode, 
                logicalPath: logicalPath, 
                tokenPath: tokenPath,
                incomingDiagnostics: localDiagnostics,
                namespaceName: name
            );
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return new Models.Namespace(
            namespaceName: name!,
            interfaces: interfaces,
            classes: classes,
            location: location,
            diagnostics: localDiagnostics
        );
    }
    List<Models.Interface> ParseInterfaces(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName
    )
    {
        var tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceInterfaces];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var list = new List<Models.Interface>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InterfacesMissing,
                location: location
            );

            return list;
        }

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InterfaceInvalidNode,
                    message: $"Interface at {childLogicalPath} must be a mapping node.",
                    location: new Location(childNode, childLogicalPath)
                );
                continue;
            }

            var dto = ParseInterface(
                yamlMappingNode: childYamlMappingNode, 
                logicalPath: logicalPath, 
                tokenPath: tokenPath,
                incomingDiagnostics: localDiagnostics,
                namespaceName: namespaceName
            );

            if (dto is not null) list.Add(dto);
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return list;
    }
    private Models.Interface? ParseInterface(
        YamlMappingNode yamlMappingNode, 
        string logicalPath, 
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName
    )
    {
        var type = typeof(Models.Interface);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();
        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode, 
                dtoType: type, 
                logicalPath: logicalPath
            )
        );

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceInterfaceName];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var name = GetScalar(yamlMappingNode, tokenTypeName);
        if (string.IsNullOrWhiteSpace(name))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InterfaceMissingName,
                message: $"Interface name token at {logicalPath} must be a scalar or mapping node.",
                location: location
            );
        }
        else if (!name.IsValidIdentifier())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"InterfaceName '{name}' must be a simple identifier.",
                location: location
            );
        }
        else if (!name.IsInterfaceName())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InterfaceNameInvalidFormat,
                message: $"InterfaceName '{name}' is not in the correct format. Expected format is 'I' followed by a valid identifier.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{name}']" }
            );
        }
        else
            location = location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{name}']" };

        incomingDiagnostics.AddRange(localDiagnostics);
        return new Models.Interface(
            namespaceName: namespaceName,
            interfaceName: name,
            location: location,
            diagnostics: localDiagnostics
        );
    }
    IReadOnlyList<Models.Class> ParseClasses(
        YamlMappingNode yamlMappingNode, 
        string logicalPath, 
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName
    )
    {
        var type = typeof(Models.Class);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var list = new List<Models.Class>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.ClassesMissing,
                location: location
            );

            return list;
        }

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.ClassInvalidNode,
                    message: $"Class at {childLogicalPath} must be a mapping node.",
                    location: new Location(childNode, childLogicalPath)
                );
                continue;
            }

            var dto = ParseClass(
                yamlMappingNode: childYamlMappingNode, 
                logicalPath: childLogicalPath, 
                tokenPath: tokenPath, 
                incomingDiagnostics: localDiagnostics,
                namespaceName: namespaceName
            );

            if (dto is not null) list.Add(dto);
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return list;
    }
    private Models.Class? ParseClass(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName
    )
    {
        var type = typeof(Models.Class);        
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();
        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode, 
                dtoType: type, 
                logicalPath: logicalPath
            )
        );

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassName];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var className = GetScalar(yamlMappingNode, tokenTypeName);
        if (string.IsNullOrWhiteSpace(className))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.ClassMissingName,
                message: $"Missing '{tokenTypeName}' in {logicalPath}.",
                location: location
            );
        }
        else if (!className.IsValidIdentifier())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"ClassName '{className}' is not a valid identifier.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{className}']" }
            );
        }
        else if (!className.IsPascalCase())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.IdentifierNotPascalCase,
                message: $"ClassName '{className}' is not in PascalCase.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{className}']" }
            );
        }
        else if (className.IsQualifiedName())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.ClassNameMustBeSimple,
                message: $"ClassName '{className}' is not in the correct format. Expected format is a valid identifier ending with 'Class'.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{className}']" }
            );
        }
        else
            location = location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{className}']" };

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassInterface];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var interfaceName = GetScalar(yamlMappingNode, tokenTypeName);
        if (!interfaceName.IsWhiteSpace())
        {
            if (!interfaceName.TryParseTypeRef(
                out var qualifiedName,
                out var isArray,
                out var isContainerNullable,
                out var isElementNullable)
            )
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.TypeRefInvalid,
                    message: $"Invalid type reference '{interfaceName}' at {logicalPath}.",
                    location: location
                );
            }
        }

        var parameters = ParseParameters(
            yamlMappingNode: yamlMappingNode, 
            logicalPath: logicalPath, 
            tokenPath: tokenPath,
            incomingDiagnostics: localDiagnostics,
            namespaceName: namespaceName,
            className: className!
        );

        var properties = ParseProperties(
            yamlMappingNode: yamlMappingNode,
            logicalPath: logicalPath,
            tokenPath: tokenPath,
            incomingDiagnostics: localDiagnostics,
            namespaceName: namespaceName,
            className: className!
        );

        incomingDiagnostics.AddRange(localDiagnostics);

        return new Models.Class(
            namespaceName: namespaceName,
            className: className,
            interfaceQualified: interfaceName,
            parameters: parameters,
            properties: properties,
            location: new Location(yamlMappingNode, logicalPath),
            diagnostics: localDiagnostics
        );
    }
    Dictionary<string, Models.Parameter> ParseParameters(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName,
        string className
    )
    {
        var tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassParameters];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var dictionary = new Dictionary<string, Models.Parameter>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.ParametersMissing,
                location: location
            );

            return dictionary;
        }

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.ParameterInvalidNode,
                    message: $"Initializer parameter at {childLogicalPath} must be a mapping node.",
                    location: new Location(childNode, childLogicalPath)
                );
                continue;
            }

            var dto = ParseParameter(
                yamlMappingNode: childYamlMappingNode, 
                logicalPath: childLogicalPath, 
                tokenPath: tokenPath,
                incomingDiagnostics: localDiagnostics,
                namespaceName: namespaceName,
                className: className
            );

            if (dto is not null && !dto.ParameterName.IsWhiteSpace()) 
                dictionary.Add(dto.ParameterName!, dto);
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        
        return dictionary;
    }
    private Models.Parameter? ParseParameter(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName,
        string className
    )
    {
        var type = typeof(Models.Parameter);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        var incomingLogicalPath = logicalPath;
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();
        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode, 
                dtoType: type, 
                logicalPath: logicalPath
            )
        );  

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterName];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);        
        var name = GetScalar(yamlMappingNode, tokenTypeName);
        if (string.IsNullOrWhiteSpace(name))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.ParameterMissingName,
                message: $"Missing 'parameterName' in {logicalPath}.",
                location: location
            );
        }
        else if (!name.IsValidIdentifier())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"ParameterName '{name}' is not a valid identifier.",
                location: location
            );
        }

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterClass];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);

        var hasClass = false;
        string? classQualified = null;
        bool classIsArray = false;
        bool classIsContainerNullable = false;
        bool classIsElementNullable = false;

        var classToken = GetScalar(yamlMappingNode, tokenTypeName);
        if (!string.IsNullOrWhiteSpace(classToken))
        {
            // ToDo: Add test for generics "<T>" and add diagnostic for them when found
            if (
                classToken.TryParseTypeRef(
                    out classQualified,
                    out classIsArray,
                    out classIsContainerNullable,
                    out classIsElementNullable
                )
            )
                hasClass = true;
            else
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.TypeRefInvalid,
                    message: $"Invalid type reference '{classToken}' at {logicalPath}.",
                    location: location
                );
            }
        }

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterInterface];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);

        var hasInterface = false;
        string? interfaceQualified = null;
        bool interfaceIsArray = false;
        bool interfaceIsContainerNullable = false;
        bool interfaceIsElementNullable = false;

        var interfaceToken = GetScalar(yamlMappingNode, tokenTypeName);
        if (!string.IsNullOrWhiteSpace(interfaceToken))
        {
            if (
                interfaceToken.TryParseTypeRef(
                    out interfaceQualified,
                    out interfaceIsArray,
                    out interfaceIsContainerNullable,
                    out interfaceIsElementNullable
                )
            )
                hasInterface = true;
            else
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.TypeRefInvalid,
                    message: $"Invalid type reference '{interfaceToken}' at {logicalPath}.",
                    location: location
                );
            }
        }

        if (hasClass && hasInterface)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.ParameterBothClassAndInterface,
                message: $"Parameter at {logicalPath} specifies both qualifiedClassName and qualifiedInterfaceName. Exactly one must be non-null.",
                location: location
            );
        }
        else if (!hasClass && !hasInterface)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.ParameterMissingClassOrInterface,
                message: $"Parameter at {logicalPath} must specify either qualifiedClassName or qualifiedInterfaceName.",
                location: location
            );
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        // ToDo: Add isValid to each dto can keep data found but indicate it's invalid
        // ToDo: Consider adding an List of Diagnostic to each dto to capture dto-specific issues copy those being added to diagnostics or better add to a new diagnostics for new dto then use AddRange to copy to before returning
        // ToDo: Consider combining isArray, isNullable, isElementNullable into a single struct to reduce parameter count
        // ToDo: Consider making isArray, isNullable, isElementNullable non-nullable with default false to reduce null checks
        return new Models.Parameter(
            namespaceName: namespaceName,
            className: className,
            parameterName: name,
            classToken: classToken,
            @class: classQualified.ExtractShortName(),
            classQualified: classQualified,
            interfaceToken: interfaceToken,
            @interface: interfaceQualified.ExtractShortName(),
            interfaceQualified: interfaceQualified,
            isArray: hasInterface ? interfaceIsArray : classIsArray,
            isNullable: hasInterface ? interfaceIsContainerNullable : classIsContainerNullable,
            isElementNullable: hasInterface ? interfaceIsElementNullable : classIsElementNullable,
            location: new Location(yamlMappingNode, logicalPath),
            diagnostics: localDiagnostics
        );
    }
    Dictionary<string, Models.Property> ParseProperties(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName,
        string className
    )
    {
        var tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassProperties];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var dictionary = new Dictionary<string, Models.Property>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.PropertiesMissing,
                location: location
            );

            return dictionary;
        }

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.PropertyInvalidNode,
                    message: $"Initializer property at {childLogicalPath} must be a mapping node.",
                    location: new Location(childNode, childLogicalPath)
                );
                continue;
            }

            var dto = ParseProperty(
                yamlMappingNode: childYamlMappingNode,
                logicalPath: childLogicalPath,
                tokenPath: tokenPath,
                incomingDiagnostics: localDiagnostics,
                namespaceName: namespaceName,
                className: className
            );

            if (dto is not null && !dto.PropertyName.IsWhiteSpace())
                dictionary.Add(dto.PropertyName!, dto);
        }

        incomingDiagnostics.AddRange(localDiagnostics);

        return dictionary;
    }
    private Models.Property? ParseProperty(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        string namespaceName,
        string className
    )
    {
        var type = typeof(Models.Property);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        var incomingLogicalPath = logicalPath;
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();
        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode,
                dtoType: type,
                logicalPath: logicalPath
            )
        );

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyName];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var name = GetScalar(yamlMappingNode, tokenTypeName);
        if (string.IsNullOrWhiteSpace(name))
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.PropertyMissingName,
                message: $"Missing 'propertyName' in {logicalPath}.",
                location: location
            );
        }
        else if (!name.IsValidIdentifier())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"PropertyName '{name}' is not a valid identifier.",
                location: location
            );
        }

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyClass];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);

        var hasClass = false;
        string? classQualified = null;
        bool classIsArray = false;
        bool classIsContainerNullable = false;
        bool classIsElementNullable = false;

        var classToken = GetScalar(yamlMappingNode, tokenTypeName);
        if (!string.IsNullOrWhiteSpace(classToken))
        {
            // ToDo: Add test for generics "<T>" and add diagnostic for them when found
            if (
                classToken.TryParseTypeRef(
                    out classQualified,
                    out classIsArray,
                    out classIsContainerNullable,
                    out classIsElementNullable
                )
            )
                hasClass = true;
            else
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.TypeRefInvalid,
                    message: $"Invalid type reference '{classToken}' at {logicalPath}.",
                    location: location
                );
            }
        }

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyInterface];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);

        var hasInterface = false;
        string? interfaceQualified = null;
        bool interfaceIsArray = false;
        bool interfaceIsContainerNullable = false;
        bool interfaceIsElementNullable = false;

        var interfaceToken = GetScalar(yamlMappingNode, tokenTypeName);
        if (!string.IsNullOrWhiteSpace(interfaceToken))
        {
            if (
                interfaceToken.TryParseTypeRef(
                    out interfaceQualified,
                    out interfaceIsArray,
                    out interfaceIsContainerNullable,
                    out interfaceIsElementNullable
                )
            )
                hasInterface = true;
            else
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.TypeRefInvalid,
                    message: $"Invalid type reference '{interfaceToken}' at {logicalPath}.",
                    location: location
                );
            }
        }

        if (hasClass && hasInterface)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.PropertyBothClassAndInterface,
                message: $"Property at {logicalPath} specifies both qualifiedClassName and qualifiedInterfaceName. Exactly one must be non-null.",
                location: location
            );
        }
        else if (!hasClass && !hasInterface)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.PropertyMissingClassOrInterface,
                message: $"Property at {logicalPath} must specify either qualifiedClassName or qualifiedInterfaceName.",
                location: location
            );
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        // ToDo: Add isValid to each dto can keep data found but indicate it's invalid
        // ToDo: Consider adding an List of Diagnostic to each dto to capture dto-specific issues copy those being added to diagnostics or better add to a new diagnostics for new dto then use AddRange to copy to before returning
        // ToDo: Consider combining isArray, isNullable, isElementNullable into a single struct to reduce property count
        // ToDo: Consider making isArray, isNullable, isElementNullable non-nullable with default false to reduce null checks
        return new Models.Property(
            namespaceName: namespaceName,
            className: className,
            propertyName: name,
            classToken: classToken,
            @class: classQualified.ExtractShortName(),
            classQualified: classQualified,
            interfaceToken: interfaceToken,
            @interface: interfaceQualified.ExtractShortName(),
            interfaceQualified: interfaceQualified,
            isArray: hasInterface ? interfaceIsArray : classIsArray,
            isNullable: hasInterface ? interfaceIsContainerNullable : classIsContainerNullable,
            isElementNullable: hasInterface ? interfaceIsElementNullable : classIsElementNullable,
            location: new Location(yamlMappingNode, logicalPath),
            diagnostics: localDiagnostics
        );
    }
    List<Models.Instance>? ParseInstances(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        Dictionary<string, Models.Parameter> parameterDictionary,
        Dictionary<string, Models.Class> classDictionary,
        Dictionary<string, Models.Instance> instanceDictionary
    )
    {
        var type = typeof(Models.Instance);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var list = new List<Models.Instance>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InstanceMissing,
                location: new Location(yamlMappingNode, tokenTypeName)
            );

            return list;
        }


        // Two-pass parse:
        // 1) Declare all instances (so forward references are allowed)
        // 2) Bind assignments/elements (validate references against the complete set)
        var declared = new List<(YamlMappingNode Node, string LogicalPath, string? Name, Models.Class? Class, string? ClassToken, bool IsArray, bool ExposeToMauiDi, string? FactoryInstanceName, string? FactoryMethodName, bool CanBind)>();

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InstanceInvalidNode,
                    message: $"NamedInstance at {childLogicalPath} must be a mapping node.",
                    location: new Location(childNode, childLogicalPath)
                );
                continue;
            }

            var instanceDiagnostics = new List<Diagnostic>();
            instanceDiagnostics.AddRange(
                ValidateMappingKeys(
                    yamlMappingNode: childYamlMappingNode,
                    dtoType: type,
                    logicalPath: $"{childLogicalPath}/{tokenTypeName}/"
                )
            );

            // Name
            var instanceNameToken = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceName];
            var instanceNameLogicalPath = $"{childLogicalPath}/{tokenTypeName}/{instanceNameToken}/";
            var instanceNameLocation = new Location(childYamlMappingNode, instanceNameLogicalPath);
            var instanceName = GetScalar(childYamlMappingNode, instanceNameToken);
            if (instanceName.IsWhiteSpace())
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InstanceMissingName,
                    message: $"Missing 'namedInstanceName' in {instanceNameLogicalPath}.",
                    location: instanceNameLocation
                );
            }
            else if (!instanceName.IsValidIdentifier())
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InvalidIdentifier,
                    message: $"NamedInstanceName '{instanceName}' is not a valid identifier.",
                    location: instanceNameLocation with { LogicalPath = $"{instanceNameLogicalPath}[@{instanceNameToken}='{instanceName}']" }
                );
            }
            else if (!instanceName.IsPascalCase())
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.IdentifierNotPascalCase,
                    message: $"NamedInstanceName '{instanceName}' is not in PascalCase.",
                    location: instanceNameLocation with { LogicalPath = $"{instanceNameLogicalPath}[@{instanceNameToken}='{instanceName}']" }
                );
            }

            // Class
            var classTokenName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceClass];
            var classLogicalPath = $"{childLogicalPath}/{tokenTypeName}/{classTokenName}/";
            var classLocation = new Location(childYamlMappingNode, classLogicalPath);

            var classToken = GetScalar(childYamlMappingNode, classTokenName);
            string? classQualified = null;
            var instanceIsArray = false;
            bool classIsContainerNullable;
            bool classIsElementNullable;

            Models.Class? instanceClass = null;

            if (classToken.IsWhiteSpace())
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InstanceMissingQualifiedClass,
                    message: $"Missing 'qualifiedClassName' in {classLogicalPath}.",
                    location: classLocation
                );
            }
            else if (!classToken.TryParseTypeRef(out classQualified, out instanceIsArray, out classIsContainerNullable, out classIsElementNullable))
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.TypeRefInvalid,
                    message: $"Invalid type reference '{classToken}' in " +
                             $"{classLogicalPath}. Supported forms: 'Ns.Type', 'Ns.Type?', 'Ns.Type[]', 'Ns.Type[]?'. " +
                             "Nullable element types inside arrays (e.g., 'Ns.Type?[]') are not supported.",
                    location: classLocation
                );
            }
            else
            {
                instanceClass = classDictionary.GetValueOrDefault(classQualified!);
                if (instanceClass is null)
                {
                    instanceDiagnostics.Add(
                        diagnosticCode: DiagnosticCode.InstanceClassNotFound,
                        message: $"Instance class '{classQualified}' not found for named instance '{instanceName}'.",
                        location: classLocation
                    );
                }
            }

            // ExposeToMauiDi
            var exposeTokenName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceExposeToMauiDi];
            var exposeLogicalPath = $"{childLogicalPath}/{tokenTypeName}/{exposeTokenName}/";
            _ = bool.TryParse(GetScalar(childYamlMappingNode, exposeTokenName), out var exposeToMauiDi);

            // F1: Optional factory binding (instance method on another named instance)
            var factoryInstanceTokenName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceFactoryInstance];
            var factoryInstanceLogicalPath = $"{childLogicalPath}/{tokenTypeName}/{factoryInstanceTokenName}/";
            var factoryInstanceName = GetScalar(childYamlMappingNode, factoryInstanceTokenName);
            if (!string.IsNullOrWhiteSpace(factoryInstanceName) && !factoryInstanceName.IsValidIdentifier())
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InvalidIdentifier,
                    message: $"factoryInstance '{factoryInstanceName}' is not a valid identifier.",
                    location: new Location(childYamlMappingNode, factoryInstanceLogicalPath)
                        with { LogicalPath = $"{factoryInstanceLogicalPath}[@{factoryInstanceTokenName}='{factoryInstanceName}']" }
                );
            }

            var factoryMethodTokenName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceFactoryMethod];
            var factoryMethodLogicalPath = $"{childLogicalPath}/{tokenTypeName}/{factoryMethodTokenName}/";
            var factoryMethodName = GetScalar(childYamlMappingNode, factoryMethodTokenName);
            if (!string.IsNullOrWhiteSpace(factoryMethodName) && !factoryMethodName.IsValidIdentifier())
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InvalidIdentifier,
                    message: $"factoryMethod '{factoryMethodName}' is not a valid identifier.",
                    location: new Location(childYamlMappingNode, factoryMethodLogicalPath)
                        with { LogicalPath = $"{factoryMethodLogicalPath}[@{factoryMethodTokenName}='{factoryMethodName}']" }
                );
            }

            if (string.IsNullOrWhiteSpace(factoryInstanceName) ^ string.IsNullOrWhiteSpace(factoryMethodName))
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InstanceInvalidNode,
                    message: $"If a named instance specifies factory binding, both '{factoryInstanceTokenName}' and '{factoryMethodTokenName}' must be set.",
                    location: new Location(childYamlMappingNode, $"{childLogicalPath}/{tokenTypeName}/")
                );
            }

            var canBind = instanceDiagnostics.Count == 0 && instanceClass is not null;

            // Register the instance name early so later references can be validated, even if
            // this instance has other diagnostics (class not found, etc.).
            if (!instanceName.IsWhiteSpace())
            {
                if (instanceDictionary.ContainsKey(instanceName!))
                {
                    instanceDiagnostics.Add(
                        diagnosticCode: DiagnosticCode.InstanceDuplicateName,
                        message: $"Named instance '{instanceName}' is defined more than once.",
                        location: instanceNameLocation
                    );

                    canBind = false;
                }
                else
                {
                    var placeholder = new Models.Instance(
                        instanceName: instanceName,
                        @class: instanceClass,
                        classToken: classToken,
                        instanceIsArray: instanceIsArray,
                        exposeToMauiDi: exposeToMauiDi,
                        factoryInstanceName: factoryInstanceName,
                        factoryMethodName: factoryMethodName,
                        assignments: Array.Empty<Models.Assignment>(),
                        elements: Array.Empty<Models.Element>(),
                        location: new Location(childYamlMappingNode, $"{childLogicalPath}/{tokenTypeName}/"),
                        diagnostics: instanceDiagnostics
                    );

                    instanceDictionary.Add(instanceName!, placeholder);
                }
            }

            localDiagnostics.AddRange(instanceDiagnostics);

            declared.Add((
                Node: childYamlMappingNode,
                LogicalPath: $"{childLogicalPath}/{tokenTypeName}/",
                Name: instanceName,
                Class: instanceClass,
                ClassToken: classToken,
                IsArray: instanceIsArray,
                ExposeToMauiDi: exposeToMauiDi,
                FactoryInstanceName: factoryInstanceName,
                FactoryMethodName: factoryMethodName,
                CanBind: canBind
            ));
        }

        foreach (var entry in declared)
        {
            if (!entry.CanBind || entry.Name.IsWhiteSpace() || entry.Class is null)
            {
                // Keep placeholder ordering; generation is expected to stop due to diagnostics.
                if (!entry.Name.IsWhiteSpace() && instanceDictionary.TryGetValue(entry.Name!, out var placeholder))
                    list.Add(placeholder);
                continue;
            }

            var instanceDiagnostics = new List<Diagnostic>();

            var assignments = ParseAssignments(
                yamlMappingNode: entry.Node,
                logicalPath: entry.LogicalPath,
                tokenPath: tokenPath,
                incomingDiagnostics: instanceDiagnostics,
                parameterDictionary: parameterDictionary,
                instanceDictionary: instanceDictionary,
                instanceClass: entry.Class
            );

            var elements = ParseElements(
                yamlMappingNode: entry.Node,
                logicalPath: entry.LogicalPath,
                tokenPath: tokenPath,
                incomingDiagnostics: instanceDiagnostics,
                instanceDictionary: instanceDictionary,
                instanceClass: entry.Class
            );

            if (!string.IsNullOrWhiteSpace(entry.FactoryInstanceName)
                && !instanceDictionary.ContainsKey(entry.FactoryInstanceName!))
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InstanceMissing,
                    message: $"factoryInstance '{entry.FactoryInstanceName}' was referenced but not found.",
                    location: new Location(entry.Node, entry.LogicalPath)
                );
            }

            if (!string.IsNullOrWhiteSpace(entry.FactoryInstanceName) && elements.Count > 0)
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InstanceBothAssignmentsAndElementsSet,
                    message: $"Named instance '{entry.Name}' in {entry.LogicalPath} cannot use both factory binding and elements.",
                    location: new Location(entry.Node, entry.LogicalPath)
                );
            }

            if (assignments.Count > 0 && elements.Count > 0)
            {
                instanceDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InstanceBothAssignmentsAndElementsSet,
                    message: $"Named instance '{entry.Name}' in {entry.LogicalPath} has both assignments and elements.",
                    location: new Location(entry.Node, entry.LogicalPath)
                );
            }

            localDiagnostics.AddRange(instanceDiagnostics);

            var instance = new Models.Instance(
                instanceName: entry.Name,
                @class: entry.Class,
                classToken: entry.ClassToken,
                instanceIsArray: entry.IsArray,
                exposeToMauiDi: entry.ExposeToMauiDi,
                factoryInstanceName: entry.FactoryInstanceName,
                factoryMethodName: entry.FactoryMethodName,
                assignments: assignments,
                elements: elements,
                location: new Location(entry.Node, entry.LogicalPath),
                diagnostics: instanceDiagnostics
            );

            instanceDictionary[entry.Name!] = instance;
            list.Add(instance);
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return list;
    }

    List<Models.Assignment> ParseAssignments(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        Dictionary<string, Models.Parameter> parameterDictionary,
        Dictionary<string, Models.Instance> instanceDictionary,
        Models.Class instanceClass
    )
    {
        var tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignments];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var list = new List<Models.Assignment>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null) return list;

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.AssignmentInvalidNode,
                    message: $"Assignment at {childLogicalPath} must be a mapping node.",
                    location: new Location(childNode, childLogicalPath)
                );
                continue;
            }

            Models.Assignment? dto;

            // F2: Compact literal assignment syntax.
            // Allow single-entry mappings like: `- maxBufferSize: 1000`.
            // This is literal-only; instance references remain in the expanded shape.
            if (TryParseCompactLiteralAssignment(
                yamlMappingNode: childYamlMappingNode,
                logicalPath: childLogicalPath,
                incomingDiagnostics: localDiagnostics,
                parameterDictionary: parameterDictionary,
                instanceClass: instanceClass,
                assignment: out var compactAssignment))
            {
                dto = compactAssignment;
            }
            else
            {
                dto = ParseAssignment(
                    yamlMappingNode: childYamlMappingNode,
                    logicalPath: childLogicalPath,
                    tokenPath: tokenPath,
                    incomingDiagnostics: localDiagnostics,
                    parameterDictionary: parameterDictionary,
                    instanceDictionary: instanceDictionary,
                    instanceClass: instanceClass
                );
            }

            if (dto is not null) list.Add(dto);
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return list;
    }

    private static bool TryParseCompactLiteralAssignment(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        List<Diagnostic> incomingDiagnostics,
        Dictionary<string, Models.Parameter> parameterDictionary,
        Models.Class instanceClass,
        out Models.Assignment? assignment)
    {
        assignment = null;

        // Compact form must be exactly one key/value pair.
        if (yamlMappingNode.Children.Count != 1)
            return false;

        var kvp = yamlMappingNode.Children.First();

        if (kvp.Key is not YamlScalarNode keyScalar)
            return false;

        var instanceClassQualified = instanceClass.ClassQualified!;

        var parameterName = keyScalar.Value;
        if (string.IsNullOrWhiteSpace(parameterName))
            return false;

        // v1: literal-only scalar values.
        if (kvp.Value is not YamlScalarNode valueScalar)
        {
            incomingDiagnostics.Add(new Diagnostic(
                diagnosticCode: DiagnosticCode.AssignmentInvalidNode,
                message: $"Compact assignment for '{parameterName}' at {logicalPath} must be a scalar literal value.",
                location: new Location(kvp.Value, logicalPath)));

            return true;
        }

        // Disambiguation: compact mappings can legitimately use reserved keys like `name:`
        // when the target parameter itself is named `name`.
        // If the value corresponds to a real parameter on the instance class, treat it as
        // the expanded form and let ParseAssignment() handle it.
        if (string.Equals(parameterName, Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentName], StringComparison.Ordinal))
        {
            var expandedParameterNameCandidate = valueScalar.Value;
            if (!string.IsNullOrWhiteSpace(expandedParameterNameCandidate)
                && parameterDictionary.ContainsKey($"{instanceClassQualified}.{expandedParameterNameCandidate}"))
            {
                return false;
            }

            parameterName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentName];
        }
        else if (string.Equals(parameterName, Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentLiteral], StringComparison.Ordinal)
              || string.Equals(parameterName, Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstance], StringComparison.Ordinal)
              || string.Equals(parameterName, Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstanceProperty], StringComparison.Ordinal))
        {
            // Avoid ambiguity with expanded assignment keys.
            return false;
        }

        var parameter = parameterDictionary.GetValueOrDefault($"{instanceClassQualified}.{parameterName}");
        if (parameter is null)
        {
            incomingDiagnostics.Add(new Diagnostic(
                diagnosticCode: DiagnosticCode.AssignmentParameterNotFound,
                message: $"Assignment parameterName '{parameterName}' not found in class '{instanceClassQualified}'.",
                location: new Location(keyScalar, logicalPath)));

            return true;
        }

        if (!parameterName.IsValidIdentifier())
        {
            incomingDiagnostics.Add(new Diagnostic(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"Assignment parameterName '{parameterName}' must be a simple identifier (no namespace).",
                location: new Location(keyScalar, logicalPath)));

            return true;
        }

        var assignmentLiteral = valueScalar.Value;
        var haveAssignmentLiteral = assignmentLiteral is not null;
        var assignmentLiteralInferredClass = haveAssignmentLiteral ? assignmentLiteral.InferredClass() : null;

        if (assignmentLiteralInferredClass is not null)
        {
            if (parameter.Class != @"String" && parameter.Class != assignmentLiteralInferredClass)
            {
                incomingDiagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.AssignmentLiteralTypeMismatch,
                    message: $"Assignment literal '{assignmentLiteral}' inferred type '{assignmentLiteralInferredClass}' does not match parameter '{parameterName}' type '{parameter.Class}'.",
                    location: new Location(valueScalar, logicalPath)));

                return true;
            }
        }

        // Initializer Parameter Assignment Clause (mirrors ParseAssignment())
        string? initializerParameterAssignmentClause = null;
        if (haveAssignmentLiteral)
        {
            if (!parameter.IsArray)
            {
                if (parameter.ClassQualified!.Equals("System.String", StringComparison.OrdinalIgnoreCase))
                    assignmentLiteral = $"\"{assignmentLiteral}\"";

                initializerParameterAssignmentClause = $"{parameterName}: {assignmentLiteral}";
            }
            else
            {
                if (assignmentLiteral == @"[]")
                    initializerParameterAssignmentClause = $"{parameterName}: Array.Empty<{parameter.ClassQualified}>()";
                else
                {
                    incomingDiagnostics.Add(new Diagnostic(
                        diagnosticCode: DiagnosticCode.AssignmentLiteralArrayNotSupported,
                        message: $"Array parameter '{parameterName}' at {logicalPath} does not support literal assignments. Use instance assignment for arrays.",
                        location: new Location(valueScalar, logicalPath)));
                    return true;
                }
            }
        }
        else
        {
            if (parameter.IsNullable)
                initializerParameterAssignmentClause = $"{parameterName}: null";
            else
            {
                incomingDiagnostics.Add(new Diagnostic(
                    diagnosticCode: DiagnosticCode.AssignmentNoValueOrInstance,
                    message: $"Assignment at {logicalPath} must specify either literal, instance or instanceProperty.",
                    location: new Location(valueScalar, logicalPath)));
                return true;
            }
        }

        assignment = new Models.Assignment(
            name: parameterName,
            literal: assignmentLiteral,
            literalInferredClass: assignmentLiteralInferredClass,
            instance: null,
            instancePropertyPath: null,
            initializerParameterAssignmentClause: initializerParameterAssignmentClause,
            parameter: parameter,
            location: new Location(yamlMappingNode, logicalPath),
            diagnostics: Array.Empty<Diagnostic>());

        return true;
    }
    
    private Models.Assignment? ParseAssignment(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        Dictionary<string, Models.Parameter> parameterDictionary,
        Dictionary<string, Models.Instance> instanceDictionary,
        Models.Class instanceClass
    )
    {
        var type = typeof(Models.Assignment);        
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();
        localDiagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode, 
                dtoType: type, 
                logicalPath: logicalPath
            )
        );

        // Assignment/Parameter Name
        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentName];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var parameterName = GetScalar(yamlMappingNode, tokenTypeName);
        bool haveParameterName = !parameterName.IsWhiteSpace();
        if (parameterName.IsWhiteSpace())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.AssignmentMissingParameterName,
                message: $"Missing 'parameterName' in {logicalPath}.",
                location: location
            );
        }
        else if (!parameterName.IsValidIdentifier())
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.InvalidIdentifier,
                message: $"Assignment parameterName '{parameterName}' must be a simple " +
                         "identifier (no namespace).",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{parameterName}']" }
            );
        }

        var instanceClassQualified = instanceClass.ClassQualified!;
        var parameter = parameterDictionary.GetValueOrDefault($"{instanceClassQualified}.{parameterName}");
        var foundParameter = parameter is not null;

        if (haveParameterName && !foundParameter)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.AssignmentParameterNotFound,
                message: $"Assignment parameterName '{parameterName}' not found in class '{instanceClassQualified}'.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{parameterName}']" }
            );
        }

        // Literal
        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentLiteral];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var assignmentLiteral = GetScalar(yamlMappingNode, tokenTypeName);
        var haveAssignmentLiteral = assignmentLiteral is not null;
        var assignmentLiteralInferredClass = haveAssignmentLiteral ? assignmentLiteral.InferredClass() : null;
        if (parameter is not null && assignmentLiteralInferredClass is not null)
        {
            if (parameter.Class != @"String" &&  parameter.Class != assignmentLiteralInferredClass)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.AssignmentLiteralTypeMismatch,
                    message: $"Assignment literal '{assignmentLiteral}' inferred type '{assignmentLiteralInferredClass}' " +
                             $"does not match parameter '{parameterName}' type '{parameter.Class}'.",
                    location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{assignmentLiteral}']" }
                );
            }
        }

        // Parameter Instance
        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstance];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var parameterInstance = GetScalar(yamlMappingNode, tokenTypeName);
        var parameterInstanceParts = string.IsNullOrWhiteSpace(parameterInstance)
            ? Array.Empty<string>()
            : parameterInstance.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var haveParameterInstanceName = parameterInstanceParts.Length > 0;
        var parameterInstanceName = haveParameterInstanceName ? parameterInstanceParts[0] : string.Empty;

        var parameterInstancePropertyPath = parameterInstanceParts.Length > 1
            ? string.Join('.', parameterInstanceParts[1..])
            : null;

        var haveParameterInstancePropertyPath = !string.IsNullOrWhiteSpace(parameterInstancePropertyPath);

        if (!string.IsNullOrWhiteSpace(parameterInstanceName))
        {
            haveParameterInstanceName = true;
            if (!parameterInstanceName.IsValidIdentifier())
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.InvalidIdentifier,
                    message: $"NamedInstanceName '{parameterInstanceName}' is not a valid identifier.",
                    location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{parameterInstanceName}']" }
                );
            }
            else if (!parameterInstanceName.IsPascalCase())
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.IdentifierNotPascalCase,
                    message: $"NamedInstanceName '{parameterInstanceName}' is not in PascalCase.",
                    location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{parameterInstanceName}']" }
                );
            }
        }

        if (haveParameterInstancePropertyPath)
        {
            foreach (var segment in parameterInstanceParts[1..])
            {
                if (!segment.IsValidIdentifier())
                {
                    localDiagnostics.Add(
                        diagnosticCode: DiagnosticCode.InvalidIdentifier,
                        message: $"Instance property path segment '{segment}' is not a valid identifier.",
                        location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{parameterInstance}']" }
                    );
                    break;
                }
            }
        }

        if (haveParameterInstanceName && !parameterInstanceName.IsWhiteSpace())
        {
            if (!instanceDictionary.ContainsKey(parameterInstanceName!))
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.AssignmentInstanceNotFound,
                    message: $"NamedInstanceName '{parameterInstanceName}' was referenced but not found.",
                    location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{parameterInstance}']" }
                );
            }
        }

        var assignmentCount = haveAssignmentLiteral ? 1 : 0;
        assignmentCount += haveParameterInstanceName ? 1 : 0;

        if (assignmentCount == 0)
        {
            if (foundParameter && !parameter!.IsNullable)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.AssignmentNoValueOrInstance,
                    message: $"Assignment at {logicalPath} must specify either literal, instance or instanceProperty.",
                    location: location
                );
            }
        }
        else if (assignmentCount > 1)
        {
            localDiagnostics.Add(
                diagnosticCode: DiagnosticCode.AssignmentMoreThanOneAssignmentTypeForParameter,
                message: $"Assignment at {logicalPath} must use only one of literal, instance or instanceProperty.",
                location: location
            );
        }

        // Initializer Parameter Assignment Clause
        string? initializerParameterAssignmentClause = null;
        if (localDiagnostics.Count == 0)
        {
            if (haveAssignmentLiteral)
                if (!parameter!.IsArray)
                {
                    if (parameter.ClassQualified!.Equals("System.String", StringComparison.OrdinalIgnoreCase))
                        assignmentLiteral = $"\"{assignmentLiteral}\"";
                    initializerParameterAssignmentClause = $"{parameterName}: {assignmentLiteral}";
                }
                else
                    if (assignmentLiteral == @"[]")
                    initializerParameterAssignmentClause = $"{parameterName}: Array.Empty<{parameter.ClassQualified}>()";
                else
                    localDiagnostics.Add(
                        diagnosticCode: DiagnosticCode.AssignmentLiteralArrayNotSupported,
                        message: $"Array parameter '{parameterName}' at {logicalPath} does not support literal assignments. " +
                                "Use instance assignment for arrays.",
                        location: location
                    );
            else if (haveParameterInstanceName)
            {
                initializerParameterAssignmentClause = parameter!.IsArray
                    // ToDo: Kludge: More generation logic where it doesn't belong. Need to refactor later.
                    ? $"{parameterName}: registry.Get{parameterInstanceName}_Internal()" 
                    : $"{parameterName}: registry.Get{parameterInstanceName}()";

                if (haveParameterInstancePropertyPath)
                    initializerParameterAssignmentClause = $"{initializerParameterAssignmentClause}.{parameterInstancePropertyPath}";
            }
            else
                // No literal or instance specified
                if (parameter!.IsNullable)
                initializerParameterAssignmentClause = $"{parameterName}: null";
            else
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.AssignmentNoValueOrInstance,
                    message: $"Non nullable parameter null Assignment at {logicalPath} must specify either assignedValue or assignedNamedInstance.",
                    location: location
                );
        }

        incomingDiagnostics.AddRange(localDiagnostics);

        if (parameter is null)
            return null;

        return new Models.Assignment(
            name: parameterName,
            literal: assignmentLiteral,
            literalInferredClass: assignmentLiteralInferredClass,
            instance: parameterInstanceName,
            instancePropertyPath: parameterInstancePropertyPath,
            initializerParameterAssignmentClause: initializerParameterAssignmentClause,
            parameter: parameter!,
            location: location,
            diagnostics: localDiagnostics
        );
    }
    List<Models.Element> ParseElements(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        string tokenPath,
        List<Diagnostic> incomingDiagnostics,
        Dictionary<string, Models.Instance> instanceDictionary,
        Models.Class instanceClass
    )
    {
        var type = typeof(Models.Element);
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var localDiagnostics = new List<Diagnostic>();

        var list = new List<Models.Element>();
        var yamlSequenceNode = GetChildSequence(yamlMappingNode, tokenTypeName);
        if (yamlSequenceNode is null) return list;

        for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
        {
            var childLogicalPath = $"{logicalPath}.{tokenTypeName}[{i}]";
            var childNode = yamlSequenceNode.Children[i];
            var childYamlMappingNode = childNode as YamlMappingNode;
            if (childYamlMappingNode is null)
            {
                localDiagnostics.Add(
                    diagnosticCode: DiagnosticCode.ElementInvalidNode,
                    message: $"Element at {childLogicalPath} must be a mapping node.",
                    location: new Location(childNode, childLogicalPath)
                );
                continue;
            }

            var dto = ParseElement(
                yamlMappingNode: childYamlMappingNode,
                logicalPath: childLogicalPath,
                incomingDiagnostics: localDiagnostics,
                instanceDictionary: instanceDictionary,
                instanceClass: instanceClass
            );

            if (dto is not null) list.Add(dto);
        }

        incomingDiagnostics.AddRange(localDiagnostics);
        return list;
    }
    Models.Element ParseElement(
        YamlMappingNode yamlMappingNode,
        string logicalPath,
        List<Diagnostic> incomingDiagnostics,
        Dictionary<string, Models.Instance> instanceDictionary,
        Models.Class instanceClass
    )
    {
        var type = typeof(Models.Element);        
        var tokenTypeName = Models.Schema.TypeToTokenName[type];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        var location = new Location(yamlMappingNode, logicalPath);
        var diagnostics = new List<Diagnostic>();
        diagnostics.AddRange(
            ValidateMappingKeys(
                yamlMappingNode: yamlMappingNode, 
                dtoType: type, 
                logicalPath: logicalPath
            )
        );

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceElementLiteral];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var literal = GetScalar(yamlMappingNode, tokenTypeName);
        var haveLiteral = !literal.IsWhiteSpace();;

        tokenTypeName = Models.Schema.TokenTypeToName[TokenTypes.instancesInstanceElementInstance];
        logicalPath = $"{logicalPath}/{tokenTypeName}/";
        location = new Location(yamlMappingNode, logicalPath);
        var instance = GetScalar(yamlMappingNode, tokenTypeName);
        var haveInstance = false;
        if (!instance.IsWhiteSpace())
        {
            if (!instance.IsValidIdentifier())
            {
                diagnostics.Add(
                    diagnosticCode: DiagnosticCode.InvalidIdentifier,
                    message: $"NamedInstanceName '{instance}' is not a valid identifier.",
                    location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{instance}']" }
                );
            }
            else if (!instance.IsPascalCase())
            {
                diagnostics.Add(
                    diagnosticCode: DiagnosticCode.IdentifierNotPascalCase,
                    message: $"NamedInstanceName '{instance}' is not in PascalCase.",
                    location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{instance}']" }
                );
            }
            else
                haveInstance = true;
        }

        if (haveInstance && !instanceDictionary.ContainsKey(instance!))
        {
            diagnostics.Add(
                diagnosticCode: DiagnosticCode.InstanceMissing,
                message: $"NamedInstanceName '{instance}' was referenced but not found.",
                location: location with { LogicalPath = $"{logicalPath}[@{tokenTypeName}='{instance}']" }
            );
        }

        if (!haveLiteral && !haveInstance) 
        {
            diagnostics.Add(
                diagnosticCode: DiagnosticCode.ElementMissingValue,
                message: $"Element at {logicalPath} must specify either assignedValue or assignedNamedInstance.",
                location: location
            );
        }
        else if (haveLiteral && haveInstance)
        {
            diagnostics.Add(
                diagnosticCode: DiagnosticCode.ElementBothValueAndInstance,
                message: $"Element at {logicalPath} cannot specify both assignedValue and assignedNamedInstance.",
                location: location
            );
        }

        incomingDiagnostics.AddRange(diagnostics);

        return new Models.Element(
            literal: literal,
            instance: instance,
            instanceClass: instanceClass,
            location: location,
            diagnostics: diagnostics
        );
    }

    static (
        Dictionary<string, Models.Namespace> NamespaceDictionary,
        Dictionary<string, Models.Class> ClassDictionary,
        Dictionary<string, Models.Interface> InterfaceDictionary,
        Dictionary<string, Models.Parameter> ParameterDictionary
    ) BuildDictionaries(
        IReadOnlyList<Models.Namespace> namespaces,
        List<Diagnostic> diagnostics
    )
    {
        Dictionary<string, Models.Namespace> namespaceDictionary = new();
        Dictionary<string, Models.Class> classDictionary = new(); 
        Dictionary<string, Models.Interface> interfaceDictionary = new();
        Dictionary<string, Models.Parameter> parameterDictionary = new();

        if (diagnostics.Count > 0)
        {
            return (
                namespaceDictionary,
                classDictionary,
                interfaceDictionary,
                parameterDictionary
            );
        }

        foreach (var @namespace in namespaces)
        {
            if (namespaceDictionary.ContainsKey(@namespace.NamespaceName))
            {
                diagnostics.Add(
                    diagnosticCode: DiagnosticCode.DuplicateNamespace,
                    message: $"Duplicate namespace '{@namespace.NamespaceName}' found.",
                    location: @namespace.Location
                );
                continue;
            }
            else
                namespaceDictionary.Add(@namespace.NamespaceName, @namespace);

            foreach (var @class in @namespace.Classes)
            {                
                if (classDictionary.ContainsKey(@class.ClassQualified!))
                {
                    diagnostics.Add(
                        diagnosticCode: DiagnosticCode.DuplicateClass,
                        message: $"Duplicate class '{@class.ClassQualified}' found.",
                        location: @class.Location
                    );
                    continue;
                }
                else
                    classDictionary.Add(@class.ClassQualified!, @class);

                foreach (var parameter in @class.Parameters)
                {
                    var parameterKey = $"{@class.ClassQualified}.{parameter.Value.ParameterName}";
                    if (parameterDictionary.ContainsKey(parameterKey))
                        diagnostics.Add(
                            diagnosticCode: DiagnosticCode.DuplicateParameter,
                            message: $"Duplicate parameter '{parameterKey}' found.",
                            location: parameter.Value.Location
                        );
                        else
                            parameterDictionary.Add(parameterKey, parameter.Value);
                }
            }

            foreach (var @interface in @namespace.Interfaces)
            {
                if (interfaceDictionary.ContainsKey(@interface.InterfaceQualified!))
                {
                    diagnostics.Add(
                        diagnosticCode: DiagnosticCode.DuplicateInterface,
                        message: $"Duplicate interface '{@interface.InterfaceQualified}' found.",
                        location: @interface.Location
                    );
                    continue;
                }
                else
                    interfaceDictionary.Add(@interface.InterfaceQualified!, @interface);
            }
        }
        return (
            namespaceDictionary,
            classDictionary,
            interfaceDictionary,
            parameterDictionary
        );
    }
    // Safe scalar extraction helper
    private static string? GetScalar(YamlMappingNode yamlMappingNode, string key)
    {
        if (yamlMappingNode.Children
            .TryGetValue(
                new YamlScalarNode(key), out var value
            )
            && value is YamlScalarNode scalar
        )
        {
            if (scalar.Value is null) return null;
            var trimmedScalarValue = scalar.Value.Trim();
            if (trimmedScalarValue.Length == 0) return null;
            return trimmedScalarValue == @"null" ? null : trimmedScalarValue;
        }

        return null;
    }

    // Return null when child sequence is missing
    private static YamlSequenceNode? GetChildSequence(YamlMappingNode yamlMappingNode, string key)
        => yamlMappingNode.Children.TryGetValue(
            new YamlScalarNode(key), out var value)
                && value is YamlSequenceNode seq ? seq : null;

    // Return null when child mapping is missing
    private static YamlMappingNode? GetChildMapping(YamlMappingNode yamlMappingNode, string key)
        => yamlMappingNode.Children.TryGetValue(new YamlScalarNode(key), out var value) && value is YamlMappingNode map ? map : null;


    // Validate mapping keys against schema and return diagnostics
    private IReadOnlyList<Diagnostic> ValidateMappingKeys(
        YamlMappingNode yamlMappingNode,
        Type dtoType,
        string logicalPath
    )
    {
        var diagnostics = new List<Diagnostic>();

        if (!Models.Schema.AllowedKeys.TryGetValue(dtoType, out var allowed)) return diagnostics;

        var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal);

        foreach (var kv in yamlMappingNode.Children)
        {
            if (kv.Key is not YamlScalarNode keyScalar) continue;
            var key = keyScalar.Value ?? string.Empty;
            if (!allowedSet.Contains(key))
            {
                // Use DiagnosticsHelper so location and provenance are consistent
                var documentLocation = new Location(keyScalar, logicalPath);
                diagnostics.Add(
                    diagnosticCode: DiagnosticCode.UnrecognizedToken,
                    message: $"Unrecognized token '{key}' at {logicalPath}. Allowed keys: {string.Join(", ", allowed)}",
                    location: documentLocation
                );
            }
        }

        return diagnostics.ToList().AsReadOnly();
    }
}
