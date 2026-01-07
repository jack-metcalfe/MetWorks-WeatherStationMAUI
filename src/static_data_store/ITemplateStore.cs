using System.Collections.Generic;
namespace StaticDataStore;
public interface ITemplateStore
{
    bool TryGetTemplate(string templateName, out string templateText);
    string getUsedTemplateNames();
}