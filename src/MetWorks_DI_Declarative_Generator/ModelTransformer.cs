namespace MetWorks.DI.Declarative.Generator;
/// <summary>
/// Performs single-pass transformation of source Model to all template-specific record types.
/// Traverses the source model once and extracts data for all templates simultaneously.
/// </summary>
public sealed class ModelTransformer
{
    private readonly Model _sourceModel;
    private readonly CodeGen _codeGen;

    public ModelTransformer(Model sourceModel)
    {
        _sourceModel = sourceModel ?? throw new ArgumentNullException(nameof(sourceModel));
        _codeGen = sourceModel.CodeGen ?? throw new ArgumentNullException(nameof(sourceModel.CodeGen));
    }

    /// <summary>
    /// Performs single-pass transformation of the source model.
    /// Returns all template-specific models ready for rendering.
    /// </summary>
    public TransformationResult TransformAll()
    {
        var result = new TransformationResult(_codeGen);

        var orderedInstances = InstanceDependencySorter.Sort(_sourceModel.Instances);

        // Single pass through instances - extract data for all templates simultaneously
        foreach (var instance in orderedInstances)
        {
            TransformInstance(instance, result);
        }

        // Finalize all models with accumulated instance data
        result.FinalizeAll();

        return result;
    }

    /// <summary>
    /// Extracts data from a single instance for all applicable templates in one pass.
    /// </summary>
    private void TransformInstance(
        Instance instance,
        TransformationResult result
    )
    {
        result.RegisterInstance(instance.InstanceName!);

        // Accessors template (instance list aggregation)
        result.AddAccessorInstance(
            new Models.Accessors.Instance {
                Name = instance.InstanceName!,
                ClassQualified = instance.ClassQualified!,
                IsArray = instance.InstanceIsArray!,
                InterfaceQualified = instance.InterfaceQualified,
                HasAssignments = instance.HasAssignments
            }
        );
        // Registry template (instance list aggregation)
        result.AddRegistryInstance(
            new Models.Registry.Instance {
                Name = instance.InstanceName!,
                HasAssignments = instance.HasAssignments,
                HasDisposable = false  // TODO: Determine from source model
            }
        );
        if (instance.ExposeToMauiDi)
        {
            var serviceTypeQualified = (instance.InterfaceQualified ?? instance.ClassQualified!)
                + (instance.InstanceIsArray == true ? "[]" : string.Empty);

            // Important: `RegisterSingletonsInMaui(...)` needs to register instances into MAUI DI
            // during the DDI create phase (before `InitializeAllAsync(...)` has run). External
            // accessors include use-before-init guards for assignment-driven instances, so the
            // MAUI exposure path must use internal accessors.
            var resolveExpression = $"Get{instance.InstanceName!}_Internal()";

            var useNonGenericServiceRegistration = false;

            // D3: Prefer exposing CancellationToken values to MAUI DI, not CancellationTokenSource.
            if (string.Equals(instance.ClassQualified, "System.Threading.CancellationTokenSource", StringComparison.Ordinal)
                && instance.InstanceIsArray != true)
            {
                serviceTypeQualified = "System.Threading.CancellationToken";
                resolveExpression += ".Token";
                useNonGenericServiceRegistration = true;
            }

            result.AddMauiDiInstance(
                new Models.ExposeToMauiDi.Instance {
                    Name = instance.InstanceName!,
                    ClassQualified = instance.ClassQualified!,
                    IsArray = instance.InstanceIsArray!,
                    InterfaceQualified = instance.InterfaceQualified,
                    ServiceTypeQualified = serviceTypeQualified,
                    ResolveExpression = resolveExpression,
                    UseNonGenericServiceRegistration = useNonGenericServiceRegistration
                }
            );
        }

        // Instance.Factory template (per-instance)
        result.SetInstanceFactoryData(
            instance.InstanceName!,
            new Models.Instance.Factory.Instance {
                Name = instance.InstanceName!,
                ClassQualified = instance.ClassQualified!,
                IsArray = instance.InstanceIsArray!,
                HasElements = instance.HasElements,
                FactoryInstanceName = instance.FactoryInstanceName,
                FactoryMethodName = instance.FactoryMethodName
            }
        );

        // Instance.Field template (per-instance)
        result.SetInstanceFieldData(
            instance.InstanceName!,
            new Models.Instance.Field.Instance {
                Name = instance.InstanceName!,
                ClassQualified = instance.ClassQualified!,
                IsArray = instance.InstanceIsArray!
            }
        );

        // Elements.Initializer template (per-instance)
        if (instance.HasElements)
        {
            result.SetElementsInitializerData(
                instance.InstanceName!,
                new Models.Elements.Initializer.Instance {
                    Name = instance.InstanceName!,
                    ClassQualified = instance.ClassQualified!,
                    IsArray = instance.InstanceIsArray!,
                    ElementsConstructionExpression = instance.ElementsConstructionExpression
                }
            );
        }

        // Assignments.Initializer template (per-instance)
        if (instance.HasAssignments)
        {
            result.SetAssignmentsInitializerData(
                instance.InstanceName!,
                new Models.Assignments.Initializer.Instance {
                    Name = instance.InstanceName!,
                    HasAssignments = instance.HasAssignments,
                    Assignments = ExtractAssignments(instance),
                    InitializationDependencies = ExtractInitializationDependencies(instance)
                }
            );
        }
    }

    /// <summary>
    /// Extracts assignment data from an instance.
    /// </summary>
    private List<Models.Assignments.Initializer.Assignment> ExtractAssignments(Instance instance)
    {
        return instance.Assignments
            .Select(a => new Models.Assignments.Initializer.Assignment
            {
                ParameterName = a.Name
                    ?? throw new InvalidOperationException($"Assignment missing name on instance {instance.InstanceName}"),
                InitializerArgumentExpression = a.InitializerParameterAssignmentClause
                    ?? string.Empty
            })
            .ToList();
    }

    private List<string> ExtractInitializationDependencies(Instance instance)
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

            // Only await instances that themselves require async initialization.
            if (_sourceModel.InstanceDictionary.TryGetValue(depName, out var depInstance) && depInstance.HasAssignments)
            {
                if (seen.Add(depName))
                    result.Add(depName);
            }
        }

        return result;
    }
}

/// <summary>
/// Contains transformed template-specific models built from a single pass through the source model.
/// Separates accumulation phase from finalization phase.
/// </summary>
public sealed class TransformationResult
{
    private readonly CodeGen _codeGen;
    private readonly List<string> _instanceOrder = new();
    private readonly HashSet<string> _instanceNameSet = new();
    private bool _finalized;

    // Accumulated instance data for list-based templates
    private readonly List<Models.Accessors.Instance> _accessorInstances = new();
    private readonly List<Models.Registry.Instance> _registryInstances = new();
    private readonly List<Models.ExposeToMauiDi.Instance> _exposeToMauiDiInstances = new();

    // Per-instance template data (keyed by instance name)
    private readonly Dictionary<string, Models.Instance.Factory.Instance> _instanceFactoryData = new();
    private readonly Dictionary<string, Models.Instance.Field.Instance> _instanceFieldData = new();
    private readonly Dictionary<string, Models.Elements.Initializer.Instance> _elementsInitializerData = new();
    private readonly Dictionary<string, Models.Assignments.Initializer.Instance> _assignmentsInitializerData = new();

    // Finalized models
    private Models.Accessors.Model? _accessorsModel;
    private Models.Registry.Model? _registryModel;
    private Models.ExposeToMauiDi.Model? _exposeToMauiDiModel;
    private Dictionary<string, Models.Instance.Factory.Model>? _instanceFactoryModels;
    private Dictionary<string, Models.Instance.Field.Model>? _instanceFieldModels;
    private Dictionary<string, Models.Elements.Initializer.Model>? _elementsInitializerModels;
    private Dictionary<string, Models.Assignments.Initializer.Model>? _assignmentsInitializerModels;

    public TransformationResult(CodeGen codeGen)
    {
        _codeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
    }

    #region Accumulation Phase (populated during single pass)

    public void AddAccessorInstance(Models.Accessors.Instance instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        _accessorInstances.Add(instance);
    }
    public void AddRegistryInstance(Models.Registry.Instance instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        _registryInstances.Add(instance);
    }
    internal void AddMauiDiInstance(Models.ExposeToMauiDi.Instance instance)
    {
        _exposeToMauiDiInstances.Add(instance);
    }
    public void RegisterInstance(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new ArgumentException("Instance name required", nameof(instanceName));

        if (!_instanceNameSet.Add(instanceName))
            throw new InvalidOperationException($"Duplicate instance name '{instanceName}' detected during transformation.");

        _instanceOrder.Add(instanceName);
    }

    public void SetInstanceFactoryData(string instanceName, Models.Instance.Factory.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _instanceFactoryData[instanceName] = data;
    }

    public void SetInstanceFieldData(string instanceName, Models.Instance.Field.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _instanceFieldData[instanceName] = data;
    }

    public void SetElementsInitializerData(string instanceName, Models.Elements.Initializer.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _elementsInitializerData[instanceName] = data;
    }

    public void SetAssignmentsInitializerData(string instanceName, Models.Assignments.Initializer.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _assignmentsInitializerData[instanceName] = data;
    }

    #endregion

    #region Finalization Phase (called after accumulation)

    /// <summary>
    /// Finalizes all accumulated data into complete ModelBase records.
    /// Must be called once after all accumulation is complete.
    /// </summary>
    public void FinalizeAll()
    {
        _accessorsModel = new Models.Accessors.Model
        {
            TemplateRequested = TemplateDictionary.EnumToInfo[TemplateEnum.Accessors].Name,
            Namespace = _codeGen.Namespace!,
            ContainerClass = _codeGen.RegistryClass!,
            Instances = _accessorInstances
        };

        _registryModel = new Models.Registry.Model
        {
            TemplateRequested = TemplateDictionary.EnumToInfo[TemplateEnum.Registry].Name,
            Namespace = _codeGen.Namespace!,
            ContainerClass = _codeGen.RegistryClass!,
            Instances = _registryInstances
        };

        _exposeToMauiDiModel = new Models.ExposeToMauiDi.Model
        {
            TemplateRequested = TemplateDictionary.EnumToInfo[TemplateEnum.ExposeToMauiDi].Name,
            Namespace = _codeGen.Namespace!,
            ContainerClass = _codeGen.RegistryClass!,
            Instances = _exposeToMauiDiInstances
        };

        _instanceFactoryModels = _instanceFactoryData.ToDictionary(
            kvp => kvp.Key,
            kvp => new Models.Instance.Factory.Model
            {
                TemplateRequested = TemplateDictionary.EnumToInfo[TemplateEnum.InstanceFactory].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                Instance = kvp.Value
            });

        _instanceFieldModels = _instanceFieldData.ToDictionary(
            kvp => kvp.Key,
            kvp => new Models.Instance.Field.Model
            {
                TemplateRequested = TemplateDictionary.EnumToInfo[TemplateEnum.InstanceField].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                Instance = kvp.Value
            });

        _elementsInitializerModels = _elementsInitializerData.ToDictionary(
            kvp => kvp.Key,
            kvp => new Models.Elements.Initializer.Model
            {
                TemplateRequested = TemplateDictionary.EnumToInfo[TemplateEnum.ElementsInitializer].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                Instance = kvp.Value
            });

        _assignmentsInitializerModels = _assignmentsInitializerData.ToDictionary(
            kvp => kvp.Key,
            kvp => new Models.Assignments.Initializer.Model
            {
                TemplateRequested = TemplateDictionary.EnumToInfo[TemplateEnum.AssignmentsInitializer].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                InitializerName = _codeGen.Initializer
                    ?? throw new InvalidOperationException("Initializer is required for assignments initializer template."),
                Instance = kvp.Value
            });

            _finalized = true;
    }

    #endregion

    #region Result Access (after FinalizeAll)

    /// <summary>
    /// Gets the finalized Accessors model. Must call FinalizeAll() first.
    /// </summary>
    public Models.Accessors.Model AccessorsModel => _accessorsModel ?? throw new InvalidOperationException("FinalizeAll() must be called first");
    /// <summary>
    /// Gets the finalized Registry model. Must call FinalizeAll() first.
    /// </summary>
    public Models.Registry.Model RegistryModel => _registryModel ?? throw new InvalidOperationException("FinalizeAll() must be called first");
    public Models.ExposeToMauiDi.Model ExposeToMauiDiModel => _exposeToMauiDiModel ?? throw new InvalidOperationException("FinalizeAll() must be called first");
    /// <summary>
    /// Gets per-instance factory data by instance name.
    /// </summary>
    public Models.Instance.Factory.Model GetInstanceFactoryData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_instanceFactoryModels is null || !_instanceFactoryModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No factory data for instance '{instanceName}'");
        return model;
    }

    /// <summary>
    /// Gets per-instance field data by instance name.
    /// </summary>
    public Models.Instance.Field.Model GetInstanceFieldData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_instanceFieldModels is null || !_instanceFieldModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No field data for instance '{instanceName}'");
        return model;
    }

    /// <summary>
    /// Gets per-instance elements initializer data by instance name.
    /// </summary>
    public Models.Elements.Initializer.Model GetElementsInitializerData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_elementsInitializerModels is null || !_elementsInitializerModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No elements initializer data for instance '{instanceName}'");
        return model;
    }

    /// <summary>
    /// Gets per-instance assignments initializer data by instance name.
    /// </summary>
    public Models.Assignments.Initializer.Model GetAssignmentsInitializerData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_assignmentsInitializerModels is null || !_assignmentsInitializerModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No assignments initializer data for instance '{instanceName}'");

        return model;
    }
    /// <summary>
    /// Gets all accumulated instance names for iteration.
    /// </summary>
    public IEnumerable<string> AllInstanceNames => _finalized
        ? _instanceOrder
        : throw new InvalidOperationException("FinalizeAll() must be called first");

    #endregion
}
