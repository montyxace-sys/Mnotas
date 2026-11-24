using Avalonia;
using MnotasApp = Mnotas.Portable.App.App;

namespace Mnotas.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<MnotasApp>().UsePlatformDetect();
}
