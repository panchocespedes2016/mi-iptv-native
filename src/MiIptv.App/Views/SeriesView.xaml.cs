using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MiIptv.App.Models;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

/// <summary>Series con búsqueda global en todas las subdivisiones (muestra 📁 categoría).</summary>
public partial class SeriesView : UserControl
{
    private List<(SeriesStream S, string Cat)> _all = new();
    private List<(string Id, string Name)> _cats = new();
    private string? _activeCat;
    private bool _loaded;

    public SeriesView() => InitializeComponent();

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
            StatusLabel.Text = "Series requiere un perfil Xtream";
            return;
        }
        StatusLabel.Text = "Cargando…";
        try
        {
            var scats = await st.Xtream.GetSeriesCategoriesAsync();
            _cats = scats.Select(c => (c.CategoryId, c.CategoryName)).ToList();
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
        _all.Clear();
        if (catId is null)
        {
            // todas: una pasada por categoría
            foreach (var (id, name) in _cats)
            {
                var list = await st.Xtream!.GetSeriesAsync(id);
                _all.AddRange(list.Select(s => (s, name)));
            }
        }
        else
        {
            var name = _cats.FirstOrDefault(c => c.Id == catId).Name ?? "";
            var list = await st.Xtream!.GetSeriesAsync(catId);
            _all.AddRange(list.Select(s => (s, name)));
        }
        ApplyFilter();
        StatusLabel.Text = $"{_all.Count} series";
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
        if (q.Length > 0) list = list.Where(x => x.S.Name.ToLowerInvariant().Contains(q));
        Grid.ItemsSource = list.Take(400).Select(x => new MediaCard(
            x.S.SeriesId.ToString(), x.S.Name, x.S.Cover,
            _activeCat is null ? $"📁 {x.Cat}" : (string.IsNullOrEmpty(x.S.Rating) ? null : $"★ {x.S.Rating}"),
            x.S)).ToList();
    }

    private void Grid_Activated(object sender, SelectionChangedEventArgs e)
    {
        if (Grid.SelectedItem is not MediaCard card || card.Tag is not SeriesStream s) return;
        Grid.SelectedItem = null;
        new SeriesDetailWindow(s).Show();
    }
}
