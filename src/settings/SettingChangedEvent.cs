namespace Settings;

public class SettingChangedEvent : ISettingChangedEvent
{
    public required string Path { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public ISettingValue? Setting { get; init; }
}