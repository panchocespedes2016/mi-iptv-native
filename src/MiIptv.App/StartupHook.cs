using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace MiIptv.App;

/// <summary>
/// Las DLLs (gestionadas y nativas de WPF/diagnóstico) viven en la subcarpeta "libs\".
/// Este inicializador de módulo se ejecuta antes que Main y redirige la
/// resolución de ensamblados y de librerías nativas hacia libs\.
/// Al lado del exe solo queda lo que el arranque del runtime exige ahí:
/// exe, dll de entrada, jsons, la cadena del host (hostfxr/hostpolicy/coreclr/…)
/// y System.Private.CoreLib.
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

        // Ensamblados gestionados -> libs\*.dll y libs\<idioma>\*.resources.dll
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            if (string.IsNullOrEmpty(name.Name)) return null;

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

        // Librerías nativas (D3DCompiler, PresentationNative, …) -> libs\*.dll
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) =>
        {
            var candidate = Path.Combine(libsDir, name + ".dll");
            return File.Exists(candidate) ? NativeLibrary.Load(candidate) : IntPtr.Zero;
        };
    }
}
