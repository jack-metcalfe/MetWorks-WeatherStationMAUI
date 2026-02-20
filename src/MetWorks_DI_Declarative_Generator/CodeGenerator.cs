namespace MetWorks.DI.Declarative.Generator;
public sealed class CodeGenerator
{
    private readonly ITemplateStore _templateStore;
    private readonly TemplateRenderer _templateRenderer;
    /// Context dictionary keys:
    internal const string c_generatedNamespace = @"GeneratedNamespace";
    internal const string c_registryClassName = "RegistryClassName";
    internal const string c_generatedHeader = "GeneratedHeader";
    internal const string c_instances = "Instances";
    internal const string c_namedInstanceName = "NamedInstanceName";


    /// <summary>
    /// Initializes a new instance of the <see cref="CodeGenerator"/> class.
    /// </summary>
    /// <param name="templateStore">Template store providing Handlebars templates.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="templateStore"/> is null.</exception>
    public CodeGenerator(ITemplateStore templateStore)
    {
        _templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
        _templateRenderer = new TemplateRenderer(_templateStore);
    }
    /// <summary>
    /// Generates all code files for the given canonical model.
    /// </summary>
    /// <param name="model">Canonical model DTO containing registry and instance definitions.</param>
    /// <returns>A dictionary of file names mapped to generated file content.</returns>
    public IReadOnlyDictionary<string, string> GenerateFiles(Model model)
    {
        var files = new Dictionary<string, string>();
        // Step 1: Create transformer and perform single-pass transformation
        var transformer = new ModelTransformer(model);
        var result = transformer.TransformAll();

        // Step 2: Access finalized list-based models
        ProcessTemplate(files, result.RegistryModel);
        ProcessTemplate(files, result.AccessorsModel);
        ProcessTemplate(files, result.ExposeToMauiDiModel);

        // Step 3: Access per-instance models by instance name
        foreach (var instanceName in result.AllInstanceNames)
        {
            var modelInstance = model.InstanceDictionary[instanceName];

            var factoryData = result.GetInstanceFactoryData(instanceName);
            var fieldData = result.GetInstanceFieldData(instanceName);

            var elementsData = modelInstance.HasElements
                ? result.GetElementsInitializerData(instanceName)
                : null;

            var assignmentsData = modelInstance.HasAssignments
                ? result.GetAssignmentsInitializerData(instanceName)
                : null;
            
            // Use each model with its corresponding template
            ProcessTemplate(files, factoryData);
            ProcessTemplate(files, fieldData);
            if (elementsData is not null) 
                ProcessTemplate(files, elementsData);
            if (assignmentsData is not null) 
                ProcessTemplate(files, assignmentsData);
        }
        return files;
    }

    /// <summary>
    /// Emits the aggregate registry file.
    /// </summary>
    private void ProcessTemplate(
        IDictionary<string, string> files,
        IModelBase templateModel
    )
    {
        TemplateInfo templateInfo = 
            TemplateDictionary.NameToInfo[templateModel.TemplateRequested];

        var result = _templateRenderer.Render(
            templateInfo.TemplateEnum,
            templateModel
        );

        string filename = templateInfo.FileName;
        if (templateModel is IModelSingleInstance singleInstanceModel)
            filename = $"{singleInstanceModel.InstanceName}_{filename}";

        files[filename] = result;
    }

    /// <summary>
    /// Emits per-instance fragments (member, elements initializer, assignments initializer).
    /// </summary>
    private void EmitInstanceFragments(
        IDictionary<string, string> files,
        string codePath,
        IDictionary<string, object?> expando
    )
    {
        var niName = (string)expando[CodeGenerator.c_namedInstanceName]!;

        // Registry.InstanceField (backing field declaration)
        var fieldTemplate = Handlebars.Compile(_templateStore.GetTemplate(TemplateEnum.InstanceField));
        EmitFile(files, codePath, $"{niName}_InstanceField.cs", fieldTemplate(expando));

        // Registry.InstanceFactory (creation logic)
        var factoryTemplate = Handlebars.Compile(_templateStore.GetTemplate(TemplateEnum.InstanceFactory));
        EmitFile(files, codePath, $"{niName}_InstanceFactory.cs", factoryTemplate(expando));
        // Elements initializer if Elements present
        if (expando.TryGetValue("Elements", out var elementsObj) &&
            elementsObj is List<Dictionary<string, object?>> elements &&
            elements.Count > 0)
        {
            var elementsInitializerTemplate = Handlebars.Compile(_templateStore.GetTemplate(TemplateEnum.ElementsInitializer));
            EmitFile(files, codePath, $"{niName}_ElementsInitializer.cs", elementsInitializerTemplate(expando));
        }

        // Assignments initializer if Assignments present
        if (expando.TryGetValue("Assignments", out var assignmentsObj) &&
            assignmentsObj is List<Dictionary<string, object?>> assignments &&
            assignments.Count > 0)
        {
            var assignmentsInitializerTemplate = Handlebars.Compile(_templateStore.GetTemplate(TemplateEnum.AssignmentsInitializer));
            EmitFile(files, codePath, $"{niName}_Initializer.cs", assignmentsInitializerTemplate(expando));
        }
    }


    /// <summary>
    /// Emits a generated file with provenance header.
    /// </summary>
    private static void EmitFile(
        IDictionary<string, string> files,
        string generatedCodePath,
        string fileName,
        string content
    )
    {
        files[fileName] = $"// Generated by DdiCodeGen at {DateTime.UtcNow:o}\n" +
            $"// Output Path: {generatedCodePath}\n" + content;
    }
}
