namespace MetWorks.DI.Declarative.Templates;
/// <summary>
/// Represents a store of templates that can be retrieved by name.
/// </summary>
public interface ITemplateStore
{
    /// <summary>
    /// Gets the template content for the specified template name
    /// </summary>
    /// <param name="name">The name of the template to retrieve.</param>
    /// <returns>The template content as a string.</returns>
    string GetTemplate(TemplateEnum templateEnum);
    /// <summary>
    /// Dictionary of TemplateEnum to template name
    /// </summary>
    public Dictionary<TemplateEnum, string> GetPartialTemplateEnumToNameDictionary();
}
