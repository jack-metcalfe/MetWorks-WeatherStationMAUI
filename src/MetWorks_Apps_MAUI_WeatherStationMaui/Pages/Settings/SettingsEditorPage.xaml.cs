namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.Settings;

using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

public partial class SettingsEditorPage : ContentPage
{
    public SettingsEditorPage(SettingsEditorViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        InitializeComponent();
        BindingContext = vm;

        try
        {
            ToolbarItems.Add(
                new ToolbarItem
                {
                    Text = "Close",
                    Priority = 0,
                    Order = ToolbarItemOrder.Primary,
                    Command = new Command(async () =>
                    {
                        try
                        {
                            if (Shell.Current is null) return;
                            await Shell.Current.GoToAsync("///Weather/MainSwipeHostPage");
                        }
                        catch { }
                    })
                }
            );
        }
        catch { }
    }
}
