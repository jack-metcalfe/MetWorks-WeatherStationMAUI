namespace MetWorks.DI.Declarative.Templates;
public static class TemplateDictionary
{
    public static readonly Dictionary<TemplateEnum, TemplateInfo> EnumToInfo = new()
    {
        {
            TemplateEnum.Accessors, new TemplateInfo (
                templateEnum: TemplateEnum.Accessors,
                name: "Accessors",
                fileName: "Accessors.g.cs",
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.Accessors.hbs",
                hasInstanceList: true
            )
        },
        {
            TemplateEnum.AccessorTriplet, new TemplateInfo (
                templateEnum: TemplateEnum.AccessorTriplet,
                name: "Accessors.Triplet",
                fileName: string.Empty,
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.Accessors.Triplet.hbs",
                isPartial: true,
                hasInstanceList: false
            )
        },
        {
            TemplateEnum.AssignmentsInitializer, new TemplateInfo (
                templateEnum: TemplateEnum.AssignmentsInitializer,
                name: "Assignments.Initializer",
                fileName: "Initializer.g.cs",
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.Assignments.Initializer.hbs",
                hasInstanceList: false
            )
        },
        {
            TemplateEnum.ElementsInitializer, new TemplateInfo (
                templateEnum: TemplateEnum.ElementsInitializer,
                name: "Elements.Initializer",
                fileName: "ElementsInitializer.g.cs",
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.Elements.Initializer.hbs",
                hasInstanceList: false
            )
        },
        {
            TemplateEnum.FileHeader, new TemplateInfo (
                templateEnum: TemplateEnum.FileHeader,
                name: "File.Header",
                fileName: string.Empty,
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.File.Header.hbs",
                isPartial: true,
                hasInstanceList: false
            )
        },
        {
            TemplateEnum.InstanceFactory, new TemplateInfo (
                templateEnum: TemplateEnum.InstanceFactory,
                name: "Instance.Factory",
                fileName: "InstanceFactory.g.cs",
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.Instance.Factory.hbs",
                hasInstanceList: false
            )
        },
        {
            TemplateEnum.InstanceField, new TemplateInfo (
                templateEnum: TemplateEnum.InstanceField,
                name: "Instance.Field",
                fileName: "InstanceField.g.cs",
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.Instance.Field.hbs",
                hasInstanceList: false
            )
        },
        {
            TemplateEnum.Registry, new TemplateInfo (
                templateEnum: TemplateEnum.Registry,
                name: "Registry",
                fileName: "Registry.g.cs",
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.Registry.hbs",
                hasInstanceList: true
            )
        },
        {
            TemplateEnum.ExposeToMauiDi, new TemplateInfo (
                templateEnum: TemplateEnum.ExposeToMauiDi,
                name: "ExposeToMauiDi",
                fileName: "ExposeToMauiDi.g.cs",
                resourceName: "MetWorks.DI.Declarative.Resources.Templates.ExposeToMauiDi.hbs",
                hasInstanceList: true
            )
        },
    };

    // Lookup by template name for callers that start from the name rather than the enum.
    public static readonly Dictionary<string, TemplateInfo> NameToInfo = EnumToInfo
        .Values
        .ToDictionary(info => info.Name, info => info);
}
