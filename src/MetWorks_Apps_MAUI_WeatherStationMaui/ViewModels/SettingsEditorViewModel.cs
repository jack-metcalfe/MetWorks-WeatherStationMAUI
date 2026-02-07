namespace MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;
public sealed class SettingsEditorViewModel : INotifyPropertyChanged
{
    readonly ISettingRepository _iSettingRepository;
    readonly ILoggerResilient _iLoggerResilient;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SettingItemViewModel> Items { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand ReloadCommand { get; }

    string? _status;
    public string? Status
    {
        get => _status;
        private set
        {
            if (string.Equals(_status, value, StringComparison.Ordinal)) return;
            _status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }

    public SettingsEditorViewModel(
        ISettingRepository iSettingRepository,
        ILoggerResilient iLoggerResilient
    )
    {
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iLoggerResilient);

        _iSettingRepository = iSettingRepository;
        _iLoggerResilient = iLoggerResilient;

        SaveCommand = new Command(Save);
        ReloadCommand = new Command(Load);

        Load();
    }

    void Load()
    {
        try
        {
            Items.Clear();

            var defs = _iSettingRepository
                .GetAllDefinitions()
                .Where(d => d.IsEditable)
                .OrderBy(d => d.Path, StringComparer.Ordinal);

            foreach (var def in defs)
            {
                var current = _iSettingRepository.GetValueOrDefault(def.Path) ?? def.DefaultValue;
                Items.Add(new SettingItemViewModel(def, current));
            }

            Status = $"Loaded {Items.Count} setting(s).";
        }
        catch (Exception ex)
        {
            Status = "Failed to load settings.";
            _iLoggerResilient.Error(Status, ex);
        }
    }

    void Save()
    {
        try
        {
            var overrides = Items
                .Select(i => new SettingValue { Path = i.Path, Value = i.EditedValue })
                .Cast<ISettingValue>()
                .ToList();

            var ok = _iSettingRepository.ApplyOverrides(overrides);
            if (ok)
            {
                Load();
                Status = "Saved.";
            }
            else
            {
                Status = "Save failed.";
            }
        }
        catch (Exception ex)
        {
            Status = "Save failed.";
            _iLoggerResilient.Error(Status, ex);
        }
    }

    public sealed class SettingItemViewModel : INotifyPropertyChanged
    {
        readonly ISettingDefinition _definition;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Path => _definition.Path;
        public string Description => _definition.Description;
        public bool IsSecret => _definition.IsSecret;

        public string? ExpectedValueType => _definition.ExpectedValueType;
        public IReadOnlyList<string> AllowableValues => _definition.AllowableValues ?? Array.Empty<string>();
        public bool HasAllowableValues => AllowableValues.Count > 0;

        string _editedValue;
        public string EditedValue
        {
            get => _editedValue;
            set
            {
                if (string.Equals(_editedValue, value, StringComparison.Ordinal)) return;
                _editedValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditedValue)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditedValueDisplay)));
            }
        }

        public string EditedValueDisplay => IsSecret ? "••••••" : EditedValue;

        public SettingItemViewModel(ISettingDefinition definition, string currentValue)
        {
            ArgumentNullException.ThrowIfNull(definition);
            _definition = definition;
            _editedValue = currentValue ?? string.Empty;
        }
    }
}
