using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MiIptv.App.Models;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

public partial class SeriesDetailWindow : Window
{
    private readonly SeriesStream _series;
    private Dictionary<string, List<Episode>> _seasons = new();
    private bool _loadingSeason;

    public SeriesDetailWindow(SeriesStream series)
    {
        InitializeComponent();
        _series = series;
        TitleLabel.Text = series.Name;
        if (!string.IsNullOrEmpty(series.Cover))
            Cover.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(series.Cover));
        MetaLabel.Text = string.Join("  •  ",
            new[] { series.ReleaseDate, series.Genre,
                    string.IsNullOrEmpty(series.Rating) ? "" : $"★ {series.Rating}" }
            .Where(s => !string.IsNullOrEmpty(s)));
        PlotLabel.Text = series.Plot ?? "";
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var st = AppState.Current;
        if (st.Xtream is null) return;
        StatusLabel.Text = "Cargando temporadas…";
        try
        {
            var el = await st.Xtream.GetSeriesInfoAsync(_series.SeriesId);
            if (el.TryGetProperty("episodes", out var eps) && eps.ValueKind == JsonValueKind.Object)
            {
                foreach (var season in eps.EnumerateObject())
                {
                    var list = new List<Episode>();
                    foreach (var ep in season.Value.EnumerateArray())
                    {
                        var id = Str(ep, "id");
                        var num = ep.TryGetProperty("episode_num", out var n) ? n.GetInt32() : 0;
                        var title = Str(ep, "title");
                        var ext = Str(ep, "container_extension");
                        EpisodeInfo? info = null;
                        if (ep.TryGetProperty("info", out var ii) && ii.ValueKind == JsonValueKind.Object)
                            info = new EpisodeInfo(Str(ii, "plot"), Str(ii, "duration"), Str(ii, "rating"));
                        list.Add(new Episode(id, num, title, ext, info));
                    }
                    _seasons[season.Name] = list.OrderBy(x => x.EpisodeNum).ToList();
                }
            }
            _loadingSeason = true;
            SeasonBox.ItemsSource = _seasons.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            if (SeasonBox.Items.Count > 0) SeasonBox.SelectedIndex = 0;
            _loadingSeason = false;
            ShowSeason(SeasonBox.SelectedItem as string);
            StatusLabel.Text = "";
        }
        catch (Exception ex) { StatusLabel.Text = "Error: " + ex.Message; }
    }

    private void Season_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSeason) return;
        ShowSeason(SeasonBox.SelectedItem as string);
    }

    private void ShowSeason(string? season)
    {
        if (season is null || !_seasons.TryGetValue(season, out var eps)) return;
        EpisodeList.ItemsSource = eps.Select(ep => new MediaCard(
            ep.Id, $"E{ep.EpisodeNum} — {ep.Title}",
            null, ep.Info?.Plot, ep)).ToList();
    }

    private void Episode_Activated(object sender, SelectionChangedEventArgs e)
    {
        if (EpisodeList.SelectedItem is not MediaCard card || card.Tag is not Episode ep) return;
        EpisodeList.SelectedItem = null;
        var st = AppState.Current;
        if (st.Xtream is null) return;
        var ext = string.IsNullOrEmpty(ep.ContainerExtension) ? "mp4" : ep.ContainerExtension;
        var url = st.Xtream.EpisodeUrl(ep.Id, ext);
        var title = $"{_series.Name} E{ep.EpisodeNum} — {ep.Title}";
        PlayerService.Current.Play(url, title);
        new PlayerWindow(title).Show();
        Close();
    }
}
