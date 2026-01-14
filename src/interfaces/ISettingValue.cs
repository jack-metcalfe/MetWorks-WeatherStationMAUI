namespace Interfaces;

public interface ISettingValue
{
    string Path { get; }
    string? Value { get; }
}