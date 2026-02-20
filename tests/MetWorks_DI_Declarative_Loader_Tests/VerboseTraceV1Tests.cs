public sealed class VerboseTraceV1Tests
{
    [Fact]
    public void WhenBuildingVerboseTraceThenHeaderIsEmitted()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var model = new Loader().Load(yaml);

        var lines = GenerationTraceV1.BuildLines(model, yamlPath: "WeatherStationMaui.yaml", outputDir: "out");

        Assert.Contains(lines, l => l.Contains("DDI_TRACE v=1 mode=verbose", StringComparison.Ordinal));
    }

    [Fact]
    public void WhenBuildingVerboseTraceThenOrderLineForTheRootCancellationTokenSourceIsEmitted()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var model = new Loader().Load(yaml);

        var lines = GenerationTraceV1.BuildLines(model, yamlPath: "WeatherStationMaui.yaml", outputDir: "out");

        Assert.Contains(lines, l => l.Contains("DDI_ORDER", StringComparison.Ordinal)
            && l.Contains("name=TheRootCancellationTokenSource", StringComparison.Ordinal)
            && l.Contains("class=System.Threading.CancellationTokenSource", StringComparison.Ordinal));
    }

    [Fact]
    public void WhenBuildingVerboseTraceThenInitLineForTheSettingRepositoryIsEmitted()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var model = new Loader().Load(yaml);

        var lines = GenerationTraceV1.BuildLines(model, yamlPath: "WeatherStationMaui.yaml", outputDir: "out");

        Assert.Contains(lines, l => l.Contains("DDI_INIT", StringComparison.Ordinal)
            && l.Contains("name=TheSettingRepository", StringComparison.Ordinal)
            && l.Contains("init=InitializeAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void WhenBuildingVerboseTraceThenBindLineForExternalCancellationDottedPropertyIsEmitted()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var model = new Loader().Load(yaml);

        var lines = GenerationTraceV1.BuildLines(model, yamlPath: "WeatherStationMaui.yaml", outputDir: "out");

        Assert.Contains(lines, l => l.Contains("DDI_BIND", StringComparison.Ordinal)
            && l.Contains("instance=TheMetricsSamplerService", StringComparison.Ordinal)
            && l.Contains("param=externalCancellation", StringComparison.Ordinal)
            && l.Contains("source=instance", StringComparison.Ordinal)
            && l.Contains("value=TheRootCancellationTokenSource.Token", StringComparison.Ordinal));
    }
}
