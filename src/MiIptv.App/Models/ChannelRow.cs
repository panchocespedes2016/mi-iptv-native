using System.ComponentModel;

namespace MiIptv.App.Models;

/// <summary>Fila de canal con estado de favorito (para la lista de TV en vivo).</summary>
public sealed class ChannelRow : INotifyPropertyChanged
{
    public ChannelItem Channel { get; }

    private bool _fav;
    public bool IsFavorite
    {
        get => _fav;
        set { _fav = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite))); }
    }

    public ChannelRow(ChannelItem channel, bool fav)
    {
        Channel = channel;
        _fav = fav;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
