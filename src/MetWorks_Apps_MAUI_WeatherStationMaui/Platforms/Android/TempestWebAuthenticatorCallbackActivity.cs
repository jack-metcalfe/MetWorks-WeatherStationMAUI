using Android.App;
using Android.Content;
using Android.Content.PM;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Platforms.Android;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    actions: [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = MetWorks.Maui.Services.TempestOAuthTokenProvider.RedirectUriScheme
)]
public sealed class TempestWebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
