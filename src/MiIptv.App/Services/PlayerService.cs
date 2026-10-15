using LibVLCSharp.Shared;

namespace MiIptv.App.Services;

/// <summary>Reproductor único de la app (motor VLC). Una instancia para todo.</summary>
public sealed class PlayerService
{
    public static PlayerService Current { get; } = new();

    private readonly LibVLC _libVLC;
    public MediaPlayer Player { get; }
    public string CurrentTitle { get; private set; } = "";
    public string CurrentUrl { get; private set; } = "";

    public event Action? StateChanged;

    private PlayerService()
    {
        _libVLC = new LibVLC();
        Player = new MediaPlayer(_libVLC);
        Player.Playing += (_, _) => StateChanged?.Invoke();
        Player.Paused += (_, _) => StateChanged?.Invoke();
        Player.Stopped += (_, _) => StateChanged?.Invoke();
    }

    public void Play(string url, string title)
    {
        CurrentUrl = url;
        CurrentTitle = title;
        Player.Stop();
        using var media = new Media(_libVLC, url, FromType.FromLocation);
        Player.Play(media);
    }

    public void TogglePlayPause()
    {
        if (Player.IsPlaying) Player.Pause();
        else Player.Play();
    }

    public void Stop() => Player.Stop();
}
