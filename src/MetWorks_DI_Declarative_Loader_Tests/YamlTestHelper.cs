public static class YamlTestHelper
{
    const string FixturesPath = @"../../../fixtures/Load";
    public static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine(FixturesPath, name));
}
