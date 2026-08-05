public sealed class InstanceSortingTests
{
    [Fact]
    public void WhenSortingInstancesThenDependenciesComeFirst()
    {
        var yaml = YamlTestHelper.LoadFixture("InstanceSorting_Input_Unsorted.yaml");

        var sorted = YamlFormatter.SortInstancesByDependency(yaml);

        var indexA = sorted.IndexOf("- name: TheA", StringComparison.Ordinal);
        var indexB = sorted.IndexOf("- name: TheB", StringComparison.Ordinal);

        Assert.True(indexA >= 0);
        Assert.True(indexB >= 0);
        Assert.True(indexA < indexB);
    }

    [Fact]
    public void WhenSortingInstancesThenLoaderParsesWithoutDiagnostics()
    {
        var yaml = YamlTestHelper.LoadFixture("InstanceSorting_Input_Unsorted.yaml");

        var sorted = YamlFormatter.SortInstancesByDependency(yaml);
        var model = new Loader().Load(sorted);

        Assert.Empty(model.Diagnostics);
    }
}
