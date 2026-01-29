namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

public interface IContentViewFactory
{
    View Create(
        LogicalContentKey content, 
        DeviceContext deviceContext
    );
}
