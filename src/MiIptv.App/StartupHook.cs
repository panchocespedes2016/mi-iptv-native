using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace MiIptv.App;

/// <summary>
/// Las DLLs gestionadas viven en la subcarpeta "libs\" (la carpeta principal
/// queda limpia). Este inicializador de módulo se ejecuta antes que Main y
/// redirige la resolución de ensamblados hacia libs\.
/// </summary>
internal static class StartupHook
{
    [ModuleInitializer]
    internal static void Init()
    {
        var baseDir = AppContext.BaseDirectory;
        var libsDir = Path.Combine(baseDir, "libs");
        if (!Directory.Exists(libsDir)) return;

        string[]? subDirs = null;

        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            if (string.IsNullOrEmpty(name.Name)) return null;

            // 1) libs\<nombre>.dll
            var direct = Path.Combine(libsDir, name.Name + ".dll");
            if (File.Exists(direct))
                return context.LoadFromAssemblyPath(direct);

            // 2) libs\<subcarpeta>\<nombre>.dll (satélites de idioma: cs\, es\, ...)
            subDirs ??= Directory.GetDirectories(libsDir);
            foreach (var sub in subDirs)
            {
                var sat = Path.Combine(sub, name.Name + ".dll");
                if (File.Exists(sat))
                    return context.LoadFromAssemblyPath(sat);
            }
            return null;
        };
    }
}
