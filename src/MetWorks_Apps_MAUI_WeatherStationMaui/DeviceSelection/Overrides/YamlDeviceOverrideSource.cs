namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection.Overrides;
public sealed class YamlDeviceOverrideSource : IDeviceOverrideSource
{
    const string OverrideFileName = "device-overrides.yaml";
    readonly Lazy<DeviceOverridesIndex> _index;

    public YamlDeviceOverrideSource()
    {
        _index = new Lazy<DeviceOverridesIndex>(LoadIndex);
    }

    public bool TryGetOverride(
        LogicalContentKey content, 
        DeviceContext deviceContext, 
        out string variantKey
    )
    {
        variantKey = string.Empty;

        var index = _index.Value;
        var contentKey = content.ToString();
        if (!index.ContentOverrides.TryGetValue(contentKey, out var perContent))
            return false;

        var identityKey = DeviceOverridesIndex.ComposeIdentityKey(deviceContext.Platform, deviceContext.Manufacturer, deviceContext.Model);
        if (!perContent.TryGetValue(identityKey, out var byIdentity))
            return false;

        // Orientation-specific override wins
        var orientationKey = deviceContext.Orientation.ToString();
        if (byIdentity.TryGetValue(orientationKey, out var oriented))
        {
            variantKey = oriented;
            return true;
        }

        // Otherwise look for a default entry
        if (byIdentity.TryGetValue("Default", out var def))
        {
            variantKey = def;
            return true;
        }

        return false;
    }

    static DeviceOverridesIndex LoadIndex()
    {
        // Load from app package so this works on Android/Windows.
        try
        {
            using var fs = Task.Run(
                async () => {
                    return await FileSystem.OpenAppPackageFileAsync(
                        OverrideFileName
                    ).ConfigureAwait(false);
                }
            ).GetAwaiter().GetResult();

            var root = YamlHelpers.ParseStreamToMappingNode(
                fs, OverrideFileName
            );
            return DeviceOverridesIndex.FromYaml(root);
        }
        catch
        {
            // Optional file: if missing or malformed, return empty overrides.
            return DeviceOverridesIndex.Empty;
        }
    }

    sealed class DeviceOverridesIndex
    {
        public static readonly DeviceOverridesIndex Empty = new();

        // contentKey -> identityKey -> ("Default" or Orientation) -> variantKey
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> ContentOverrides { get; } = new(StringComparer.Ordinal);

        public static string ComposeIdentityKey(string platform, string manufacturer, string model)
            => $"{platform}|{manufacturer}|{model}";

        public static DeviceOverridesIndex FromYaml(YamlMappingNode root)
        {
            var idx = new DeviceOverridesIndex();

            var devicesNode = GetSequence(root, "devices");
            if (devicesNode is null)
                return idx;

            foreach (var item in devicesNode)
            {
                if (item is not YamlMappingNode device)
                    continue;

                var platform = GetScalar(device, "platform");
                var manufacturer = GetScalar(device, "manufacturer");
                var model = GetScalar(device, "model");

                if (
                    string.IsNullOrWhiteSpace(platform) 
                    || string.IsNullOrWhiteSpace(manufacturer) 
                    || string.IsNullOrWhiteSpace(model)
                )
                    continue;

                var identityKey = ComposeIdentityKey(
                    platform, manufacturer, model
                );

                var overrides = GetMapping(device, "overrides");
                if (overrides is null)
                    continue;

                foreach (var kvp in overrides.Children) {
                    var contentKey = ((YamlScalarNode)kvp.Key).Value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(contentKey))
                        continue;

                    if (!idx.ContentOverrides.TryGetValue(contentKey, out var perContent))
                    {
                        perContent = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                        idx.ContentOverrides[contentKey] = perContent;
                    }

                    if (!perContent.TryGetValue(identityKey, out var perIdentity))
                    {
                        perIdentity = new Dictionary<string, string>(StringComparer.Ordinal);
                        perContent[identityKey] = perIdentity;
                    }

                    // Two supported shapes:
                    // 1) overrides: { DefaultWeather: "VariantKey" }
                    // 2) overrides: { DefaultWeather: { Portrait: "VariantKey", Landscape: "VariantKey" } }
                    if (kvp.Value is YamlScalarNode scalar)
                    {
                        var value = scalar.Value;
                        if (!string.IsNullOrWhiteSpace(value))
                            perIdentity["Default"] = value;
                    }
                    else if (kvp.Value is YamlMappingNode map)
                    {
                        foreach (var okvp in map.Children)
                        {
                            if (okvp.Key is YamlScalarNode ok && okvp.Value is YamlScalarNode ov)
                            {
                                var oKey = ok.Value ?? string.Empty;
                                var oVal = ov.Value ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(oKey) && !string.IsNullOrWhiteSpace(oVal))
                                    perIdentity[oKey] = oVal;
                            }
                        }
                    }
                }
            }

            return idx;
        }

        static YamlSequenceNode? GetSequence(
            YamlMappingNode node, string key
        )
        {
            if (!node.Children.TryGetValue(new YamlScalarNode(key), out var child))
                return null;
            return child as YamlSequenceNode;
        }

        static YamlMappingNode? GetMapping(YamlMappingNode node, string key)
        {
            if (!node.Children.TryGetValue(new YamlScalarNode(key), out var child))
                return null;
            return child as YamlMappingNode;
        }

        static string? GetScalar(YamlMappingNode node, string key)
        {
            if (!node.Children.TryGetValue(new YamlScalarNode(key), out var child))
                return null;
            return (child as YamlScalarNode)?.Value;
        }
    }
}
