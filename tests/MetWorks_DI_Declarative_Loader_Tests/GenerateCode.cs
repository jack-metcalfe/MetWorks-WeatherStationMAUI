public class Registry
{
    const string TargetFolder = @"../../../fixtures/Testing";
    readonly Loader Loader = new();
    [Fact]
    public void All()
    {
        var yaml = YamlTestHelper.LoadFixture("WeatherStationMaui.yaml");
        var rawModel = Loader.Load(yaml);
        var _templateStore = new TemplateStore();
        var codeGenerator = new CodeGenerator(_templateStore);
        var files = codeGenerator.GenerateFiles(rawModel);

        AssertUseBeforeInitGuards(files);
        AssertMauiExposeUsesInternalAccessors(files);
        SaveFiles(files);
    }

    private static void AssertUseBeforeInitGuards(IReadOnlyDictionary<string, string> files)
    {
        if (!files.TryGetValue("Accessors.g.cs", out var accessors))
            throw new InvalidOperationException("Expected generated file 'Accessors.g.cs' was not present.");

        // D4: Assignment-driven instances must throw if accessed before initialization has completed.
        // TheSettingRepository has assignments in the WeatherStationMaui.yaml fixture.
        Assert.Contains("GetTheSettingRepository()", accessors, StringComparison.Ordinal);
        Assert.Contains("_initTask_TheSettingRepository", accessors, StringComparison.Ordinal);
        Assert.Contains("was accessed before initialization", accessors, StringComparison.Ordinal);
    }

    private static void AssertMauiExposeUsesInternalAccessors(IReadOnlyDictionary<string, string> files)
    {
        if (!files.TryGetValue("ExposeToMauiDi.g.cs", out var exposeToMauiDi))
            throw new InvalidOperationException("Expected generated file 'ExposeToMauiDi.g.cs' was not present.");

        // MAUI DI exposure must be possible during the create phase (before InitializeAllAsync).
        // Assignment-driven instances (like TheSettingRepository) have guards on external getters,
        // so ExposeToMauiDi must use internal accessors.
        Assert.Contains("GetTheSettingRepository_Internal()", exposeToMauiDi, StringComparison.Ordinal);
        Assert.DoesNotContain("GetTheSettingRepository()", exposeToMauiDi, StringComparison.Ordinal);
    }
    private static void SaveFiles(IReadOnlyDictionary<string, string> files)
    {
        if (files == null) throw new ArgumentNullException(nameof(files));
        Directory.CreateDirectory(TargetFolder);
        foreach (var kvp in files)
            File.WriteAllText(Path.Combine(TargetFolder, kvp.Key), kvp.Value);
    }
}
