namespace MetWorks.DI.Declarative.EnumDefinitions;
public enum TokenTypes
{
    // Top-level
    root,

    // CodeGen
    codeGen,
    codeGenRegistryClass,
    codeGenCodePath,
    codeGenNamespace,
    codeGenInitializer,

    namespaces,
    namespacesNamespace,
    namespacesNamespaceName,
    namespacesNamespaceInterface,
    namespacesNamespaceInterfaces,
    namespacesNamespaceInterfaceName,
    namespacesNamespaceClass,
    namespacesNamespaceClasses,
    namespacesNamespaceClassName,
    namespacesNamespaceClassInterface,
    namespacesNamespaceClassParameter,
    namespacesNamespaceClassParameters,
    namespacesNamespaceClassParameterName,
    namespacesNamespaceClassParameterClass,
    namespacesNamespaceClassParameterInterface,
    namespacesNamespaceClassProperties,
    namespacesNamespaceClassProperty,
    namespacesNamespaceClassPropertyName,
    namespacesNamespaceClassPropertyClass,
    namespacesNamespaceClassPropertyInterface,

    // NamedInstance
    instances,
    instancesInstance,
    instancesInstanceName,
    instancesInstanceClass,
    instancesInstanceExposeToMauiDi,
    instancesInstanceFactoryInstance,
    instancesInstanceFactoryMethod,
    instancesInstanceAssignment,
    instancesInstanceAssignments,
    instancesInstanceAssignmentName,
    instancesInstanceAssignmentLiteral,
    instancesInstanceAssignmentInstance,
    instancesInstanceAssignmentInstanceProperty,
    instancesInstanceElement,
    instancesInstanceElementLiteral,
    instancesInstanceElementInstance,
}
