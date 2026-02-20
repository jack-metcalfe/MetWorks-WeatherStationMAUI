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
        SaveFiles(files);
    }
    private static void SaveFiles(IReadOnlyDictionary<string, string> files)
    {
        if (files == null) throw new ArgumentNullException(nameof(files));
        Directory.CreateDirectory(TargetFolder);
        foreach (var kvp in files)
            File.WriteAllText(Path.Combine(TargetFolder, kvp.Key), kvp.Value);
    }
}
