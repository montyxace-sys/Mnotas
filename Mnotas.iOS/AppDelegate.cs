using Avalonia;
using Avalonia.iOS;
using Foundation;
using MnotasApp = Mnotas.Portable.App.App;

namespace Mnotas.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<MnotasApp>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) => base.CustomizeAppBuilder(builder);
}
