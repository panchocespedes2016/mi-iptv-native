namespace MiIptv.App.Models;

/// <summary>Canal unificado (Xtream o m3u) listo para reproducir.</summary>
public sealed record ChannelItem(string Id, string Name, string? Logo, string? Group, string Url);
