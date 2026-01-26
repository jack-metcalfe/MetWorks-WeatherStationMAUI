using MetWorks.EventRelay;
using MetWorks.Common.Settings;
using MetWorks.Constants;
using Xunit;

public class EventRelayPathTests
{
    [Fact]
    public void RegisterWithBuildGroupPath_ReceivesMessagesForSettingPaths()
    {
        var relay = new EventRelayPath();

        var groupPrefix = LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildGroupPath();
        var settingPath = LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_airTemperature);

        int calls = 0;
        relay.Register(groupPrefix, sv =>
        {
            Assert.Equal(settingPath, sv.Path);
            calls++;
        });

        var sv = new SettingValue { Path = settingPath, Value = "degree celsius" };
        relay.Send(sv);

        Assert.Equal(1, calls);

        // Should not be called for other groups
        var otherPath = LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(SettingConstants.LoggerFile_fileSizeLimitBytes);
        var otherCalls = 0;
        relay.Register(LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildGroupPath(), _ => otherCalls++);
        relay.Send(new SettingValue { Path = otherPath, Value = "100" });
        Assert.Equal(1, otherCalls);
    }
}
