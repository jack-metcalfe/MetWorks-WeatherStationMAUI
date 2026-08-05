using MetWorks.DI.Declarative.Generator;
using MetWorks.DI.Declarative.Syntax;
using MetWorks.DI.Declarative.Templates;
using Xunit;

public sealed class InstanceOrderingTests
{
    [Fact]
    public void WhenInstanceDependsOnLaterInstanceThenRegistryCreateAllIsDependencyFirst()
    {
        var yaml = YamlTestHelper.LoadFixture("InstanceOrdering.yaml");

        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var files = new CodeGenerator(new TemplateStore()).GenerateFiles(model);

        Assert.True(files.TryGetValue("Registry.g.cs", out var registryText));

        var aIndex = registryText.IndexOf("TheA_InstanceFactory.Create(this);", StringComparison.Ordinal);
        var bIndex = registryText.IndexOf("TheB_InstanceFactory.Create(this);", StringComparison.Ordinal);

        Assert.True(aIndex >= 0);
        Assert.True(bIndex >= 0);
        Assert.True(bIndex < aIndex);
    }

    [Fact]
    public void WhenInstanceGraphContainsCycleThenGenerationFailsWithCycleMessage()
    {
        var yaml = YamlTestHelper.LoadFixture("InstanceCycle.yaml");

        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var ex = Assert.Throws<DdiGenerationException>(() =>
            new CodeGenerator(new TemplateStore()).GenerateFiles(model));

        Assert.Single(ex.Diagnostics);
        Assert.Equal(MetWorks.DI.Declarative.Diagnostics.DiagnosticCode.DependencyCycleDetected, ex.Diagnostics[0].DiagnosticCode);
        Assert.Contains("Cycle detected", ex.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TheA", ex.Diagnostics[0].Message, StringComparison.Ordinal);
        Assert.Contains("TheB", ex.Diagnostics[0].Message, StringComparison.Ordinal);
    }
}
