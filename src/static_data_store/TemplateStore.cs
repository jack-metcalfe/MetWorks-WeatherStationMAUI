using System.Diagnostics;

namespace StaticDataStore;
public class TemplateStore : ITemplateStore
{
    static Dictionary<string, string> templateToResourcePath = new()
    {
        { "PrimitiveArray.Member", "DdiCodeGen.PrimitiveArray.Member.tplt" },
        { "PrimitiveArray.InnerMembers", "DdiCodeGen.PrimitiveArray.InnerMembers.tplt"   },
        { "PrimitiveArray", "DdiCodeGen.PrimitiveArray.tplt"   },
        { "NamedInstanceAccessor.Function", "DdiCodeGen.NamedInstanceAccessor.Function.tplt" },
        { "NamedInstanceAccessor.Function.Initializer","DdiCodeGen.NamedInstanceAccessor.Function.Initializer.tplt" },
        { "NamedInstanceAccessor.Function.NamedInstanceArray","DdiCodeGen.NamedInstanceAccessor.Function.NamedInstanceArray.tplt" },
        { "NamedInstanceAccessor.Class","DdiCodeGen.NamedInstanceAccessor.Class.tplt" },
        { "Registry.Member", "DdiCodeGen.Registry.Member" },
        { "Registry", "DdiCodeGen.Registry" },
        { "Registration.Fragment", "DdiCodeGen.Registration.Fragment" },
        { "Registration", "DdiCodeGen.Registration" },
        { "Initializer.Invoker", "DdiCodeGen.Initializer.Invoker" },
        { "Initializer", "DdiCodeGen.Initializer" },
    };
    static List<string> templateList = new();
    public bool TryGetTemplate(string templateName, out string templateText)
    {
        templateText = string.Empty;
        if (templateToResourcePath.TryGetValue(templateName, out var resourcePath))
        {
            if (!templateList.Contains(templateName))
            {
                templateList.Add(templateName);
            }

            // Load the template text from the resource path
            templateText = StaticData.GetResourceAsString(resourcePath) ?? string.Empty;
            return true;
        }
        return false;
    }

    public string getUsedTemplateNames()
    {
        return string.Join(Environment.NewLine, templateList);
    }
}
