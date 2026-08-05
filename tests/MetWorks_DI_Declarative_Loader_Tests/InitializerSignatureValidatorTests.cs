using MetWorks.DI.Declarative.Generator;
using MetWorks.DI.Declarative.Syntax;
using MetWorks.DI.Declarative.Diagnostics;
using SyntaxLoader.Tests.Fakes;
using Xunit;

public sealed class InitializerSignatureValidatorTests
{
    [Fact]
    public void WhenYamlMatchesInitializeAsyncSignatureThenValidatorProducesNoDiagnostics()
    {
        var yaml = YamlTestHelper.LoadFixture("InitializerSignature_Valid.yaml");
        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var diags = InitializerSignatureValidator.Validate(model, new[] { typeof(InitService).Assembly.Location });
        Assert.Empty(diags);
    }

    [Fact]
    public void WhenYamlParameterTypeDiffersFromInitializeAsyncThenValidatorReportsTypeMismatch()
    {
        var yaml = YamlTestHelper.LoadFixture("InitializerSignature_TypeMismatch.yaml");
        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var diags = InitializerSignatureValidator.Validate(model, new[] { typeof(InitService).Assembly.Location });

        Assert.Single(diags);
        Assert.Equal(DiagnosticCode.InitializerSignatureParameterTypeMismatch, diags[0].DiagnosticCode);
    }

    [Fact]
    public void WhenYamlOmitsRequiredInitializeAsyncParameterThenValidatorReportsMissingRequiredParameter()
    {
        var yaml = YamlTestHelper.LoadFixture("InitializerSignature_MissingRequired.yaml");
        var model = new Loader().Load(yaml);
        Assert.Empty(model.Diagnostics);

        var diags = InitializerSignatureValidator.Validate(model, new[] { typeof(InitService).Assembly.Location });

        Assert.Single(diags);
        Assert.Equal(DiagnosticCode.InitializerSignatureMissingRequiredParameter, diags[0].DiagnosticCode);
    }
}
