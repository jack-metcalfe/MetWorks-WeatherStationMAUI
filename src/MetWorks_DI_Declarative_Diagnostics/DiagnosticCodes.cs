namespace MetWorks.DI.Declarative.Diagnostics;
public enum DiagnosticCode
{
    // Provenance (PROVxxx)
    ProvenanceMissingEntries,
    ProvenanceMissingLogicalPath,
    ProvenanceInvalidVersion,

    // Namespaces (NSxxx)
    NamespacesMissing,
    NamespaceMissingName,
    NamespaceInvalidSegment,        // segment violates identifier rules
    NamespaceEmptySegment,          // e.g., "Company..Product"
    NamespaceReservedSegment,       // segment is a C# keyword
    NamespaceNameMustBeSimple,    // namespace name must not be a single identifier
    NamespaceInvalidNode,
    DuplicateNamespace,

    // Identifiers (IDxxx)
    InvalidIdentifier,              // general identifier pattern violation
    IdentifierNotPascalCase,     // identifier is not in PascalCase
    ReservedIdentifier,             // identifier is a C# keyword/contextual keyword
    DuplicateTypeIdentifier,        // same (namespace, name) used by multiple types
    DuplicateInstanceIdentifier,    // duplicate named instance in same namespace

    // Interfaces (IFACExxx)
    InterfacesMissing,
    InterfacesMissingSequenceNode,
    InterfaceMissingName,
    InterfaceNameInvalidFormat, // interface name does not conform to expected format
    InterfaceMissingQualifiedName,
    InterfaceInvalidNode,
    InterfaceNameMustBeSimple,    // interface name must not be qualified
    DuplicateInterface,

    // Classes (CLASSxxx)
    ClassMissingName,
    ClassBothClassAndInterfaceSet,
    ClassNeitherClassNorInterfaceSet,
    ClassMissingParameters,
    ClassInterfaceMustBeQualified,
    ClassNameMustBeSimple,    // class name must not be qualified
    ClassInvalidNode,
    ClassesMissing,
    ClassesNotSequenceNode,
    DuplicateClass,

    // Parameters (PARAMxxx)
    ParametersMissing,
    ParameterMissingName,
    ParameterMissingQualifiedClass,
    ParameterMissingQualifiedInterface,
    ParameterBothClassAndInterface,
    ParameterMissingClassOrInterface,
    ParameterMustBeInterfaceForNonPrimitive,
    ParameterInvalidNode,
    DuplicateParameter,

    // Properties (PARAMxxx)
    PropertiesMissing,
    PropertyMissingName,
    PropertyMissingQualifiedClass,
    PropertyMissingQualifiedInterface,
    PropertyBothClassAndInterface,
    PropertyMissingClassOrInterface,
    PropertyMustBeInterfaceForNonPrimitive,
    PropertyInvalidNode,
    DuplicateProperty,

    // Named Instances (NIxxx)
    InstanceClassNotFound,       // referenced class not found
    InstanceMissingName,
    InstanceMissingQualifiedClass,
    InstanceBothAssignmentsAndElementsSet,
    InstanceDuplicateName,      // mirrors ID004 but scoped to instances
    InstanceMissing,               // referenced named instance not found
    InstanceMissingOrNotExposingInterface, // referenced named instance not found or does not expose required interface
    InstanceInvalidNode,

    // Assignments (ASSIGNxxx)
    AssignmentInvalidLiteral,
    AssignmentLiteralArrayNotSupported,
    AssignmentNoValueOrInstance,
    AssignmentMoreThanOneAssignmentTypeForParameter,
    AssignmentMissingParameterName,
    AssignmentMissingValueOrInstance,
    AssignmentInvalidNode,
    AssignmentParameterNotFound,
    AssignmentLiteralTypeMismatch,
    AssignmentInstanceNotFound,

    // Elements (ELEMxxx)
    ElementMissingValueOrInstance,
    ElementInvalidNode,
    ElementMissingValue,            // value missing from element definition
    ElementMissingNamedInstance,   // named instance missing from element definition
    ElementBothValueAndInstance,
    // CodeGen config (CODEGENxxx)
    CodeGenMissing,
    CodeGenMissingRegistryClass,
    CodeGenMissingGeneratedPath,
    CodeGenMissingNamespace,
    CodeGenMissingInitializer,

    // Generator tokens/templates (GENxxx)
    UnrecognizedToken,
    MissingTemplate,                // requested template not found
    MissingPlaceholderValue,        // placeholder lacked a DTO value
    DuplicateInvokerKey,              // same invoker key used multiple times
    DependencyOrderViolation,        // generator dependency ordering is invalid
    DependencyCycleDetected,         // generator detected a dependency cycle
    InitializerSignatureTypeNotFound, // concrete type could not be resolved for initializer signature validation
    InitializerSignatureMethodNotFound, // initializer method not found on concrete type
    InitializerSignatureMethodAmbiguous, // multiple initializer overloads match validation criteria
    InitializerSignatureParameterMissing, // YAML assignment parameter missing from initializer signature
    InitializerSignatureParameterTypeMismatch, // initializer parameter type differs from YAML model
    InitializerSignatureMissingRequiredParameter, // initializer signature contains required parameter not supplied by YAML
    DottedPropertyTypeNotFound, // dotted-property validation could not resolve a concrete type in the chain
    DottedPropertyYamlTypeNotFound, // dotted-property validation could not resolve a YAML class type in the chain
    DottedPropertyYamlPropertyNotDeclared, // dotted-property segment is not declared in YAML model
    DottedPropertyYamlPropertyTypeNotDeclared, // dotted-property segment type is not declared in YAML model (cannot continue chain)
    DottedPropertyConcretePropertyNotFound, // dotted-property segment is not found on the reflected concrete type
    DottedPropertyConcretePropertyTypeMismatch, // dotted-property segment type differs between YAML model and reflected concrete type
    DottedPropertyInterfacePropertyNotFound, // dotted-property segment is not found on the exposed interface type
    FactoryBindingInstanceNotFound, // factory binding references a factoryInstance that is not declared
    FactoryBindingTypeNotFound, // factory binding factoryInstance type could not be resolved
    FactoryBindingMethodNotFound, // factory binding method not found on factory type
    FactoryBindingMethodAmbiguous, // factory binding method has multiple overloads
    FactoryBindingMethodHasParameters, // factory binding method must be parameterless in v1
    FactoryBindingReturnTypeMismatch, // factory binding method return type does not match instance type
    TypeRefInvalid,               // type reference string is malformed
    DtoValidationException,               // exception thrown during DTO validation
    TemplateUnresolvedPlaceholder,      // template placeholder could not be resolved
    YamlEmptyDocument,
    YamlRootNodeNotMapping,
    RootYamlDocumentMissing,
    YamlParseError,

}
public static class DiagnosticCodeInfo
{
    private static readonly IReadOnlyDictionary<DiagnosticCode, DiagnosticSeverity> _severityMap =
        new Dictionary<DiagnosticCode, DiagnosticSeverity>
        {
        // Provenance
        { DiagnosticCode.ProvenanceMissingEntries, DiagnosticSeverity.Error },
        { DiagnosticCode.ProvenanceMissingLogicalPath, DiagnosticSeverity.Error },
        { DiagnosticCode.ProvenanceInvalidVersion, DiagnosticSeverity.Error },

        // Namespaces
        { DiagnosticCode.NamespacesMissing, DiagnosticSeverity.Error },
        { DiagnosticCode.NamespaceMissingName, DiagnosticSeverity.Error },
        { DiagnosticCode.NamespaceInvalidSegment, DiagnosticSeverity.Error },
        { DiagnosticCode.NamespaceEmptySegment, DiagnosticSeverity.Error },
        { DiagnosticCode.NamespaceReservedSegment, DiagnosticSeverity.Warning },
        { DiagnosticCode.NamespaceNameMustBeSimple, DiagnosticSeverity.Error },
        { DiagnosticCode.NamespaceInvalidNode, DiagnosticSeverity.Error },
        { DiagnosticCode.DuplicateNamespace, DiagnosticSeverity.Error },

        // Identifiers
        { DiagnosticCode.InvalidIdentifier, DiagnosticSeverity.Error },
        { DiagnosticCode.IdentifierNotPascalCase, DiagnosticSeverity.Error },
        { DiagnosticCode.ReservedIdentifier, DiagnosticSeverity.Warning },
        { DiagnosticCode.DuplicateTypeIdentifier, DiagnosticSeverity.Error },
        { DiagnosticCode.DuplicateInstanceIdentifier, DiagnosticSeverity.Error },

        // Interfaces        
        { DiagnosticCode.InterfacesMissing, DiagnosticSeverity.Error },
        { DiagnosticCode.InterfacesMissingSequenceNode, DiagnosticSeverity.Error },
        { DiagnosticCode.InterfaceNameInvalidFormat, DiagnosticSeverity.Error },
        { DiagnosticCode.InterfaceMissingName, DiagnosticSeverity.Error },
        { DiagnosticCode.InterfaceMissingQualifiedName, DiagnosticSeverity.Error },
        { DiagnosticCode.InterfaceInvalidNode, DiagnosticSeverity.Error },
        { DiagnosticCode.InterfaceNameMustBeSimple, DiagnosticSeverity.Error },
        { DiagnosticCode.DuplicateInterface, DiagnosticSeverity.Error },

        // Classes
        { DiagnosticCode.ClassesMissing, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassesNotSequenceNode, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassMissingName, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassBothClassAndInterfaceSet, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassNeitherClassNorInterfaceSet, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassMissingParameters, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassInterfaceMustBeQualified, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassNameMustBeSimple, DiagnosticSeverity.Error },
        { DiagnosticCode.ClassInvalidNode, DiagnosticSeverity.Error },
        { DiagnosticCode.DuplicateClass, DiagnosticSeverity.Error },

        // Parameters
        { DiagnosticCode.ParametersMissing, DiagnosticSeverity.Error },
        { DiagnosticCode.ParameterMissingName, DiagnosticSeverity.Error },
        { DiagnosticCode.ParameterMissingQualifiedClass, DiagnosticSeverity.Error },
        { DiagnosticCode.ParameterMissingQualifiedInterface, DiagnosticSeverity.Error },
        { DiagnosticCode.ParameterBothClassAndInterface, DiagnosticSeverity.Error },
        { DiagnosticCode.ParameterMissingClassOrInterface, DiagnosticSeverity.Error },
        { DiagnosticCode.ParameterMustBeInterfaceForNonPrimitive, DiagnosticSeverity.Error },
        { DiagnosticCode.ParameterInvalidNode, DiagnosticSeverity.Error },
        { DiagnosticCode.DuplicateParameter, DiagnosticSeverity.Error },

        // Named Instances
        { DiagnosticCode.InstanceClassNotFound, DiagnosticSeverity.Error },
        { DiagnosticCode.InstanceMissingName, DiagnosticSeverity.Error },
        { DiagnosticCode.InstanceMissingQualifiedClass, DiagnosticSeverity.Error },
        { DiagnosticCode.InstanceBothAssignmentsAndElementsSet, DiagnosticSeverity.Error },
        { DiagnosticCode.InstanceDuplicateName, DiagnosticSeverity.Error },
        { DiagnosticCode.InstanceMissing, DiagnosticSeverity.Error },
        { DiagnosticCode.InstanceMissingOrNotExposingInterface, DiagnosticSeverity.Error },
        { DiagnosticCode.InstanceInvalidNode, DiagnosticSeverity.Error },

        // Assignments
        { DiagnosticCode.AssignmentMissingParameterName, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentNoValueOrInstance, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentMoreThanOneAssignmentTypeForParameter, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentInvalidLiteral, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentLiteralArrayNotSupported, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentInvalidNode, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentParameterNotFound, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentLiteralTypeMismatch, DiagnosticSeverity.Error },
        { DiagnosticCode.AssignmentInstanceNotFound, DiagnosticSeverity.Error },

        // Elements
        { DiagnosticCode.ElementMissingValueOrInstance, DiagnosticSeverity.Error },
        { DiagnosticCode.ElementInvalidNode, DiagnosticSeverity.Error },
        { DiagnosticCode.ElementMissingValue, DiagnosticSeverity.Error },
        { DiagnosticCode.ElementMissingNamedInstance, DiagnosticSeverity.Error },
        { DiagnosticCode.ElementBothValueAndInstance, DiagnosticSeverity.Error },

        // CodeGen
        { DiagnosticCode.CodeGenMissing, DiagnosticSeverity.Error },
        { DiagnosticCode.CodeGenMissingRegistryClass, DiagnosticSeverity.Error },
        { DiagnosticCode.CodeGenMissingGeneratedPath, DiagnosticSeverity.Error },
        { DiagnosticCode.CodeGenMissingNamespace, DiagnosticSeverity.Error },
        { DiagnosticCode.CodeGenMissingInitializer, DiagnosticSeverity.Error },

        // Generator
        { DiagnosticCode.UnrecognizedToken, DiagnosticSeverity.Error },
        { DiagnosticCode.MissingTemplate, DiagnosticSeverity.Error },
        { DiagnosticCode.MissingPlaceholderValue, DiagnosticSeverity.Error },
        { DiagnosticCode.DuplicateInvokerKey, DiagnosticSeverity.Error },
        { DiagnosticCode.DependencyOrderViolation, DiagnosticSeverity.Error },
         { DiagnosticCode.DependencyCycleDetected, DiagnosticSeverity.Error },
         { DiagnosticCode.InitializerSignatureTypeNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.InitializerSignatureMethodNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.InitializerSignatureMethodAmbiguous, DiagnosticSeverity.Error },
         { DiagnosticCode.InitializerSignatureParameterMissing, DiagnosticSeverity.Error },
         { DiagnosticCode.InitializerSignatureParameterTypeMismatch, DiagnosticSeverity.Error },
         { DiagnosticCode.InitializerSignatureMissingRequiredParameter, DiagnosticSeverity.Error },
         { DiagnosticCode.DottedPropertyTypeNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.DottedPropertyYamlTypeNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.DottedPropertyYamlPropertyNotDeclared, DiagnosticSeverity.Error },
         { DiagnosticCode.DottedPropertyYamlPropertyTypeNotDeclared, DiagnosticSeverity.Error },
         { DiagnosticCode.DottedPropertyConcretePropertyNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.DottedPropertyConcretePropertyTypeMismatch, DiagnosticSeverity.Error },
         { DiagnosticCode.DottedPropertyInterfacePropertyNotFound, DiagnosticSeverity.Warning },
         { DiagnosticCode.FactoryBindingInstanceNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.FactoryBindingTypeNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.FactoryBindingMethodNotFound, DiagnosticSeverity.Error },
         { DiagnosticCode.FactoryBindingMethodAmbiguous, DiagnosticSeverity.Error },
         { DiagnosticCode.FactoryBindingMethodHasParameters, DiagnosticSeverity.Error },
         { DiagnosticCode.FactoryBindingReturnTypeMismatch, DiagnosticSeverity.Error },
        { DiagnosticCode.TypeRefInvalid, DiagnosticSeverity.Error },
        { DiagnosticCode.DtoValidationException, DiagnosticSeverity.Error },
        { DiagnosticCode.TemplateUnresolvedPlaceholder, DiagnosticSeverity.Error },
        { DiagnosticCode.YamlEmptyDocument, DiagnosticSeverity.Error },
        { DiagnosticCode.YamlRootNodeNotMapping, DiagnosticSeverity.Error },
        { DiagnosticCode.RootYamlDocumentMissing, DiagnosticSeverity.Error },
        { DiagnosticCode.YamlParseError, DiagnosticSeverity.Error },
        };

    private static readonly IReadOnlyDictionary<DiagnosticCode, string> _messageMap =
        new Dictionary<DiagnosticCode, string>
        {
            // Provenance
            { DiagnosticCode.ProvenanceMissingEntries, "Provenance stack is empty." },
            { DiagnosticCode.ProvenanceMissingLogicalPath, "Provenance logical path is missing." },
            { DiagnosticCode.ProvenanceInvalidVersion, "Provenance version is invalid." },

            // Namespaces
            { DiagnosticCode.NamespacesMissing, "Missing required 'namespaces' section in YAML." },
            { DiagnosticCode.NamespaceMissingName, "Namespace name is missing." },
            { DiagnosticCode.NamespaceInvalidSegment, "Namespace segment is invalid." },
            { DiagnosticCode.NamespaceEmptySegment, "Namespace contains an empty segment." },
            { DiagnosticCode.NamespaceReservedSegment, "Namespace segment is reserved." },
            { DiagnosticCode.NamespaceNameMustBeSimple, "Namespace name must not include dots." },
            { DiagnosticCode.NamespaceInvalidNode, "Namespace node must be a mapping." },
            { DiagnosticCode.DuplicateNamespace, "Duplicate namespace name." },

            // Identifiers
            { DiagnosticCode.InvalidIdentifier, "Identifier is not valid." },
            { DiagnosticCode.IdentifierNotPascalCase, "Identifier is not in PascalCase." },
            { DiagnosticCode.ReservedIdentifier, "Identifier is reserved." },
            { DiagnosticCode.DuplicateTypeIdentifier, "Duplicate type identifier." },
            { DiagnosticCode.DuplicateInstanceIdentifier, "Duplicate named instance identifier." },

            // Interfaces
            { DiagnosticCode.InterfacesMissing, "Missing required 'interface(s)' section in YAML." },
            { DiagnosticCode.InterfacesMissingSequenceNode, "The 'interface(s)' token must be a sequence node." },
            { DiagnosticCode.InterfaceMissingName, "Interface name is missing." },
            { DiagnosticCode.InterfaceNameInvalidFormat, "Interface name does not conform to expected format." },
            { DiagnosticCode.InterfaceMissingQualifiedName, "Interface name must be qualified." },
            { DiagnosticCode.InterfaceInvalidNode, "Interface node must be a mapping or scalar." },
            { DiagnosticCode.InterfaceNameMustBeSimple, "Interface name must not be qualified." },
            { DiagnosticCode.DuplicateInterface, "Duplicate interface name." },

            // Classes
            { DiagnosticCode.ClassesMissing, "Missing required 'class(es)' section in YAML." },
            { DiagnosticCode.ClassesNotSequenceNode, "The 'class(es)' token must be a sequence node." },
            { DiagnosticCode.ClassMissingName, "Class name is missing." },
            { DiagnosticCode.ClassBothClassAndInterfaceSet, "Class cannot specify both class and interface." },
            { DiagnosticCode.ClassNeitherClassNorInterfaceSet, "Class must specify class or interface." },
            { DiagnosticCode.ClassMissingParameters, "Class initializer parameters are missing." },
            { DiagnosticCode.ClassInterfaceMustBeQualified, "Interface name must be qualified." },
            { DiagnosticCode.ClassNameMustBeSimple, "Class name must not be qualified." },
            { DiagnosticCode.ClassInvalidNode, "Class node must be a mapping." },
            { DiagnosticCode.DuplicateClass, "Duplicate class name." },

            // Parameters
            { DiagnosticCode.ParametersMissing, "Missing required 'parameter(s)' section in YAML." },
            { DiagnosticCode.ParameterMissingName, "Parameter name is missing." },
            { DiagnosticCode.ParameterMissingQualifiedClass, "Parameter missing qualified class name." },
            { DiagnosticCode.ParameterMissingQualifiedInterface, "Parameter missing qualified interface name." },
            { DiagnosticCode.ParameterBothClassAndInterface, "Parameter cannot specify both class and interface." },
            { DiagnosticCode.ParameterMissingClassOrInterface, "Parameter must specify class or interface." },
            { DiagnosticCode.ParameterMustBeInterfaceForNonPrimitive, "Parameter must be an interface for non-primitive types." },
            { DiagnosticCode.ParameterInvalidNode, "Parameter node must be a mapping." },
            { DiagnosticCode.DuplicateParameter, "Duplicate parameter name." },

            // Named Instances
            { DiagnosticCode.InstanceClassNotFound, "Referenced class not found." },
            { DiagnosticCode.InstanceMissingName, "Named instance name is missing." },
            { DiagnosticCode.InstanceMissingQualifiedClass, "Named instance missing qualified class." },
            { DiagnosticCode.InstanceBothAssignmentsAndElementsSet, "Named instance cannot have both assignments and elements." },
            { DiagnosticCode.InstanceDuplicateName, "Named instance name is duplicated." },
            { DiagnosticCode.InstanceMissing, "Referenced named instance not found." },
            { DiagnosticCode.InstanceMissingOrNotExposingInterface, "Referenced named instance not found or does not expose required interface." },
            { DiagnosticCode.InstanceInvalidNode, "Named instance node must be a mapping." },

            // Assignments
            { DiagnosticCode.AssignmentNoValueOrInstance, "Assignment must specify a value or a named instance." },
            { DiagnosticCode.AssignmentMoreThanOneAssignmentTypeForParameter, "Assignment cannot specify both a value and a named instance." },
            { DiagnosticCode.AssignmentMissingParameterName, "Assignment missing parameter name." },
            { DiagnosticCode.AssignmentMissingValueOrInstance, "Assignment must specify a value or a named instance." },
            { DiagnosticCode.AssignmentInvalidLiteral, "Assignment literal is not valid." },
            { DiagnosticCode.AssignmentLiteralArrayNotSupported, "Assignment literal array is not supported." },
            { DiagnosticCode.AssignmentInvalidNode, "Assignment node must be a mapping." },
            { DiagnosticCode.AssignmentParameterNotFound, "Assignment parameter name not found in class definition." },
            { DiagnosticCode.AssignmentLiteralTypeMismatch, "Assignment literal type does not match parameter type." },
            { DiagnosticCode.AssignmentInstanceNotFound, "Assignment named instance not found." },

            // Elements
            { DiagnosticCode.ElementMissingValueOrInstance, "Element must specify a value or a named instance." },
            { DiagnosticCode.ElementInvalidNode, "Element node must be a mapping." },
            { DiagnosticCode.ElementMissingValue, "Element is missing an assigned value." },
            { DiagnosticCode.ElementMissingNamedInstance, "Element is missing an assigned named instance." },
            { DiagnosticCode.ElementBothValueAndInstance, "Element cannot specify both value and named instance." },

            // CodeGen
            { DiagnosticCode.CodeGenMissing, "Missing 'codeGen' section." },
            { DiagnosticCode.CodeGenMissingRegistryClass, "codeGen is missing 'registryClassName'." },
            { DiagnosticCode.CodeGenMissingGeneratedPath, "codeGen is missing 'generatedCodePath'." },
            { DiagnosticCode.CodeGenMissingNamespace, "codeGen is missing 'namespaceName'." },
            { DiagnosticCode.CodeGenMissingInitializer, "codeGen is missing 'initializer:'." },

            // Generator
            { DiagnosticCode.UnrecognizedToken, "Unrecognized token." },
            { DiagnosticCode.MissingTemplate, "Requested template was not found." },
            { DiagnosticCode.MissingPlaceholderValue, "Template placeholder value is missing." },
            { DiagnosticCode.DuplicateInvokerKey, "Invoker key is duplicated." },
            { DiagnosticCode.DependencyOrderViolation, "Generator dependency order is invalid." },
            { DiagnosticCode.FactoryBindingInstanceNotFound, "Factory binding references an instance that was not found." },
            { DiagnosticCode.FactoryBindingTypeNotFound, "Factory binding could not resolve the factory type." },
            { DiagnosticCode.FactoryBindingMethodNotFound, "Factory binding method was not found on the factory type." },
            { DiagnosticCode.FactoryBindingMethodAmbiguous, "Factory binding method is ambiguous (multiple overloads)." },
            { DiagnosticCode.FactoryBindingMethodHasParameters, "Factory binding method must be parameterless." },
            { DiagnosticCode.FactoryBindingReturnTypeMismatch, "Factory binding method return type does not match the instance type." },
            { DiagnosticCode.TypeRefInvalid, "Type reference is invalid." },
            { DiagnosticCode.DtoValidationException, "Exception occurred during DTO validation." },
            { DiagnosticCode.TemplateUnresolvedPlaceholder, "Template placeholder could not be resolved." },
            { DiagnosticCode.YamlEmptyDocument, "YAML document is empty." },
            { DiagnosticCode.YamlRootNodeNotMapping, "YAML root node is not a mapping node." },
            { DiagnosticCode.RootYamlDocumentMissing, "YAML document is missing a root node." },
            { DiagnosticCode.YamlParseError, "YAML parse error." },
        };

    public static DiagnosticSeverity GetSeverity(this DiagnosticCode code)
        => _severityMap.TryGetValue(code, out var s) ? s : DiagnosticSeverity.Error;

    public static string? GetDefaultMessage(this DiagnosticCode code)
        => _messageMap.TryGetValue(code, out var message) ? message : null;

    public static bool HasMapping(this DiagnosticCode code) => _severityMap.ContainsKey(code);
}
