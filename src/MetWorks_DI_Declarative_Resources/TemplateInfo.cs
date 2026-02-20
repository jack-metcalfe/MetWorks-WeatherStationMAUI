namespace MetWorks.DI.Declarative.Templates;
public readonly record struct TemplateInfo
{
    public readonly TemplateEnum TemplateEnum;
    public readonly string Name;
    public readonly string FileName;
    public readonly string ResourceName;
    public readonly bool IsPartial;
    public readonly bool HasInstanceList;
    public TemplateInfo(
        TemplateEnum templateEnum,
        string name,
        string fileName,
        string resourceName,
        bool isPartial = false,
        bool hasInstanceList = false
    )
    {
        TemplateEnum = templateEnum;
        Name = name;
        FileName = fileName;
        ResourceName = resourceName;
        IsPartial = isPartial;
        HasInstanceList = hasInstanceList;
    }
}
