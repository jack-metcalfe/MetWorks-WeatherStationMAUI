public sealed class GenerationReportV1Tests
{
    [Fact]
    public void WhenBuildingReportThenInitializerCallGraphHeaderIsPresent()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var model = new Loader().Load(yaml);

        var report = GenerationReportV1.BuildMarkdown(model, yamlPath: "WeatherStationMaui.yaml", outputDir: "out");

        Assert.Contains("## Initializer call graph (deterministic)", report, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenBuildingReportThenCallGraphIncludesSettingRepositoryGate()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var model = new Loader().Load(yaml);

        var report = GenerationReportV1.BuildMarkdown(model, yamlPath: "WeatherStationMaui.yaml", outputDir: "out");

        Assert.Contains("Registry.WhenTheSettingRepositoryInitializedAsync", report, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenBuildingReportThenUnusedModelEntriesHeaderIsPresent()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var model = new Loader().Load(yaml);

        var report = GenerationReportV1.BuildMarkdown(model, yamlPath: "WeatherStationMaui.yaml", outputDir: "out");

        Assert.Contains("## Unused namespace model entries", report, StringComparison.Ordinal);
    }
}
