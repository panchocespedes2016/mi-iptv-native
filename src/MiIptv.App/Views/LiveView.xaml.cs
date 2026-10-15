using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MiIptv.App.Models;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

public partial class LiveView : UserControl
{
    private List<ChannelItem> _all = new();          // m3u: todo parseado
    private List<(string Id, string Name)> _cats = new();
    private string? _activeCat;
    private bool _loaded;

    public LiveView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var st = AppState.Current;
        var p = st.ActiveProfile;
        if (p is null || (p.Type == "xtream" && st.Xtream is null))
        {
            HintLabel.Text = "Carga un perfil en Ajustes";
            return;
        }
        HintLabel.Text = "Cargando canales…";
        try
        {
            if (p.Type == "xtream" && st.Xtream is not null)
            {
                var cats = await st.Xtream.GetLiveCategoriesAsync();
                _cats = cats.Select(c => (c.CategoryId, c.CategoryName)).ToList();
                BuildCatButtons();
                await LoadXtreamCategoryAsync(null);
            }
            else
            {
                var text = p.Content ?? await DownloadAsync(p.Url!);
                var parsed = M3UParser.Parse(text);
                var groups = parsed.Select(c => c.Group ?? "Sin grupo").Distinct().ToList();
                _cats = groups.Select((g, i) => ($"g{i}:{g}", g)).ToList();
                _all = parsed.Select((c, i) => new ChannelItem($"m3u:{i}", c.Name, c.Logo, c.Group, c.Url)).ToList();
                BuildCatButtons();
                ApplyFilter();
            }
            HintLabel.Text = "Elige un canal de la lista";
        }
        catch (Exception ex) { HintLabel.Text = "Error: " + ex.Message; }
    }

    private async Task LoadXtreamCategoryAsync(string? catId)
    {
        var st = AppState.Current;
        var streams = await st.Xtream!.GetLiveStreamsAsync(catId);
        _all = streams.Select(s => new ChannelItem(
            $"xt:{s.StreamId}", s.Name, s.StreamIcon, null, st.Xtream.LiveUrl(s.StreamId))).ToList();
        ApplyFilter();
    }

    private static async Task<string> DownloadAsync(string url)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };
        return await http.GetStringAsync(url);
    }

    private void BuildCatButtons()
    {
        CatPanel.Children.Clear();
        AddCatButton(null, "Todas");
        foreach (var (id, name) in _cats) AddCatButton(id, name);
    }

    private void AddCatButton(string? id, string name)
    {
        var b = new Button
        {
            Content = name, Tag = id,
            Margin = new Thickness(2), Padding = new Thickness(8, 4, 8, 4),
        };
        if (id == _activeCat) b.Background = (Brush)FindResource("Accent");
        b.Click += async (_, _) =>
        {
            _activeCat = id;
            BuildCatButtons();
            var st = AppState.Current;
            if (st.ActiveProfile?.Type == "xtream" && st.Xtream is not null)
            {
                HintLabel.Text = "Cargando…";
                try { await LoadXtreamCategoryAsync(id); }
                catch (Exception ex) { HintLabel.Text = "Error: " + ex.Message; return; }
                HintLabel.Text = "Elige un canal de la lista";
            }
            else ApplyFilter();
        };
        CatPanel.Children.Add(b);
    }

    private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        var list = _all.AsEnumerable();
        // En Xtream la categoría ya viene filtrada del servidor; en m3u se filtra aquí.
        if (_activeCat is not null && AppState.Current.ActiveProfile?.Type == "m3u")
        {
            var catName = _cats.FirstOrDefault(c => c.Id == _activeCat).Name;
            list = list.Where(c => c.Group == catName);
        }
        if (q.Length > 0) list = list.Where(c => c.Name.ToLowerInvariant().Contains(q));
        ChannelList.ItemsSource = list.Take(500).ToList();
    }

    private void Channel_Activated(object sender, SelectionChangedEventArgs e)
    {
        if (ChannelList.SelectedItem is not ChannelItem ch) return;
        ChannelList.SelectedItem = null;
        try
        {
            PlayerService.Current.Play(ch.Url, ch.Name);
            new PlayerWindow(ch.Name).Show();
        }
        catch (Exception ex)
        {
            HintLabel.Text = "Error al reproducir: " + ex.Message;
        }
    }
}
