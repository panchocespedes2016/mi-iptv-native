using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MiIptv.App.Services;
using MiIptv.App.Views;

namespace MiIptv.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text = "v" + (v is null ? "?" : $"{v.Major}.{v.Minor}.{v.Build}");
        UpdateSourceLabel();
        Navigate("live");
    }

    private void UpdateSourceLabel() =>
        SourceLabel.Text = "Fuente: " + (AppState.Current.ActiveProfile?.Name ?? "—");

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b) Navigate(b.Tag as string ?? "");
    }

    private void Navigate(string tag)
    {
        UpdateSourceLabel();
        MainContent.Content = tag switch
        {
            "settings" => new SettingsView(),
            "live" => new LiveView(),
            "epg" => Placeholder("🗓 Guía", "Etapa 4: parrilla EPG."),
            "movies" => Placeholder("🎬 Películas", "Etapa 5: catálogo VOD."),
            "series" => Placeholder("📺 Series", "Etapa 5: catálogo de series."),
            "favs" => Placeholder("⭐ Favoritos", "Etapa 6."),
            "multi" => Placeholder("⊞ Multivista", "Etapa 6: 4 pantallas simultáneas."),
            _ => Placeholder("MiIPTV", ""),
        };
    }

    private static TextBlock Placeholder(string title, string sub) => new()
    {
        Text = $"{title}\n\n{sub}",
        FontSize = 18,
        Foreground = Brushes.Gray,
        Margin = new Thickness(24),
    };
}
