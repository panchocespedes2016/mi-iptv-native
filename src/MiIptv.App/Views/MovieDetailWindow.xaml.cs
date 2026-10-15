using System.Text.Json;
using System.Windows;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

public partial class MovieDetailWindow : Window
{
    private readonly VodStream _vod;
    private string? _playUrl;

    public MovieDetailWindow(VodStream vod)
    {
        InitializeComponent();
        _vod = vod;
        TitleLabel.Text = vod.Name;
        Poster.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(vod.StreamIcon ?? ""));
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var st = AppState.Current;
        if (st.Xtream is null) return;
        StatusLabel.Text = "Cargando ficha…";
        try
        {
            var el = await st.Xtream.GetVodInfoAsync(_vod.StreamId);
            var info = el.GetProperty("info");
            var container = Str(info, "container_extension");
            if (string.IsNullOrEmpty(container)) container = _vod.ContainerExtension ?? "mp4";
            _playUrl = st.Xtream.MovieUrl(_vod.StreamId, container);

            var genre = Str(info, "genre");
            var rating = Str(info, "rating");
            var year = Str(info, "releasedate");
            var dur = Str(info, "duration");
            MetaLabel.Text = string.Join("  •  ",
                new[] { year, genre, dur, string.IsNullOrEmpty(rating) ? "" : $"★ {rating}" }
                .Where(s => !string.IsNullOrEmpty(s)));
            PlotLabel.Text = Str(info, "plot");
            var cast = Str(info, "cast");
            if (!string.IsNullOrEmpty(cast)) CastLabel.Text = "Reparto: " + cast;
            StatusLabel.Text = "";
        }
        catch (Exception ex) { StatusLabel.Text = "Error: " + ex.Message; }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_playUrl is null) return;
        PlayerService.Current.Play(_playUrl, _vod.Name);
        new PlayerWindow(_vod.Name).Show();
        Close();
    }
}
