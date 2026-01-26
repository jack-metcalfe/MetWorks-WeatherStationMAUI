using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

public class DdiOrderTests
{
    [Fact]
    public void Registry_CreateAll_Order_Matches_Yaml_Instance_Order()
    {
        // Locate repo root by searching upward for known generated registry file
        var start = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var repoRoot = FindRepoRoot(start) ?? throw new InvalidOperationException("Repository root not found");

        var yamlPath = Path.Combine(repoRoot, "src", "MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs", "WeatherStationMaui.yaml");
        var registryPath = Path.Combine(repoRoot, "src", "MetWorks_DdiRegistry", "Registry.g.cs");

        Assert.True(File.Exists(yamlPath), $"YAML file not found: {yamlPath}");
        Assert.True(File.Exists(registryPath), $"Registry file not found: {registryPath}");

        var yamlLines = File.ReadAllLines(yamlPath);
        var registryText = File.ReadAllText(registryPath);

        var instanceNames = ParseYamlInstanceNames(yamlLines);
        Assert.NotEmpty(instanceNames);

        var createdNames = ParseRegistryCreateAllOrder(registryText);
        Assert.NotEmpty(createdNames);

        // Compare sequences
        Assert.Equal(instanceNames, createdNames);
    }

    [Fact]
    public void Registry_InitializeAll_Order_Matches_Yaml_Instance_Order()
    {
        var start = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var repoRoot = FindRepoRoot(start) ?? throw new InvalidOperationException("Repository root not found");

        var yamlPath = Path.Combine(repoRoot, "src", "MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs", "WeatherStationMaui.yaml");
        var registryPath = Path.Combine(repoRoot, "src", "MetWorks_DdiRegistry", "Registry.g.cs");

        Assert.True(File.Exists(yamlPath), $"YAML file not found: {yamlPath}");
        Assert.True(File.Exists(registryPath), $"Registry file not found: {registryPath}");

        var yamlLines = File.ReadAllLines(yamlPath);
        var registryText = File.ReadAllText(registryPath);

        var instanceNames = ParseYamlInstanceNames(yamlLines);
        Assert.NotEmpty(instanceNames);

        var initOrder = ParseRegistryInitializeAllOrder(registryText);
        Assert.NotEmpty(initOrder);

        // initOrder entries are like TheSettingProvider -> match to instance names
        Assert.Equal(instanceNames, initOrder);
    }

    static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "MetWorks_DdiRegistry", "Registry.g.cs");
            if (File.Exists(candidate)) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    static string[] ParseYamlInstanceNames(string[] lines)
    {
        var names = new System.Collections.Generic.List<string>();
        bool inInstanceSection = false;
        foreach (var raw in lines)
        {
            var line = raw.TrimStart();
            if (!inInstanceSection)
            {
                if (line.StartsWith("instance:", StringComparison.Ordinal))
                {
                    inInstanceSection = true;
                }
                continue;
            }

            // stop when reaching another top-level section
            if (!raw.StartsWith(" ") && !raw.StartsWith("\t")) break;

            var m = Regex.Match(line, "^- name:\s*\"?(?<n>[^\"\s]+)\"?");
            if (m.Success)
            {
                names.Add(m.Groups["n"].Value.Trim());
            }
        }
        return names.ToArray();
    }

    static string[] ParseRegistryInitializeAllOrder(string registryText)
    {
        var m = Regex.Match(registryText, @"public async Task InitializeAllAsync\(\)\s*\{(?<body>[\s\S]*?)\n\s*\}", RegexOptions.Multiline);
        if (!m.Success) return Array.Empty<string>();
        var body = m.Groups["body"].Value;

        var names = new System.Collections.Generic.List<string>();
        // Match lines like: await TheSettingProvider_Initializer.Initialize_TheSettingProviderAsync(this).ConfigureAwait(false);
        var rx = new Regex("await\s+(?<init>\\w+)_Initializer\\.Initialize_\\w+Async\\(this\\)", RegexOptions.Multiline);
        foreach (Match mm in rx.Matches(body))
        {
            var init = mm.Groups["init"].Value; // e.g., TheSettingProvider
            names.Add(init);
        }
        return names.ToArray();
    }

    static string[] ParseRegistryCreateAllOrder(string registryText)
    {
        var m = Regex.Match(registryText, @"public void CreateAll\(\)\s*\{(?<body>[\s\S]*?)\n\s*\}", RegexOptions.Multiline);
        if (!m.Success) return Array.Empty<string>();
        var body = m.Groups["body"].Value;

        var names = new System.Collections.Generic.List<string>();
        var rx = new Regex("\\b(?<factory>\\w+)_InstanceFactory\\.Create\\(this\\)\\s*;", RegexOptions.Multiline);
        foreach (Match mm in rx.Matches(body))
        {
            var factory = mm.Groups["factory"].Value;
            names.Add(factory);
        }
        return names.ToArray();
    }
}
