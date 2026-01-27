using MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;

namespace MetWorks.Common.Tests;

public class MainDeviceViewsCatalogTests
{
    [Fact]
    public void DefaultViewTypeName_IsRegistered()
    {
        var ok = MainDeviceViewsCatalog.TryGetViewType(MainDeviceViewsCatalog.DefaultViewTypeName, out var viewType);
        Assert.True(ok);
        Assert.NotNull(viewType);
    }

    [Fact]
    public void AllViewTypeNames_AreResolvable()
    {
        foreach (var name in MainDeviceViewsCatalog.AllViewTypeNames)
        {
            var ok = MainDeviceViewsCatalog.TryGetViewType(name, out var viewType);
            Assert.True(ok);
            Assert.NotNull(viewType);
        }
    }

    [Fact]
    public void AllViewTypes_Contains_DefaultViewType()
    {
        var ok = MainDeviceViewsCatalog.TryGetViewType(MainDeviceViewsCatalog.DefaultViewTypeName, out var viewType);
        Assert.True(ok);

        Assert.Contains(viewType, MainDeviceViewsCatalog.AllViewTypes);
    }
}
