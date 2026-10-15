using System.IO;
using System.Reflection;
using System.Windows;
using LibVLCSharp.Shared;

namespace MiIptv.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
            MessageBox.Show("Error inesperado:\n" + a.ExceptionObject, "MiIPTV",
                MessageBoxButton.OK, MessageBoxImage.Error);
        try
        {
            // Motor de video VLC (nativo: todos los códecs). Va en libvlc\win-x64 junto al exe.
            var dir = FindLibVlcDir();
            if (dir is null) LibVLCSharp.Shared.Core.Initialize();
            else LibVLCSharp.Shared.Core.Initialize(dir);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo iniciar el motor de video:\n" + ex.Message,
                "MiIPTV", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        base.OnStartup(e);
    }

    private static string? FindLibVlcDir()
    {
        var candidates = new List<string>();
        try
        {
            var loc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(loc)) candidates.Add(Path.Combine(loc, "libvlc", "win-x64"));
        }
        catch { }
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "libvlc", "win-x64"));
        return candidates.FirstOrDefault(d => File.Exists(Path.Combine(d, "libvlc.dll")));
    }
}
