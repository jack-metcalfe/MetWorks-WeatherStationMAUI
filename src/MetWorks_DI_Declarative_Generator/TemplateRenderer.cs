namespace MetWorks.DI.Declarative.Generator;
public sealed class TemplateRenderer
{
    private readonly IHandlebars _iHandlebars;
    private readonly ITemplateStore _iTemplateStore;
    private readonly ConcurrentDictionary<TemplateEnum, Func<object, string>> _compiledTemplates = new();

    public TemplateRenderer(ITemplateStore store)
    {
        _iTemplateStore = store;
        _iHandlebars = CreateHandlebarsWithPartials(store);
    }

    private static IHandlebars CreateHandlebarsWithPartials(ITemplateStore store)
    {
        var handlebars = Handlebars.Create();

        // Load and register all partials upfront
        var partialTemplateEnumToNames = store.GetPartialTemplateEnumToNameDictionary();

        foreach (var partialTemplateEnum in partialTemplateEnumToNames)
        {
            var content = store.GetTemplate(partialTemplateEnum.Key);
            handlebars.RegisterTemplate(partialTemplateEnum.Value, content);
        }

        return handlebars;
    }

    public string Render(TemplateEnum templateEnum, object ctx)
    {
        string result;
        try
        {
            var templateProcessor = _compiledTemplates.GetOrAdd(
                templateEnum,
                key =>
                {
                    var templateContent = _iTemplateStore.GetTemplate(templateEnum);
                    var compiled = _iHandlebars.Compile(templateContent);
                    return ctx2 => compiled(ctx2);
                });

            result = templateProcessor(ctx);
        }
        catch (HandlebarsException handlebarsException)
        {
            throw new InvalidOperationException(
                $"Handlebars error rendering template '{templateEnum}': {handlebarsException.Message}",
                handlebarsException
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error rendering template '{templateEnum}': {ex.Message}",
                ex
            );
        }
        return result;
    }
}
