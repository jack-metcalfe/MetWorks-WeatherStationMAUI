namespace MetWorks.DI.Declarative.Syntax.Models;
// Centralized schema: allowed keys per DTO type
public static class Schema
{
    public static readonly IReadOnlyDictionary<TokenTypes, string> TokenTypeToName = 
        new Dictionary<TokenTypes, string>
    {
        { TokenTypes.root, @"/" },

        { TokenTypes.codeGen, @"codeGen" },
        { TokenTypes.codeGenRegistryClass, @"registryClass" },
        { TokenTypes.codeGenCodePath, @"codePath" },
        { TokenTypes.codeGenNamespace, @"namespace" },
        { TokenTypes.codeGenInitializer, @"initializer" },
        { TokenTypes.namespaces, @"namespace" },
        { TokenTypes.namespacesNamespace, @"namespace" },
        { TokenTypes.namespacesNamespaceName, @"name" },
        { TokenTypes.namespacesNamespaceInterface, @"interface" },
        { TokenTypes.namespacesNamespaceInterfaces, @"interface" },
        { TokenTypes.namespacesNamespaceInterfaceName, @"name" },
        { TokenTypes.namespacesNamespaceClass, @"class" },
        { TokenTypes.namespacesNamespaceClasses, @"class" },
        { TokenTypes.namespacesNamespaceClassName, @"name" },
        { TokenTypes.namespacesNamespaceClassInterface, @"interface" },
        { TokenTypes.namespacesNamespaceClassParameters, @"parameter" },
        { TokenTypes.namespacesNamespaceClassParameter, @"parameter" },
        { TokenTypes.namespacesNamespaceClassParameterName, @"name" },
        { TokenTypes.namespacesNamespaceClassParameterClass, @"class" },
        { TokenTypes.namespacesNamespaceClassParameterInterface, "interface" },
        { TokenTypes.namespacesNamespaceClassProperties, @"property" },
        { TokenTypes.namespacesNamespaceClassProperty, @"property" },
        { TokenTypes.namespacesNamespaceClassPropertyName, @"name" },
        { TokenTypes.namespacesNamespaceClassPropertyClass, @"class" },
        { TokenTypes.namespacesNamespaceClassPropertyInterface, "interface" },

        { TokenTypes.instances, @"instance" },
        { TokenTypes.instancesInstance, @"instance" },
        { TokenTypes.instancesInstanceName, @"name" },
        { TokenTypes.instancesInstanceClass, @"class" },
        { TokenTypes.instancesInstanceExposeToMauiDi, @"exposeToMauiDi" },
        { TokenTypes.instancesInstanceFactoryInstance, @"factoryInstance" },
        { TokenTypes.instancesInstanceFactoryMethod, @"factoryMethod" },
        { TokenTypes.instancesInstanceAssignment, @"assignment" },
        { TokenTypes.instancesInstanceAssignments, @"assignment" },
        { TokenTypes.instancesInstanceAssignmentName, @"name" },
        { TokenTypes.instancesInstanceAssignmentLiteral, @"literal" },
        { TokenTypes.instancesInstanceAssignmentInstance, @"instance" },
        { TokenTypes.instancesInstanceAssignmentInstanceProperty, @"instanceProperty" },
        { TokenTypes.instancesInstanceElement, @"element" },
        { TokenTypes.instancesInstanceElementLiteral, @"literal" },
        { TokenTypes.instancesInstanceElementInstance, "instance" },
    };
    public static readonly IReadOnlyDictionary<Type, string> TypeToTokenName =
        new Dictionary<Type, string>
    {
        { typeof(Model), TokenTypeToName[TokenTypes.root] },
        { typeof(CodeGen),  TokenTypeToName[TokenTypes.codeGen] },
        { typeof(Namespace), TokenTypeToName[TokenTypes.namespacesNamespace] },
        { typeof(Class), TokenTypeToName[TokenTypes.namespacesNamespaceClass] },
        { typeof(Parameter), TokenTypeToName[TokenTypes.namespacesNamespaceClassParameter] },
        { typeof(Property), TokenTypeToName[TokenTypes.namespacesNamespaceClassProperty] },
        { typeof(Instance), TokenTypeToName[TokenTypes.instancesInstance] },
        { typeof(Interface), TokenTypeToName[TokenTypes.namespacesNamespaceInterface] },
        { typeof(Assignment), TokenTypeToName[TokenTypes.instancesInstanceAssignment] },
        { typeof(Element), TokenTypeToName[TokenTypes.instancesInstanceElement] },
    };
    public static readonly IReadOnlyDictionary<Type, string> TypeToTokenPath =
        new Dictionary<Type, string>
    {
        { typeof(Model), @"/" },
        { typeof(CodeGen),  @"/codeGen" },
        { typeof(Namespace), @"/namespaces/namespace" },
        { typeof(Class), @"/namespaces/namespace/class" },
        { typeof(Parameter), @"/namespaces/namespace/class/parameter" },       
        { typeof(Property), @"/namespaces/namespace/class/property" },
        { typeof(Instance), @"/instances/instance" },
        { typeof(Assignment), @"/instances/instance/assignment" },
        { typeof(Element), @"/instances/instance/elements" },
    };
    public static readonly IReadOnlyDictionary<string, string> LogicalPathToToken = 
        new Dictionary<string, string>
    {
        { @"/", TokenTypeToName[TokenTypes.root]  },

        { @"/codeGen", TokenTypeToName[TokenTypes.codeGen] },

        { @"/codeGen/registryClass", TokenTypeToName[TokenTypes.codeGenRegistryClass] },
        { @"/codeGen/codePath", TokenTypeToName[TokenTypes.codeGenCodePath] },
        { @"/codeGen/namespace", TokenTypeToName[TokenTypes.codeGenNamespace] },
        { @"/codeGen/initializer", TokenTypeToName[TokenTypes.codeGenInitializer] },

        { @"/namespaces", TokenTypeToName[TokenTypes.namespaces] },

        { @"/namespaces/namespace", TokenTypeToName[TokenTypes.namespacesNamespace] },
        { @"/namespaces/namespace/name", TokenTypeToName[TokenTypes.namespacesNamespaceName] },

        { @"/namespaces/namespace/interface", TokenTypeToName[TokenTypes.namespacesNamespaceInterface] },
        { @"/namespaces/namespace/interface/name", TokenTypeToName[TokenTypes.namespacesNamespaceInterfaceName] },

        { @"/namespaces/namespace/class", TokenTypeToName[TokenTypes.namespacesNamespaceClass] },

        { @"/namespaces/namespace/class/name", TokenTypeToName[TokenTypes.namespacesNamespaceClassName] },

        { @"/namespaces/namespace/class/interface", TokenTypeToName[TokenTypes.namespacesNamespaceClassInterface] },

        { @"/namespaces/namespace/class/parameter", TokenTypeToName[TokenTypes.namespacesNamespaceClassParameter] },

        { @"/namespaces/namespace/class/parameter/name", TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterName] },
        { @"/namespaces/namespace/class/parameter/class", TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterClass] },
        { @"/namespaces/namespace/class/parameter/interface", TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterInterface] },

        { @"/namespaces/namespace/class/property", TokenTypeToName[TokenTypes.namespacesNamespaceClassProperty] },

        { @"/namespaces/namespace/class/property/name", TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyName] },
        { @"/namespaces/namespace/class/property/class", TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyClass] },
        { @"/namespaces/namespace/class/property/interface", TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyInterface] },

        { @"/instances", TokenTypeToName[TokenTypes.instances] },

        { @"/instances/instance", TokenTypeToName[TokenTypes.instancesInstance] },

        { @"/instances/instance/name", TokenTypeToName[TokenTypes.instancesInstanceName] },
        { @"/instances/instance/class", TokenTypeToName[TokenTypes.instancesInstanceClass] },
        { @"/instances/instance/exposeToMauiDi", TokenTypeToName[TokenTypes.instancesInstanceExposeToMauiDi] },
        { @"/instances/instance/factoryInstance", TokenTypeToName[TokenTypes.instancesInstanceFactoryInstance] },
        { @"/instances/instance/factoryMethod", TokenTypeToName[TokenTypes.instancesInstanceFactoryMethod] },
        { @"/instances/instance/assignment", TokenTypeToName[TokenTypes.instancesInstanceAssignment] },

        { @"/instances/instance/assignment/name", TokenTypeToName[TokenTypes.instancesInstanceAssignmentName] },
        { @"/instances/instance/assignment/literal", TokenTypeToName[TokenTypes.instancesInstanceAssignmentLiteral] },
        { @"/instances/instance/assignment/instance", TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstance] },
        { @"/instances/instance/assignment/instanceProperty", TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstanceProperty] },

        { @"/instances/instance/elements", TokenTypeToName[TokenTypes.instancesInstanceElement] },

        { @"/instances/instance/elements/literal", TokenTypeToName[TokenTypes.instancesInstanceElementLiteral] },
        { @"/instances/instance/elements/instance", TokenTypeToName[TokenTypes.instancesInstanceElementInstance] },
    };
    public static readonly IReadOnlyDictionary<TokenTypes, string> TokenTypeToTokenPath =
        new Dictionary<TokenTypes, string>
    {
        { TokenTypes.root, @"/" },

        { TokenTypes.codeGen, @"/codeGen" },

        { TokenTypes.codeGenRegistryClass, @"/codeGen/registryClass" },
        { TokenTypes.codeGenCodePath, @"/codeGen/codePath" },
        { TokenTypes.codeGenNamespace, @"/codeGen/namespace" },
        { TokenTypes.codeGenInitializer, @"/codeGen/initializer" },

        { TokenTypes.namespacesNamespace, @"/namespaces/namespace" },

        { TokenTypes.namespacesNamespaceName, @"/namespaces/namespace/name" },
        { TokenTypes.namespacesNamespaceInterface, @"/namespaces/namespace/interface" },

        { TokenTypes.namespacesNamespaceInterfaceName, @"/namespaces/namespace/interface/name" },

        { TokenTypes.namespacesNamespaceClass, @"/namespaces/namespace/class" },

        { TokenTypes.namespacesNamespaceClassName, @"/namespaces/namespace/class/name" },
        { TokenTypes.namespacesNamespaceClassInterface, @"/namespaces/namespace/class/interface" },

        { TokenTypes.namespacesNamespaceClassParameter, @"/namespaces/namespace/class/parameter" },

        { TokenTypes.namespacesNamespaceClassParameterName, @"/namespaces/namespace/class/parameter/name" },
        { TokenTypes.namespacesNamespaceClassParameterClass, @"/namespaces/namespace/class/parameter/class" },
        { TokenTypes.namespacesNamespaceClassParameterInterface, @"/namespaces/namespace/class/parameter/interface" },

        { TokenTypes.namespacesNamespaceClassProperty, @"/namespaces/namespace/class/property" },

        { TokenTypes.namespacesNamespaceClassPropertyName, @"/namespaces/namespace/class/property/name" },
        { TokenTypes.namespacesNamespaceClassPropertyClass, @"/namespaces/namespace/class/property/class" },
        { TokenTypes.namespacesNamespaceClassPropertyInterface, @"/namespaces/namespace/class/property/interface" },

        { TokenTypes.instancesInstance, @"/instances/instance" },

        { TokenTypes.instancesInstanceName, @"/instances/instance/name" },
        { TokenTypes.instancesInstanceClass, @"/instances/instance/class" },
        { TokenTypes.instancesInstanceExposeToMauiDi, @"/instances/instance/exposeToMauiDi" },
        { TokenTypes.instancesInstanceFactoryInstance, @"/instances/instance/factoryInstance" },
        { TokenTypes.instancesInstanceFactoryMethod, @"/instances/instance/factoryMethod" },
        { TokenTypes.instancesInstanceAssignment, @"/instances/instance/assignment" },

        { TokenTypes.instancesInstanceAssignmentName, @"/instances/instance/assignment/name" },
        { TokenTypes.instancesInstanceAssignmentLiteral, @"/instances/instance/assignment/literal" },
        { TokenTypes.instancesInstanceAssignmentInstance, @"/instances/instance/assignment/instance" },
        { TokenTypes.instancesInstanceAssignmentInstanceProperty, @"/instances/instance/assignment/instanceProperty" },

        { TokenTypes.instancesInstanceElement, @"/instances/instance/elements" },

        { TokenTypes.instancesInstanceElementLiteral, @"/instances/instance/elements/literal" },
        { TokenTypes.instancesInstanceElementInstance, @"/instances/instance/elements/instance" },
    };
    public static readonly IReadOnlyDictionary<Type, string> TypeToPath =
        new Dictionary<Type, string>
    {
        { typeof(Model), @"/" },

        { typeof(CodeGen),  @"/codeGen" },

        { typeof(Namespace), @"/namespaces/namespace" },

        { typeof(Class), @"/namespaces/namespace/class" },

        { typeof(Parameter), @"/namespaces/namespace/class/parameter" },
        { typeof(Property), @"/namespaces/namespace/class/property" },

        { typeof(Instance), @"/instances/instance" },

        { typeof(Assignment), @"/instances/instance/assignment" },
        { typeof(Element), @"/instances/instance/elements" },
    };
    public static readonly IReadOnlyDictionary<Type, string[]> AllowedKeys =
        new Dictionary<Type, string[]>
        {
            { typeof(Model), new[] {
                    TokenTypeToName[TokenTypes.codeGen],
                    TokenTypeToName[TokenTypes.namespaces],
                    TokenTypeToName[TokenTypes.instances]
                }
            },

            { typeof(CodeGen), new[] {
                    TokenTypeToName[TokenTypes.codeGenRegistryClass],
                    TokenTypeToName[TokenTypes.codeGenCodePath],
                    TokenTypeToName[TokenTypes.codeGenNamespace],
                    TokenTypeToName[TokenTypes.codeGenInitializer],
                    
                }
            },
            { typeof(Namespace), new[] {
                    TokenTypeToName[TokenTypes.namespacesNamespaceName],
                    TokenTypeToName[TokenTypes.namespacesNamespaceInterface],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClass]
                }
            },
            { typeof(Class), new[] {
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassName],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassInterface],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassParameter],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassProperties]
                }
            },
            { typeof(Parameter), new[] {
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterName],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterClass],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassParameterInterface]
                 }
            },
            { typeof(Property), new[] {
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyName],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyClass],
                    TokenTypeToName[TokenTypes.namespacesNamespaceClassPropertyInterface]
                 }
            },
            { typeof(Instance), new[] {
                    TokenTypeToName[TokenTypes.instancesInstanceName],
                    TokenTypeToName[TokenTypes.instancesInstanceClass],
                    TokenTypeToName[TokenTypes.instancesInstanceExposeToMauiDi],
                    TokenTypeToName[TokenTypes.instancesInstanceFactoryInstance],
                    TokenTypeToName[TokenTypes.instancesInstanceFactoryMethod],
                    TokenTypeToName[TokenTypes.instancesInstanceAssignment],
                    TokenTypeToName[TokenTypes.instancesInstanceElement]
                }
            },
            { typeof(Interface), new[] {
                    TokenTypeToName[TokenTypes.namespacesNamespaceInterfaceName]
                }
            },
            { typeof(Assignment), new[] {
                    TokenTypeToName[TokenTypes.instancesInstanceAssignmentName],
                    TokenTypeToName[TokenTypes.instancesInstanceAssignmentLiteral],
                    TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstance],
                    TokenTypeToName[TokenTypes.instancesInstanceAssignmentInstanceProperty]
                }
            },
            { typeof(Element), new[] {
                    TokenTypeToName[TokenTypes.instancesInstanceElementLiteral],
                    TokenTypeToName[TokenTypes.instancesInstanceElementInstance]
                }
            }
        };
}
