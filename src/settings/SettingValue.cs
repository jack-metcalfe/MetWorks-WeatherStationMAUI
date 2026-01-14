namespace Settings;
public class SettingValue : ISettingValue
{
    public string Path { get; set; }
    public string? Value { get; set; }
}