using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using MiIptv.App.Services;

namespace MiIptv.App.Views;

public partial class PlayerWindow : Window
{
    private readonly DispatcherTimer _hideTimer;
    private readonly DispatcherTimer _osdTimer;
    private readonly int? _streamId;
    private bool _metaLoaded;

    public PlayerWindow(string title, int? xtreamStreamId = null)
    {
        InitializeComponent();
        _streamId = xtreamStreamId;
        TitleLabel.Text = title;
        OsdTitle.Text = title;
        VideoView.MediaPlayer = PlayerService.Current.Player;
        PlayerService.Current.StateChanged += OnStateChanged;
        UpdatePlayPause();

        PlayerService.Current.Player.Playing += (_, _) =>
            Dispatcher.InvokeAsync(LoadMetadataAsync);

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _hideTimer.Tick += (_, _) => ShowBars(false);
        _hideTimer.Start();

        _osdTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _osdTimer.Tick += async (_, _) => await LoadMetadataAsync();

        Closed += (_, _) =>
        {
            _hideTimer.Stop();
            _osdTimer.Stop();
            PlayerService.Current.StateChanged -= OnStateChanged;
            PlayerService.Current.Stop();
        };
    }

    private void OnStateChanged() => UpdatePlayPause();

    private void UpdatePlayPause() =>
        Dispatcher.Invoke(() => PlayPauseBtn.Content = PlayerService.Current.Player.IsPlaying ? "⏸" : "▶");

    private async Task LoadMetadataAsync()
    {
        if (_metaLoaded) return;
        _metaLoaded = true;
        var ps = PlayerService.Current;
        try
        {
            // pistas de audio y subtítulos
            await Dispatcher.InvokeAsync(() =>
            {
                var audioOpts = ps.Player.AudioTrackDescription
                    .Select(t => new TrackOption(t.ID, t.Name)).ToList();
                AudioBox.ItemsSource = audioOpts;
                AudioBox.SelectedItem = audioOpts.FirstOrDefault(o => o.Id == ps.Player.AudioTrack);

                var spuOpts = ps.Player.SpuDescription
                    .Select(t => new TrackOption(t.ID, t.Name)).ToList();
                SpuBox.ItemsSource = spuOpts;
                SpuBox.SelectedItem = spuOpts.FirstOrDefault(o => o.Id == ps.Player.Spu);
            });

            // datos técnicos
            var tech = new StringBuilder();
            var tracks = ps.Player.Media?.Tracks;
            if (tracks is not null)
            {
                var v = tracks.OfType<VideoTrack>().FirstOrDefault();
                if (v is not null)
                {
                    var fps = v.FrameRateDen > 0 ? (double)v.FrameRateNum / v.FrameRateDen : 0;
                    tech.Append($"{FourCC(v.Codec)} {v.Width}x{v.Height}");
                    if (fps > 0) tech.Append($" {fps:0}fps");
                }
                var a = tracks.OfType<AudioTrack>().FirstOrDefault();
                if (a is not null)
                {
                    if (tech.Length > 0) tech.Append("  •  ");
                    tech.Append($"{FourCC(a.Codec)} {a.Channels}ch {a.Rate / 1000.0:0.#}kHz");
                }
            }
            await Dispatcher.InvokeAsync(() => OsdTech.Text = tech.ToString());

            // programa actual/siguiente (Xtream)
            if (_streamId.HasValue && AppState.Current.Xtream is not null)
            {
                var progs = await AppState.Current.Xtream.GetShortEpgAsync(_streamId.Value, 4);
                var now = DateTimeOffset.Now;
                var cur = progs.FirstOrDefault(p => p.Start <= now && p.Stop > now);
                var next = progs.FirstOrDefault(p => p.Start > now);
                await Dispatcher.InvokeAsync(() =>
                {
                    if (cur is not null)
                    {
                        OsdNow.Text = $"▶ {cur.Title}  ({cur.Start:HH:mm}–{cur.Stop:HH:mm})";
                        var total = (cur.Stop - cur.Start).TotalMinutes;
                        OsdProgress.Value = total > 0 ? (now - cur.Start).TotalMinutes / total * 100 : 0;
                        OsdDesc.Text = cur.Description ?? "";
                    }
                    else OsdNow.Text = "Sin información del programa";
                    OsdNext.Text = next is null ? "" : $"Siguiente: {next.Title} ({next.Start:HH:mm})";
                });
            }
            _osdTimer.Start();
        }
        catch { /* OSD es informativo; no rompe la reproducción */ }
    }

    private static string FourCC(uint codec)
    {
        var b = BitConverter.GetBytes(codec);
        var s = Encoding.ASCII.GetString(b).Trim('\0', ' ');
        return string.IsNullOrEmpty(s) ? "?" : s.ToUpperInvariant();
    }

    private void Osd_Click(object sender, RoutedEventArgs e) => ToggleOsd();
    private void ToggleOsd() =>
        OsdPanel.Visibility = OsdPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;

    private void Audio_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (AudioBox.SelectedItem is TrackOption t)
            try { PlayerService.Current.Player.AudioTrack = t.Id; } catch { }
    }

    private void Spu_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (SpuBox.SelectedItem is TrackOption t)
            try { PlayerService.Current.Player.Spu = t.Id; } catch { }
    }

    private sealed record TrackOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    private void ShowBars(bool show)
    {
        TopBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        BottomBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show) OsdPanel.Visibility = Visibility.Collapsed;
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
        else if (e.Key == Key.I) ToggleOsd();
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
