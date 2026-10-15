using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace MiIptv.App;

/// <summary>
/// Las DLLs (gestionadas y nativas) viven en la subcarpeta "libs\".
/// Este inicializador de módulo corre antes que Main y:
///  1) precarga los ensamblados de libs\ en el contexto por defecto, y
///  2) registra redireccionadores (gestionados y nativos) como respaldo.
/// Al lado del exe solo queda lo que el arranque del runtime exige ahí.
/// </summary>
internal static class StartupHook
{
    private static string? _log;

    [ModuleInitializer]
    internal static void Init()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            _log = Path.Combine(baseDir, "startup.log");
            var libsDir = Path.Combine(baseDir, "libs");
            Log($"Init baseDir={baseDir} libsExists={Directory.Exists(libsDir)}");
            if (!Directory.Exists(libsDir)) return;

            // 1) Precarga: evita depender del evento de resolución.
            int ok = 0, skip = 0;
            foreach (var dll in Directory.GetFiles(libsDir, "*.dll"))
            {
                try
                {
                    AssemblyName.GetAssemblyName(dll); // filtra nativas (lanza excepción)
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                    ok++;
                }
                catch (Exception ex) { skip++; Log($"skip {Path.GetFileName(dll)}: {ex.GetType().Name}"); }
            }
            Log($"Precarga: {ok} ok, {skip} omitidos");

            // 2) Respaldo: redireccionadores por si algo se pide después.
            string[]? subDirs = null;
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                Log($"Resolving {name?.Name}");
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
                return File.Exists(candidate) ? NativeLibrary.Load(candidate) : IntPtr.Zero;
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
