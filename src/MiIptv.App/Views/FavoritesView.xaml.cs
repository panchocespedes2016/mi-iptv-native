using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using MiIptv.App.Models;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

public partial class FavoritesView : UserControl
{
    private bool _loaded;

    public FavoritesView() => InitializeComponent();

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        var st = AppState.Current;
        var p = st.ActiveProfile;
        var favs = st.Favorites;
        if (p is null || favs.Count == 0)
        {
            StatusLabel.Text = favs.Count == 0 ? "Sin favoritos — marca ★ en la lista de canales" : "Carga un perfil en Ajustes";
            FavList.ItemsSource = null;
            return;
        }
        StatusLabel.Text = "Cargando…";
        try
        {
            var rows = new List<ChannelRow>();
            if (p.Type == "xtream" && st.Xtream is not null)
            {
                var streams = await st.Xtream.GetLiveStreamsAsync();
                foreach (var s in streams)
                {
                    var id = $"xt_{s.StreamId}";
                    if (favs.Contains(id))
                        rows.Add(new ChannelRow(new ChannelItem(
                            id, s.Name, s.StreamIcon, null, st.Xtream.LiveUrl(s.StreamId)), true));
                }
            }
            else
            {
                var text = p.Content ?? await new HttpClient().GetStringAsync(p.Url!);
                var parsed = M3UParser.Parse(text);
                for (int i = 0; i < parsed.Count; i++)
                {
                    var id = $"m3u_{i}";
                    if (favs.Contains(id))
                    {
                        var c = parsed[i];
                        rows.Add(new ChannelRow(new ChannelItem(id, c.Name, c.Logo, c.Group, c.Url), true));
                    }
                }
            }
            FavList.ItemsSource = rows;
            StatusLabel.Text = $"{rows.Count} favoritos";
        }
        catch (Exception ex) { StatusLabel.Text = "Error: " + ex.Message; }
    }

    private void Fav_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb || cb.DataContext is not ChannelRow row) return;
        if (!row.IsFavorite)
        {
            AppState.Current.Favorites.Remove(row.Channel.Id);
            AppState.Current.Save();
            var list = (FavList.ItemsSource as List<ChannelRow>)?.Where(r => r.IsFavorite).ToList();
            FavList.ItemsSource = list;
            StatusLabel.Text = $"{list?.Count ?? 0} favoritos";
        }
    }

    private void Fav_Activated(object sender, SelectionChangedEventArgs e)
    {
        if (FavList.SelectedItem is not ChannelRow row) return;
        FavList.SelectedItem = null;
        var ch = row.Channel;
        var sid = ch.Id.StartsWith("xt_") && int.TryParse(ch.Id[3..], out var n) ? (int?)n : null;
        PlayerService.Current.Play(ch.Url, ch.Name);
        new PlayerWindow(ch.Name, sid).Show();
    }
}
