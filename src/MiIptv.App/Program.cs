using System;
using System.Runtime.CompilerServices;

namespace MiIptv.App;

/// <summary>
/// Punto de entrada en una clase simple (sin dependencias WPF).
/// Main solo toca tipos de CoreLib: primero registra el redireccionador a libs\
/// y después, en un método separado (NoInlining), arranca WPF.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
        StartupHook.Init();
        RunApp();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunApp()
    {
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
