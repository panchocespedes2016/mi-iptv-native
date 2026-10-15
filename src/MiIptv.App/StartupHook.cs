using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace MiIptv.App;

/// <summary>
/// Las DLLs (gestionadas y nativas) viven en la subcarpeta "libs\".
/// Este hook se llama desde Program.Main ANTES de tocar cualquier tipo WPF.
/// Todo lo que ejecuta Init() usa solo tipos de System.Private.CoreLib para
/// no requerir ensamblados de libs\ antes de que el redireccionador exista.
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

            // 1) Precarga de ensamblados gestionados de libs\.
            int ok = 0, skip = 0;
            foreach (var dll in Directory.GetFiles(libsDir, "*.dll"))
            {
                try
                {
                    AssemblyName.GetAssemblyName(dll); // filtra nativas
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                    ok++;
                }
                catch (Exception ex) { skip++; Log($"skip {Path.GetFileName(dll)}: {ex.GetType().Name}"); }
            }
            Log($"Precarga: {ok} ok, {skip} omitidos");

            // 2) Redireccionadores como respaldo.
            string[]? subDirs = null;
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                if (string.IsNullOrEmpty(name?.Name)) return null;
                var direct = Path.Combine(libsDir, name.Name + ".dll");
                if (File.Exists(direct))
                    return context.LoadFromAssemblyPath(direct);
                subDirs ??= Directory.GetDirectories(libsDir);
                foreach (var sub in subDirs)
                {
                    var sat = Path.Combine(sub, name.Name + ".dll");
                    if (File.Exists(sat))
                        return context.LoadFromAssemblyPath(sat);
                }
                return null;
            };

            AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) =>
            {
                var candidate = Path.Combine(libsDir, name + ".dll");
                if (!File.Exists(candidate)) return IntPtr.Zero;
                var h = LoadLibraryNative(candidate);
                Log($"Unmanaged {name} -> {(h == IntPtr.Zero ? "FALLO" : "ok")}");
                return h;
            };
            Log("Handlers registrados");
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
