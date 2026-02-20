public sealed class YamlFormattingTests
{
    [Fact]
    public void WhenFormattingYamlThenOutputIsIdempotent()
    {
        var yaml = YamlTestHelper.LoadFixture("YamlFormatting_Input.yaml");

        var formatted1 = YamlFormatter.Format(yaml);
        var formatted2 = YamlFormatter.Format(formatted1);

        Assert.Equal(formatted1, formatted2);
    }

    [Fact]
    public void WhenFormattingYamlThenLoaderParsesWithoutDiagnostics()
    {
        var yaml = YamlTestHelper.LoadFixture("YamlFormatting_Input.yaml");

        var formatted = YamlFormatter.Format(yaml);
        var model = new Loader().Load(formatted);

        Assert.Empty(model.Diagnostics);
    }
}
