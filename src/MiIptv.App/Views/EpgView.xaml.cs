using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

/// <summary>Parrilla EPG proporcional con línea AHORA (Xtream).</summary>
public partial class EpgView : UserControl
{
    private const double PxPerMin = 3.0;
    private const double RowH = 64.0;
    private const int Hours = 6;

    private List<LiveStream> _channels = new();
    private List<EpgProgram> _programs = new();
    private DateTimeOffset _windowStart;
    private bool _loaded;

    public EpgView()
    {
        InitializeComponent();
        var now = DateTimeOffset.Now;
        _windowStart = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var st = AppState.Current;
        if (st.ActiveProfile?.Type != "xtream" || st.Xtream is null)
        {
            WindowLabel.Text = "La guía EPG requiere un perfil Xtream";
            return;
        }
        WindowLabel.Text = "Cargando guía…";
        try
        {
            var chTask = st.Xtream.GetLiveStreamsAsync();
            var epgTask = st.Xtream.GetXmlTvAsync();
            await Task.WhenAll(chTask, epgTask);
            _channels = chTask.Result.Where(c => !string.IsNullOrEmpty(c.EpgChannelId)).ToList();
            _programs = epgTask.Result;
            Render();
        }
        catch (Exception ex) { WindowLabel.Text = "Error: " + ex.Message; }
    }

    private void Shift_Click(object sender, RoutedEventArgs e)
    {
        var d = int.Parse((string)((Button)sender).Tag);
        _windowStart = _windowStart.AddHours(d * Hours);
        Render();
    }

    private void Now_Click(object sender, RoutedEventArgs e)
    {
        var now = DateTimeOffset.Now;
        _windowStart = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
        Render();
    }

    private void Render()
    {
        ChannelColumn.Children.Clear();
        TimelineCanvas.Children.Clear();

        var windowEnd = _windowStart.AddHours(Hours);
        var now = DateTimeOffset.Now;
        WindowLabel.Text = $"{_windowStart:dd/MM HH:mm} – {windowEnd:HH:mm}";

        double width = Hours * 60 * PxPerMin;
        TimelineCanvas.Width = width;

        // cabecera de horas
        var headerH = 28.0;
        for (int h = 0; h <= Hours; h++)
        {
            var t = _windowStart.AddHours(h);
            var line = new Line
            {
                X1 = h * 60 * PxPerMin, X2 = h * 60 * PxPerMin,
                Y1 = 0, Y2 = headerH + _channels.Count * RowH,
                Stroke = new SolidColorBrush(Color.FromRgb(0x2c, 0x39, 0x47)), StrokeThickness = 1,
            };
            TimelineCanvas.Children.Add(line);
            var lbl = new TextBlock
            {
                Text = t.ToString("HH:mm"), Foreground = (Brush)FindResource("Muted"), FontSize = 11,
            };
            Canvas.SetLeft(lbl, h * 60 * PxPerMin + 4);
            Canvas.SetTop(lbl, 4);
            TimelineCanvas.Children.Add(lbl);
        }

        // línea AHORA
        if (now >= _windowStart && now <= windowEnd)
        {
            var x = (now - _windowStart).TotalMinutes * PxPerMin;
            var nowLine = new Line
            {
                X1 = x, X2 = x, Y1 = 0, Y2 = headerH + _channels.Count * RowH,
                Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 2,
            };
            TimelineCanvas.Children.Add(nowLine);
        }

        var byCh = _programs
            .Where(p => p.Stop > _windowStart && p.Start < windowEnd)
            .GroupBy(p => p.ChannelId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Start).ToList());

        int row = 0;
        foreach (var ch in _channels)
        {
            if (!byCh.TryGetValue(ch.EpgChannelId!, out var progs)) continue;
            double y = headerH + row * RowH;

            // nombre del canal
            var nameBlock = new Border
            {
                Height = RowH, BorderBrush = new SolidColorBrush(Color.FromRgb(0x2c, 0x39, 0x47)),
                BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(8, 4, 4, 4),
            };
            nameBlock.Child = new TextBlock
            {
                Text = ch.Name, FontWeight = FontWeights.Bold, FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center,
            };
            ChannelColumn.Children.Add(nameBlock);

            foreach (var pr in progs)
            {
                var s = pr.Start < _windowStart ? _windowStart : pr.Start;
                var e = pr.Stop > windowEnd ? windowEnd : pr.Stop;
                var mins = (e - s).TotalMinutes;
                if (mins < 3) continue;
                var x = (s - _windowStart).TotalMinutes * PxPerMin;
                var isNow = pr.Start <= now && pr.Stop > now;

                var block = new Border
                {
                    Width = mins * PxPerMin - 2, Height = RowH - 6,
                    Background = isNow ? (Brush)FindResource("Accent") : (Brush)FindResource("Panel2"),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x2c, 0x39, 0x47)),
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(1, 0, 1, 0), Cursor = Cursors.Hand,
                    ToolTip = $"{pr.Title}\n{s:HH:mm} – {e:HH:mm}\n{pr.Description}",
                    Tag = ch,
                };
                block.Child = new TextBlock
                {
                    Text = pr.Title, FontSize = 12, TextWrapping = TextWrapping.NoWrap,
                    TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(6, 4, 6, 4),
                };
                block.MouseLeftButtonUp += Program_Click;
                Canvas.SetLeft(block, x);
                Canvas.SetTop(block, y + 3);
                TimelineCanvas.Children.Add(block);
            }
            row++;
        }

        TimelineCanvas.Height = headerH + row * RowH;
        if (row == 0) WindowLabel.Text += " (sin datos en este rango)";
    }

    private void Program_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.Tag is LiveStream ch)
        {
            var st = AppState.Current;
            if (st.Xtream is null) return;
            var url = st.Xtream.LiveUrl(ch.StreamId);
            PlayerService.Current.Play(url, ch.Name);
            new PlayerWindow(ch.Name).Show();
        }
    }
}
