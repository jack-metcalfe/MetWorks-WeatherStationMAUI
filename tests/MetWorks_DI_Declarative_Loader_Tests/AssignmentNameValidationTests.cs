using MetWorks.DI.Declarative.Diagnostics;
using MetWorks.DI.Declarative.Syntax;
using Xunit;

public sealed class AssignmentNameValidationTests
{
    [Fact]
    public void WhenAssignmentNameNotInYamlModelThenLoaderReportsDiagnostic()
    {
        var yaml = YamlTestHelper.LoadFixture("AssignmentNameNotInModel.yaml");

        var model = new Loader().Load(yaml);

        Assert.Contains(model.Diagnostics, d => d.DiagnosticCode == DiagnosticCode.AssignmentParameterNotFound);
    }
}
