using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using MiIptv.Core;

namespace MiIptv.App;

public partial class MainWindow : Window
{
    private readonly AppConfig _config = AppConfig.Load();

    public MainWindow()
    {
        InitializeComponent();
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text = "v" + (v is null ? "?" : $"{v.Major}.{v.Minor}.{v.Build}");
        ShowPlaceholder("live", "📡 TV en vivo");
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        var tag = b.Tag as string ?? "";
        ShowPlaceholder(tag, b.Content as string ?? tag);
    }

    private void ShowPlaceholder(string tag, string title)
    {
        MainContent.Content = new TextBlock
        {
            Text = $"{title}\n\nEn construcción (etapa 1: esqueleto + motor VLC).",
            FontSize = 18,
            Foreground = (System.Windows.Media.Brush)FindResource("Muted"),
            Margin = new Thickness(24),
        };
    }
}
