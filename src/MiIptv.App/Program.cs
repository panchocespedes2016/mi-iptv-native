using System;

namespace MiIptv.App;

/// <summary>
/// Punto de entrada en una clase simple (sin dependencias WPF): así el
/// inicializador de módulo de StartupHook (que redirige a libs\) se registra
/// ANTES de que se cargue cualquier tipo de WPF. Si Main estuviera en App,
/// el runtime cargaría PresentationFramework antes del redireccionador.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
