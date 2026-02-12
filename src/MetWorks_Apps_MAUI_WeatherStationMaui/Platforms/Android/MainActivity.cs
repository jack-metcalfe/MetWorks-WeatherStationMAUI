using Android.App;
using Android.OS;
using Android.Content.PM;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize
                       | ConfigChanges.Orientation
                       | ConfigChanges.UiMode
                       | ConfigChanges.ScreenLayout
                       | ConfigChanges.SmallestScreenSize)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        ApplyImmersiveFullscreen();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplyImmersiveFullscreen();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        if (hasFocus)
            ApplyImmersiveFullscreen();
    }

    void ApplyImmersiveFullscreen()
    {
        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller is null) return;

        controller.Hide(WindowInsetsCompat.Type.SystemBars());
        controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
    }
}