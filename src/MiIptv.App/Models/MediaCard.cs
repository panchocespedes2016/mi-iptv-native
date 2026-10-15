namespace MiIptv.App.Models;

/// <summary>Tarjeta genérica de póster para películas/series.</summary>
public sealed record MediaCard(string Id, string Title, string? Logo, string? Sub, object Tag);
