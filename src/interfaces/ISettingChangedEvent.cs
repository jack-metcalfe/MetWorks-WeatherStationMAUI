namespace Interfaces;

public interface ISettingChangedEvent
{
    string Path { get; }
    string? OldValue { get; }
    string? NewValue { get; }
    ISettingValue? Setting { get; }
}