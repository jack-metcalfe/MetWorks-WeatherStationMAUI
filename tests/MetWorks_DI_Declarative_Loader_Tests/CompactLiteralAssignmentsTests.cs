public sealed class CompactLiteralAssignmentsTests
{
    [Fact]
    public void WhenCompactLiteralAssignmentsAreUsedThenLoaderHasNoDiagnostics()
    {
        var yaml = YamlTestHelper.LoadFixture("CompactLiteralAssignments_Valid.yaml");
        var model = new Loader().Load(yaml);

        Assert.Empty(model.Diagnostics);

        var instance = Assert.Single(model.Instances);
        Assert.True(instance.HasAssignments);
        Assert.Equal(3, instance.Assignments.Count);

        Assert.Contains(instance.Assignments, a => a.Name == "maxBufferSize" && a.InitializerParameterAssignmentClause == "maxBufferSize: 1000");
        Assert.Contains(instance.Assignments, a => a.Name == "name" && a.InitializerParameterAssignmentClause == "name: \"hello\"");
        Assert.Contains(instance.Assignments, a => a.Name == "enabled" && a.InitializerParameterAssignmentClause == "enabled: true");
    }

    [Fact]
    public void WhenCompactLiteralAssignmentsAreUsedThenInitializerUsesExpectedClauses()
    {
        var yaml = YamlTestHelper.LoadFixture("CompactLiteralAssignments_Valid.yaml");
        var model = new Loader().Load(yaml);

        Assert.Empty(model.Diagnostics);

        var generator = new CodeGenerator(new TemplateStore());
        var files = generator.GenerateFiles(model);

        var key = "TheA_Initializer.g.cs";
        var initializer = files[key];

        Assert.Contains("maxBufferSize: 1000", initializer, StringComparison.Ordinal);
        Assert.Contains("name: \"hello\"", initializer, StringComparison.Ordinal);
        Assert.Contains("enabled: true", initializer, StringComparison.Ordinal);
    }
}
