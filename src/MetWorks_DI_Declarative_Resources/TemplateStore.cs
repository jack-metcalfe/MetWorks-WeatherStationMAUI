namespace MetWorks.DI.Declarative.Templates;
/// <summary>
/// Provides access to embedded templates stored in this assembly.
/// </summary>
public sealed class TemplateStore : ITemplateStore
{
    readonly Assembly _assembly;
    readonly string[] _resourceNames;
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateStore"/> class
    /// which provides access to embedded templates in this assembly.
    /// </summary>
    public TemplateStore()
    {
        _assembly = typeof(TemplateStore).Assembly;
        _resourceNames = _assembly.GetManifestResourceNames();
    }

    /// <summary>
    /// Gets the content of an embedded template resource by name.
    /// </summary>
    public string GetTemplateByTemplateEnum(TemplateEnum templateEnum)
    {
        return GetTemplateByResourceName(TemplateDictionary.EnumToInfo[templateEnum].ResourceName);
    }
    public string GetTemplateByTemplateName(string templateName)
    {
        var templateInfo = TemplateDictionary.EnumToInfo.Values
            .FirstOrDefault(t => t.Name == templateName);            
        if (templateInfo.Equals(default))
        {
            throw new ArgumentException(
                $"Template '{templateName}' not found in TemplateStore.",
                nameof(templateName)
            );
        }
        return GetTemplateByResourceName(templateInfo.ResourceName);
    }
    public string GetTemplate(string templateName) => GetTemplateByTemplateName(templateName);
    public string GetTemplate(TemplateEnum templateEnum) => GetTemplateByTemplateEnum(templateEnum);
    public string GetTemplateByResourceName(string resourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new ArgumentException(
                $"Resource '{resourceName}' not found in assembly '{_assembly.FullName}'.",
                nameof(resourceName)
            );
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    /// <summary>
    /// Lists partial template names
    /// </summary>
    public Dictionary<TemplateEnum, string> GetPartialTemplateEnumToNameDictionary()
    {
        return TemplateDictionary.EnumToInfo.Values
            .Where(v => v.IsPartial)
            .ToDictionary(v => v.TemplateEnum, v => v.Name);
    }
}
