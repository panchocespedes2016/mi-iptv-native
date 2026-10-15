using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MiIptv.App.Models;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

public partial class MoviesView : UserControl
{
    private List<VodStream> _all = new();
    private List<(string Id, string Name)> _cats = new();
    private string? _activeCat;
    private bool _loaded;

    public MoviesView() => InitializeComponent();

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var st = AppState.Current;
        if (st.ActiveProfile?.Type != "xtream" || st.Xtream is null)
        {
            StatusLabel.Text = "Películas requiere un perfil Xtream";
            return;
        }
        StatusLabel.Text = "Cargando…";
        try
        {
            var cats = await st.Xtream.GetVodCategoriesAsync();
            _cats = cats.Select(c => (c.CategoryId, c.CategoryName)).ToList();
            BuildCatButtons();
            await LoadCategoryAsync(null);
            StatusLabel.Text = "";
        }
        catch (Exception ex) { StatusLabel.Text = "Error: " + ex.Message; }
    }

    private async Task LoadCategoryAsync(string? catId)
    {
        var st = AppState.Current;
        StatusLabel.Text = "Cargando…";
        _all = await st.Xtream!.GetVodStreamsAsync(catId);
        ApplyFilter();
        StatusLabel.Text = $"{_all.Count} películas";
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
            Content = name, Margin = new Thickness(2), Padding = new Thickness(8, 4, 8, 4),
        };
        if (id == _activeCat) b.Background = (Brush)FindResource("Accent");
        b.Click += async (_, _) =>
        {
            _activeCat = id; BuildCatButtons();
            try { await LoadCategoryAsync(id); }
            catch (Exception ex) { StatusLabel.Text = "Error: " + ex.Message; }
        };
        CatPanel.Children.Add(b);
    }

    private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        var list = _all.AsEnumerable();
        if (q.Length > 0) list = list.Where(m => m.Name.ToLowerInvariant().Contains(q));
        Grid.ItemsSource = list.Take(400).Select(m => new MediaCard(
            m.StreamId.ToString(), m.Name, m.StreamIcon,
            string.IsNullOrEmpty(m.Rating) ? null : $"★ {m.Rating}", m)).ToList();
    }

    private void Grid_Activated(object sender, SelectionChangedEventArgs e)
    {
        if (Grid.SelectedItem is not MediaCard card || card.Tag is not VodStream vod) return;
        Grid.SelectedItem = null;
        new MovieDetailWindow(vod).Show();
    }
}
