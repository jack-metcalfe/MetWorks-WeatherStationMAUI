public sealed class FactoryMethodBindingTests
{
    [Fact]
    public void WhenValidFactoryBindingIsPresentThenValidatorReturnsNoDiagnostics()
    {
        var yaml = YamlTestHelper.LoadFixture("FactoryBinding_Valid.yaml");
        var model = new Loader().Load(yaml);

        Assert.Empty(model.Diagnostics);

        var refs = new[] { typeof(Test.FactoryBinding.WidgetFactory).Assembly.Location };
        var diagnostics = FactoryMethodBindingValidator.Validate(model, refs);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void WhenFactoryBindingIsPresentThenInstanceFactoryUsesFactoryMethodCall()
    {
        var yaml = YamlTestHelper.LoadFixture("FactoryBinding_Valid.yaml");
        var model = new Loader().Load(yaml);

        Assert.Empty(model.Diagnostics);

        var generator = new CodeGenerator(new TemplateStore());
        var files = generator.GenerateFiles(model);

        var key = "TheWidget_InstanceFactory.g.cs";
        var file = files[key];

        Assert.Contains("registry.GetTheWidgetFactory_Internal().CreateWidget()", file, StringComparison.Ordinal);
    }
}
