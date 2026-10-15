using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MiIptv.App.Services;

namespace MiIptv.App.Views;

public partial class PlayerWindow : Window
{
    private readonly DispatcherTimer _hideTimer;

    public PlayerWindow(string title)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        VideoView.MediaPlayer = PlayerService.Current.Player;
        PlayerService.Current.StateChanged += UpdatePlayPause;
        UpdatePlayPause();

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _hideTimer.Tick += (_, _) => ShowBars(false);
        _hideTimer.Start();

        Closed += (_, _) =>
        {
            _hideTimer.Stop();
            PlayerService.Current.StateChanged -= UpdatePlayPause;
            PlayerService.Current.Stop();
        };
    }

    private void UpdatePlayPause() =>
        Dispatcher.Invoke(() => PlayPauseBtn.Content = PlayerService.Current.Player.IsPlaying ? "⏸" : "▶");

    private void ShowBars(bool show)
    {
        TopBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        BottomBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        Cursor = show ? Cursors.Arrow : Cursors.None;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        ShowBars(true);
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
        else if (e.Key == Key.Space) { PlayerService.Current.TogglePlayPause(); e.Handled = true; }
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e) => PlayerService.Current.TogglePlayPause();
    private void Stop_Click(object sender, RoutedEventArgs e) => PlayerService.Current.Stop();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
    }

    private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        try { PlayerService.Current.Player.Volume = (int)e.NewValue; } catch { }
    }
}
