using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using MnotasApp = Mnotas.Portable.App.App;

namespace Mnotas.Android;

[Activity(
    Label = "Mnotas",
    Theme = "@style/MnotasTheme.NoActionBar",
    Icon = "@drawable/Icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity<MnotasApp>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) => base.CustomizeAppBuilder(builder);
}
