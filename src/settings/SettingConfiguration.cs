namespace Settings;
public partial class SettingConfiguration : ISettingConfiguration
{
    bool _isInitialized = false;

    string? _path;
    public string Path
    {
        get => NullPropertyGuard.Get(_isInitialized, _path, nameof(Path));
        internal set => NullPropertyGuard.Set(_isInitialized, ref _path, value, nameof(Path));
    }
    string? _default;
    public string Default
    {
        get => NullPropertyGuard.Get(_isInitialized, _default, nameof(Default));
        internal set => NullPropertyGuard.Set(_isInitialized, ref _default, value, nameof(Default));
    }
    string? _description;
    public string Description
    {
        get => NullPropertyGuard.Get(_isInitialized, _description, nameof(Description));
        internal set => NullPropertyGuard.Set(_isInitialized, ref _description, value, nameof(Description));
    }
    bool? _isEditable;
    public bool IsEditable
    {
        get => NullPropertyGuard.Get(_isInitialized, _isEditable, nameof(IsEditable));
        internal set => NullPropertyGuard.Set(_isInitialized, ref _isEditable, value, nameof(IsEditable));
    }
    string[]? _enumValues;
    public string[]? EnumValues
    {
        get => _enumValues;
        internal set
        {
            _enumValues = value;
        }
    }
    string? _expectedValueType;
    public string ExpectedValueType 
    {
        get => NullPropertyGuard.Get(_isInitialized, _expectedValueType, nameof(ExpectedValueType));
        internal set => NullPropertyGuard.Set(_isInitialized, ref _expectedValueType, value, nameof(ExpectedValueType));
    }
    string? _group;
    public string Group
    {
        get => NullPropertyGuard.Get(_isInitialized, _group, nameof(Group));
        internal set => NullPropertyGuard.Set(_isInitialized, ref _group, value, nameof(Group));
    }
    bool? _isSecret;
    public bool IsSecret
    {
        get => NullPropertyGuard.Get(_isInitialized, _isSecret, nameof(IsSecret));
        internal set => NullPropertyGuard.Set(_isInitialized, ref _isSecret, value, nameof(IsSecret));
    }
    public SettingConfiguration() { }
    public SettingConfiguration(
        string defaultValue,
        string description,
        string expectedValueType,
        string group,
        bool isEditable,
        string path,
        string[]? enumValues,
        bool? isSecret
    )
    {
        Default = defaultValue;
        Description = description;
        IsEditable = isEditable;
        EnumValues = enumValues;
        ExpectedValueType = expectedValueType;
        Group = group;
        IsSecret = isSecret ?? false;
        Path = path;
        _isInitialized = true;
    }
    public async Task<bool> InitializeAsync(
        string defaultValue,
        string description,
        string expectedValueType,
        string group,
        bool isEditable,
        string path,
        string[]? enumValues,
        bool? isSecret
    )
    {
        try
        {
            Default = defaultValue;
            Description = description;
            IsEditable = isEditable;
            EnumValues = enumValues;
            ExpectedValueType = expectedValueType;
            Group = group;
            IsSecret = isSecret ?? false;
            Path = path;

            await Task.CompletedTask;
            return _isInitialized = true;
        }
        catch (Exception exception)
        {
            throw new Exception("Error", exception);
        }
    }
}