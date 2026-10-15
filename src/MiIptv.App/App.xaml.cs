using System.Windows;
using LibVLCSharp.Shared;

namespace MiIptv.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Motor de video VLC (nativo: todos los códecs)
        Core.Initialize();
        base.OnStartup(e);
    }
}
