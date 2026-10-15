using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiIptv.App;

/// <summary>
/// Las DLLs (gestionadas y nativas) viven en la subcarpeta "libs\".
/// Este hook se llama desde Program.Main ANTES de tocar cualquier tipo WPF.
/// IMPORTANTE: todo lo que ejecuta Init() usa SOLO tipos de System.Private.CoreLib.
/// (Ni AssemblyLoadContext ni NativeLibrary: viven en otros ensamblados que
/// estarían en libs\ y no se podrían resolver antes de que el hook exista.)
/// - Gestionadas: se resuelven con AppDomain.AssemblyResolve (CoreLib).
/// - Nativas: se precargan con LoadLibrary de kernel32 (el SO las encuentra
///   después para los DllImport).
/// </summary>
internal static class StartupHook
{
    private static string? _log;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryNative(string lpFileName);

    internal static void Init()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            _log = Path.Combine(baseDir, "startup.log");
            var libsDir = Path.Combine(baseDir, "libs");
            Log($"Init baseDir={baseDir} libsExists={Directory.Exists(libsDir)}");
            if (!Directory.Exists(libsDir)) return;

            // Nativas de libs\: precargarlas para que los DllImport las encuentren.
            int nat = 0;
            foreach (var dll in Directory.GetFiles(libsDir, "*.dll"))
            {
                bool managed;
                try { AssemblyName.GetAssemblyName(dll); managed = true; }
                catch { managed = false; }
                if (managed) continue;
                try
                {
                    if (LoadLibraryNative(dll) != IntPtr.Zero) nat++;
                    else Log($"LoadLibrary falló: {Path.GetFileName(dll)}");
                }
                catch (Exception ex) { Log($"LoadLibrary ex {Path.GetFileName(dll)}: {ex.GetType().Name}"); }
            }
            Log($"Nativas precargadas: {nat}");

            // Gestionadas: resolver bajo demanda desde libs\ (y subcarpetas de idioma).
            string[]? subDirs = null;
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    var name = new AssemblyName(args.Name).Name;
                    if (string.IsNullOrEmpty(name)) return null;
                    var direct = Path.Combine(libsDir, name + ".dll");
                    if (File.Exists(direct)) return Assembly.LoadFrom(direct);
                    subDirs ??= Directory.GetDirectories(libsDir);
                    foreach (var sub in subDirs)
                    {
                        var sat = Path.Combine(sub, name + ".dll");
                        if (File.Exists(sat)) return Assembly.LoadFrom(sat);
                    }
                }
                catch (Exception ex) { Log($"AssemblyResolve {args.Name}: {ex.GetType().Name}"); }
                return null;
            };
            Log("AssemblyResolve registrado");
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(_log ?? "startup.log", "Init ERROR: " + ex + "\n"); } catch { }
        }
    }

    private static void Log(string msg)
    {
        try { if (_log != null) File.AppendAllText(_log, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); } catch { }
    }
}
