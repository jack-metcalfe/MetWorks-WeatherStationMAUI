using MetWorks.DI.Declarative.Generator;
using MetWorks.DI.Declarative.Syntax;
using MetWorks.DI.Declarative.Diagnostics;
using SyntaxLoader.Tests.Fakes;
using Xunit;

public sealed class DottedPropertyReferenceValidatorTests
{
    [Fact]
    public void WhenDottedPropertyChainIsValidThenValidatorProducesNoDiagnostics()
    {
        var yaml = YamlTestHelper.LoadFixture("DottedProperty_Valid.yaml");
        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var diags = DottedPropertyReferenceValidator.Validate(model, new[] { typeof(DottedPropertyInitService).Assembly.Location });
        Assert.Empty(diags);
    }

    [Fact]
    public void WhenYAMLDoesNotDeclareIntermediatePropertyThenValidatorReportsYamlPropertyNotDeclared()
    {
        var yaml = YamlTestHelper.LoadFixture("DottedProperty_PropertyMissingInYaml.yaml");
        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var diags = DottedPropertyReferenceValidator.Validate(model, new[] { typeof(DottedPropertyInitService).Assembly.Location });

        Assert.Single(diags);
        Assert.Equal(DiagnosticCode.DottedPropertyYamlPropertyNotDeclared, diags[0].DiagnosticCode);
    }

    [Fact]
    public void WhenConcreteTypeDoesNotHavePropertyThenValidatorReportsConcretePropertyNotFound()
    {
        var yaml = YamlTestHelper.LoadFixture("DottedProperty_PropertyMissingInCSharp.yaml");
        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var diags = DottedPropertyReferenceValidator.Validate(model, new[] { typeof(DottedPropertyInitService).Assembly.Location });

        Assert.Single(diags);
        Assert.Equal(DiagnosticCode.DottedPropertyConcretePropertyNotFound, diags[0].DiagnosticCode);
    }

    [Fact]
    public void WhenExposedInterfaceDoesNotDeclareFirstSegmentThenValidatorReportsInterfacePropertyWarning()
    {
        var yaml = YamlTestHelper.LoadFixture("DottedProperty_InterfaceMissingProperty_Warns.yaml");
        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var diags = DottedPropertyReferenceValidator.Validate(model, new[] { typeof(DottedPropertyInitService).Assembly.Location });

        Assert.Single(diags);
        Assert.Equal(DiagnosticCode.DottedPropertyInterfacePropertyNotFound, diags[0].DiagnosticCode);
        Assert.Equal(DiagnosticSeverity.Warning, diags[0].DiagnosticCode.GetSeverity());
    }
}
